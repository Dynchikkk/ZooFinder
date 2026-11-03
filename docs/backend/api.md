# API Layer

API owns HTTP transport and composition. See [Backend](README.md) for implementation status
and [Application](application.md) for scenario behavior.

## Organization

| Responsibility | Location |
| --- | --- |
| Controllers | `Controllers/<Module>/`; namespaces match folders |
| DI registration | `DependencyInjection/<Owner>ServiceCollectionExtensions.cs` |
| Claims helpers | Authentication/ |
| Shared configuration readers and startup validation | Configuration/ |
| API-owned settings | `Configuration/<Module>/Settings/` |
| HTTP error mapping and responses | ErrorHandling/ |
| SignalR hubs and event transport | `Realtime/<Module>/` |
| Request logging middleware | Logging/ |

- ApiServiceCollectionExtensions is the single composition entry point; module details stay in their registration classes.
- Use one registration class per primary module: Auth, Users, Animals, Parks, Discussions.
  Register child scenarios with their parent module, rather than per controller.
- Auth owns security and bearer configuration; Animals owns Wiki and recognition providers.
- Give shared persistence, caching, and error handling separate registration classes.
- Register each dependency once. Only the composition entry point connects module registrations;
  modules must not invoke each other's registration methods.
- Bind and validate module settings in their owning registration. Startup validation is an ordinary method.
- Keep other helpers with their responsibility; no root Extensions folder or generic
  ServiceCollectionExtensions/ApplicationBuilderExtensions classes.
- Create folders and registration files as functionality is introduced; no empty scaffolding.

## Transport

- Controllers adapt HTTP input/output and call Application services without duplicating business rules.
- Use `api/[controller]` for controller prefixes and explicit operation paths where needed.
- Mark JSON request objects with [FromBody] and query request objects with [FromQuery].
- Pass RequestAborted to operations; obtain actor IDs from validated claims.
- Use Application responses or thin transport wrappers; return UTC dates and treat cursors as opaque.
- Validate actual image size/signature, then adapt IFormFile to the Application stream contract.
  Recognition limits belong to Application; park candidates are loaded server-side.
- Decode CSV into Application contracts before preview/apply; saving belongs to the scenario.

## Errors

Use Problem Details for HTTP errors. Expected Application messages are public; internal exception details
and raw provider responses must not reach clients.

| Outcome | HTTP status |
| --- | --- |
| Invalid request | 400 |
| Missing/invalid authentication | 401 |
| Insufficient permission | 403 |
| Not found | 404 |
| State conflict | 409 |
| Wiki/recognition unavailable or timed out | 503 |
| Unexpected failure | 500 |

Malformed HTTP requests preserve their transport status with a generic message.
Caller cancellation is not provider unavailability. Validation responses retain field errors;
empty error responses use Problem Details when the client accepts JSON.

## Logging

JSON console output includes scopes. RequestLoggingMiddleware supplies RequestId, matching Problem Details traceId,
and writes method, route template, and final status at Debug after exception handling; unmatched paths use a fixed label.
It does not record duration, actual paths, query strings, headers, bodies, route values, or user content.
Handled unexpected HTTP failures log their exception at Error, retaining the message, stack, and inner causes.
ASP.NET Core owns diagnostics for escaping HTTP failures and Hub invocation errors; the request middleware does not log exceptions.
Expected Application errors add no error log; Infrastructure owns external-unavailability warnings.
Microsoft/System diagnostics are enabled at Warning and above; host lifetime events remain at Information.
HTTP clients use standard diagnostics controlled by category filters. Application logs do not dump bodies or credentials;
exception logging does not automatically redact secrets, so exception messages must not embed credentials or user payloads.
EF sensitive-data logging and IdentityModel PII logging remain disabled.
Logging:LogLevel supports namespace/class overrides and None; default ZooFinder level is Information.
The standard configuration provider can reload logging filters independently of application settings.

## Configuration

