# API Layer

`ZooFinder.Api` is the future HTTP transport and composition root.

**Status:** Only Program.cs, project configuration, and settings exist. Program adds controllers/OpenAPI,
maps controllers, and invokes HTTPS redirection/authorization middleware. No business controllers,
Application/Infrastructure registrations, bearer authentication configuration, exception mapping,
upload decoder, CSV parser, or SignalR Hub is implemented.

The following sections describe required future behavior. Architectural boundaries are defined in
[Architecture rules](../architecture.md).

## Responsibilities

Controllers adapt transport input to Application scenarios and return their responses.
Application services own business flow; controllers do not duplicate registration or transition rules.

API will resolve authenticated identities, configure authentication, map errors, validate uploads,
and compose services with Infrastructure implementations.

## Planned Endpoint Groups

| Group | Operations |
| --- | --- |
| Animals | External/local catalog search and animal page |
| Recognition | Image upload, optional ParkId, recognition result |
| Parks | Park catalog/state, memberships/publication, import preview/apply, connection/payment tracking |
| Users | Public/current profile read and authenticated profile update |
| Auth | Register/login, refresh, revoke one/all sessions |
| Discussions | General room, creation, history, send/edit/delete message |

URLs and HTTP methods have not been implemented or finalized.
Park operations are open to everyone in v1; they must not acquire owner/role/payment gates merely because
existing Auth/Discussions use authentication. Public views still respect active/publication visibility.

## Input Boundary

- Pass `HttpContext.RequestAborted` into Application operations.
- Map pagination to existing requests; treat cursors as opaque.
- Adapt `IFormFile` to the stream-based recognition request. Check actual upload size and file signature,
  not only client-supplied content type or declared length.
- Pass optional `ParkId`; park candidates are read server-side by Recognition, not trusted from the browser.
- Decode CSV into existing import-row contracts before Application preview/apply.
  File decoding must not independently save species or memberships.
- Obtain account IDs for protected operations from validated claims, not client-selected actor IDs.

Application already validates declared image length up to 10 MiB. Upload parsing and actual file validation
remain pending, as does the internal .NET-to-Python HTTP schema.

## Output Boundary

Dates are UTC. Cursor pages return `Items` and `NextCursor`; offset pages retain page/size/count metadata.
Use Application responses or thin transport wrappers.

Recognition returns `Recognized`, `Uncertain`, or `NoAnimal`, names/alternatives, optional `ParkAnimalId`,
and optional execution metadata. A technical inference failure must not be disguised as uncertainty.
A park-card or separate catalog lookup supplies Wiki information; the model does not produce trusted source URLs.

Deleted discussion-history items retain metadata with null content. Internal exception details and raw
provider payloads are not public responses.

## Planned Error Mapping

| Application outcome | HTTP response |
| --- | --- |
| Invalid request | 400 |
| Missing/invalid authentication | 401 |
| Insufficient permission | 403 |
| Resource not found | 404 |
| State conflict | 409 |
| Unexpected failure | 500 |

Problem Details mapping and concrete provider timeout/unavailability responses remain to be implemented.

## Authentication and Composition

Bearer access tokens, refresh endpoints, and existing account rules are planned around the current Application
contracts. Refresh-session revocation does not require persisting access tokens.

Composition must register Application services, nine data sources, providers/security/event implementations,
DbContext, settings, and TimeProvider. A future Infrastructure registration helper is planned but absent.

Event transport/Hub placement must preserve the direction of dependencies: Infrastructure cannot reference Api.
No runtime database creation/migration should be inferred from the current Program scaffold.
