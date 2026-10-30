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
| HTTP error mapping and responses | ErrorHandling/ |

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
and raw provider responses must not reach clients. Custom error logging is deferred.

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