appsettings.json contains shared defaults and deployment placeholders; appsettings.Development.json supplies local overrides.
Supply secrets through runtime configuration. Settings changes require a restart.
Startup validates configuration without connecting to SQL Server or external services.
Database initialization follows the [migration policy](../development.md#database-migrations).

## Authentication

Bearer validation uses the same [JWT settings](infrastructure.md#jwt-settings) as issuance:
HS256, signature, issuer, audience, and lifetime, with no clock skew allowance or inbound claim mapping.
The sub claim must be a non-empty account GUID; sub/role are the name/role claim types.

Bearer validation does not query the database. Account status and role changes are not checked on every request;
Application scenarios retain their own checks. Revoking refresh sessions does not invalidate issued access tokens.

All Auth routes use POST under /api/Auth and JSON contracts. Token responses disable caching.

| Route | Access | Success |
| --- | --- | --- |
| register | Anonymous | 200, AuthResponse |
| login | Anonymous | 200, AuthResponse |
| refresh | Refresh-token possession | 200, AuthResponse |
| logout | Refresh-token possession | 204 |
| logout-all | Bearer; account ID from sub | 204 |

Refresh/logout do not require an unexpired access token. HTTP 401 responses include WWW-Authenticate: Bearer.
Session rules belong to [Application](application.md#authentication-and-profiles).

## User Profile

GET/PUT /api/UserProfile require bearer authentication and use the account ID from sub.
PUT accepts DisplayName; both return UserProfileResponse with HTTP 200 and disable response caching.

## Animal Catalog

GET /api/AnimalCatalog accepts AnimalSearchRequest from query parameters; GET /api/AnimalCatalog/details accepts AnimalPageRequest.
Both are public reads; [Application](application.md#catalog) owns search, pagination, and provider behavior.

## Animal Recognition

POST /api/AnimalRecognition is public and accepts multipart fields: image, languageCode (default en), and optional parkId.
It returns AnimalRecognitionResponse with HTTP 200. API checks IFormFile.Length against Application's 10 MiB limit and detects
JPEG/PNG/WebP from signatures, ignoring the supplied MIME type and extension. Empty files return 400,
oversized files 413, and unsupported signatures 415. Signature checks do not replace full image decoding in Python.
Application loads park candidates server-side; API disposes the image stream after the scenario completes.
Images are not persisted or cached by the recognition flow. Live inference requires the pending Python service.

## Parks

/api/Parks exposes search, details, creation, editing, and status changes; /api/ParkAnimals exposes membership operations.
All park operations are public in v1. IDs for individual reads, updates, and removal come from route parameters.
Creation returns 201; membership addition/restoration returns 200; removal returns 204.

### CSV Import

POST /api/ParkImport/{parkId}/preview accepts a multipart `file`; POST /api/ParkImport/{parkId}/apply accepts selected rows as JSON.
Both are public and return 200. Preview does not save data; apply uses the existing Application scenario.

CSV uses UTF-8 (with or without BOM), `;`, and these case-sensitive headers in any order:
`commonName;scientificName;informationSource;sourceItemId;languageCode;localDescription`.
Quoted fields support separators, escaped quotes, and line breaks. Blank lines are ignored; an empty language defaults to en.
Malformed CSV returns 400; an oversized file returns 413. ParkImport:MaximumFileBytes defaults to 1 MiB.
The [Application import rules](application.md#imports) define the row limit, matching, and write guarantees.

### Connection Requests

/api/ParkConnections exposes search by parkId with offset pagination, individual reads, and creation.
POST actions /{requestId}/approve, /{requestId}/confirm-payment, and /{requestId}/close manage the request workflow.
Creation returns 201; other operations return 200. All are public in v1; payment confirmation is manual.

## Discussions

/api/DiscussionRooms exposes reads by room ID, GET /general?animalId=..., and POST creation/reuse of a General room.
/api/DiscussionMessages exposes GET history (discussionRoomId, cursor, limit), POST sending, PUT /{messageId}, and DELETE /{messageId}.
Reads are public; writes require bearer authentication and take the actor ID from sub. Success is 200, except deletion (204).

The public /hubs/discussions hub only exposes JoinRoom(roomId) and LeaveRoom(roomId) for the current connection.
Joining checks room visibility through Application; closed rooms remain readable. Leaving validates the ID and permits cleanup
even if the room has disappeared. Expected join errors use HubException; unexpected error details remain hidden.
MessageCreated, MessageUpdated, and MessageDeleted carry DiscussionMessageEvent; deleted messages have null content.
Events target the room's connections, including the sender if subscribed. Clients merge HTTP responses/events by message ID.
After connecting or reconnecting, join before reloading history and merge events received during the reload.
One backend process is supported; groups and events have no durable replay. Persistence precedes publication;
publication failure may follow a committed write. Reload history to reconcile before retrying a write.
