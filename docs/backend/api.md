# API Layer

API owns HTTP transport and composition. Program.cs is a scaffold.
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

| Scenario outcome | Planned HTTP status |
| --- | --- |
| Invalid request | 400 |
| Missing/invalid authentication | 401 |
| Insufficient permission | 403 |
| Not found | 404 |
| State conflict | 409 |
| Unexpected failure | 500 |

Problem Details and provider timeout/unavailability mapping remain to be implemented.
Endpoint URLs/methods are not finalized.

## Composition and Authentication

### Composition

- All IServiceCollection helpers belong to API.
- Compose services, sources, providers, security/events, DbContext, settings, and TimeProvider during API development.
- Configure Application settings here; implementations do not register themselves.
- Event/Hub composition must preserve dependency direction: Infrastructure cannot import API.
- Runtime schema initialization is not implemented.

### Bearer Validation

Use matching [JWT settings](infrastructure.md#auth-security).

- Require HS256.
- Validate signature, issuer, audience, and lifetime.
- Disable inbound claim mapping.
- Use sub/role as name/role claim types.
- Load signing keys from secure runtime configuration.

Refresh endpoints follow existing session rules.
Access tokens are not persisted.

Business controllers, DI, bearer authentication, errors, uploads, CSV, and event transport remain pending.
