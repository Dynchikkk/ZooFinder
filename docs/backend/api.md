# API Layer

API owns HTTP transport and composition. Core setup, error handling, and Auth endpoints are implemented;
other business endpoints remain pending.
See [Backend](README.md) for current implementation status.

## Transport Rules

### Controllers and Identity

- Group controllers in Controllers/<Module>/ with matching namespaces.
- Do not create controller folders for unimplemented modules.
- Adapt transport inputs and invoke Application scenarios.
- Do not duplicate business rules in controllers.
- Pass RequestAborted to operations.
- Obtain actor account IDs from validated claims, not request-selected IDs.
- Keep public park flows open in v1 while preserving publication/availability filters.

### Uploads and Imports

- Adapt IFormFile to the stream contract.
- Check actual size and file signature, not just declared metadata.
- Application's declared recognition-image limit is 10 MiB.
- Pass optional ParkId; recognition candidates are loaded server-side.
- Do not trust a browser-supplied catalog.
- Decode CSV into existing contracts before preview/apply.
- Decoding does not independently save cards or park links.

### Responses

- Use Application responses or thin transport wrappers.
- Return UTC dates and treat cursors as opaque.
- Keep recognition outcomes and technical errors distinct.
- Wiki URLs come from the information provider.
- Deleted history retains metadata with null content.
- Internal exceptions and raw provider payloads are not public output.

## Errors

| Scenario outcome | HTTP status |
| --- | --- |
| Invalid request | 400 |
| Missing/invalid authentication | 401 |
| Insufficient permission | 403 |
| Not found | 404 |
| State conflict | 409 |
| Information/recognition provider unavailable or timed out | 503 |
| Unexpected failure | 500 |

HTTP errors use Problem Details. Application validation messages are public; unexpected failures and provider
unavailability expose no exception details. Responses include a traceId; custom error logging is deferred.
Malformed HTTP requests preserve BadHttpRequestException's status with a generic message.
Empty error responses use the standard Problem Details writer when the client's Accept header permits JSON.
Controller model-binding/validation errors retain the standard ValidationProblemDetails errors collection.
Aborted requests follow the host's cancellation handling; they are not mapped to provider unavailability.
Endpoint URLs/methods outside Auth are not finalized.

## Composition and Authentication

### Composition

- All IServiceCollection helpers belong to API.
- Runtime setup registers SQL Server DbContext, TimeProvider, Auth security/settings, Wiki/Python clients, and a bounded cache.
- Auth services and data sources are registered with scoped lifetimes.
- Application services and data sources are registered with their API modules; discussions await the event publisher.
- Configure Application settings here; implementations do not register themselves.
- Event/Hub composition must preserve dependency direction: Infrastructure cannot import API.
- Runtime schema initialization is not implemented.

Registration structure is mandatory:

- Place all IServiceCollection registration helpers in DependencyInjection, named <Owner>ServiceCollectionExtensions.
- ApiServiceCollectionExtensions is the single composition entry point. It wires modules and common framework services;
  module-specific settings, implementations, and configuration callbacks stay in the owning registration class.
- Use one registration class per primary module: Auth, Users, Animals, Parks, and Discussions.
  Group child scenarios with their parent module rather than creating registrations per controller.
- Auth owns security implementations and bearer configuration; Animals owns Wiki and recognition providers;
  Parks owns park animals, imports, and connections; Discussions owns rooms, messages, and SignalR.
- Give shared technical concerns, such as persistence, caching, and error handling, separate registration classes.
- Register each dependency once. The composition entry point connects modules and shared registrations;
  module registration methods must not invoke other modules' registration methods.
- Keep reusable configuration readers and startup validation in Configuration; startup validation is an ordinary method.
  Bind and validate module options in the owning registration class.
- Place other extension methods with their responsibility, such as claims helpers in Authentication.
  Do not introduce a root Extensions folder or generic ServiceCollectionExtensions/ApplicationBuilderExtensions classes.
- Create registration files only when their functionality is introduced; do not add empty module scaffolding.

Configuration sections are Auth, Jwt, Wiki, Recognition, and MemoryCache; SQL uses ConnectionStrings:ZooFinder.
appsettings.json contains common defaults and placeholders for deployment-specific values; appsettings.Development.json supplies local values.
The standard host loads the base file first, then environment-specific overrides; other environments must supply valid values.
Supply Jwt:SigningKeyBase64 and an identifying Wiki:UserAgent through runtime configuration; no credentials are committed.
Environment-variable keys use `__`, for example `Jwt__SigningKeyBase64` and `Wiki__UserAgent`.
Startup validates configuration and constructs core dependencies without connecting to SQL Server or external services.
Settings are bound at startup; changing them requires restarting the host.

### Bearer Validation

Validation uses the same [JWT settings](infrastructure.md#auth-security) as token issuance.

- Require HS256.
- Validate signature, issuer, audience, and lifetime.
- Disable inbound claim mapping.
- Use sub/role as name/role claim types.
- Require sub to contain a non-empty account GUID; validate lifetime with no additional clock skew allowance.
- Load signing keys from secure runtime configuration.

Bearer validation does not query the database. Access tokens are not persisted and remain valid until expiration
after refresh-session revocation; account status and role changes are not checked on every HTTP request.
Application scenarios retain their own account checks.

### Auth HTTP

All routes use POST under /api/auth and reuse Application request/response contracts.
Successful token responses use HTTP 200 and disable response caching.

| Route | Input | Access | Success |
| --- | --- | --- | --- |
| register | Login and password | Anonymous | AuthResponse |
| login | Login and password | Anonymous | AuthResponse |
| refresh | RefreshToken | Refresh-token possession | AuthResponse with rotated tokens |
| logout | RefreshToken | Refresh-token possession | 204 |
| logout-all | No body | Valid bearer token; account ID from sub | 204 |

Refresh and logout do not require an unexpired access token. Tokens are exchanged in JSON; cookie transport is not implemented.
Invalid credentials or refresh tokens return 401; a duplicate login returns 409.
HTTP 401 responses include WWW-Authenticate: Bearer without internal validation details.
Session behavior remains owned by [Application](application.md#authentication-and-profiles).

Other business controllers/module registration, uploads, CSV, and event transport remain pending.
