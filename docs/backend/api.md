# API Layer

API owns HTTP transport and composition. Dependency/configuration setup is available; HTTP endpoints remain pending.
See [Backend](README.md) for current implementation status.

## Transport Rules

### Controllers and Identity

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
Endpoint URLs/methods are not finalized.

## Composition and Authentication

### Composition

- All IServiceCollection helpers belong to API.
- Runtime setup registers SQL Server DbContext, TimeProvider, Auth security/settings, Wiki/Python clients, and a bounded cache.
- Application services and data sources are registered with their API modules; discussions await the event publisher.
- Configure Application settings here; implementations do not register themselves.
- Event/Hub composition must preserve dependency direction: Infrastructure cannot import API.
- Runtime schema initialization is not implemented.

Configuration sections are Auth, Jwt, Wiki, Recognition, and MemoryCache; SQL uses ConnectionStrings:ZooFinder.
appsettings.json contains common defaults and placeholders for deployment-specific values; appsettings.Development.json supplies local values.
The standard host loads the base file first, then environment-specific overrides; other environments must supply valid values.
Supply Jwt:SigningKeyBase64 and an identifying Wiki:UserAgent through runtime configuration; no credentials are committed.
Environment-variable keys use `__`, for example `Jwt__SigningKeyBase64` and `Wiki__UserAgent`.
Startup validates configuration and constructs core dependencies without connecting to SQL Server or external services.
Settings are bound at startup; changing them requires restarting the host.

### Bearer Validation

Use matching [JWT settings](infrastructure.md#auth-security).

- Require HS256.
- Validate signature, issuer, audience, and lifetime.
- Disable inbound claim mapping.
- Use sub/role as name/role claim types.
- Load signing keys from secure runtime configuration.

Refresh endpoints follow existing session rules.
Access tokens are not persisted.

Business controllers/module registration, bearer authentication, uploads, CSV, and event transport remain pending.
