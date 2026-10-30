# API Implementation Plan

Current position: steps 1–5 are implemented; step 6 is next.
Implementation status does not imply live SQL Server or end-to-end verification.

Follow the [development rules](../development.md) and [API rules](../backend/api.md).
The broader [product roadmap](parks-and-recognition-roadmap.md) covers Python, frontend, deployment, and research.

## Implementation Order

1. [x] **Configuration and core DI.**
   Configure SQL Server, Auth settings/security, Wiki/Python clients, bounded memory cache, and startup validation.
   Keep common defaults and placeholders in appsettings.json; supply local overrides through Development configuration.

2. [x] **Common error handling.**
   Map Application exceptions to HTTP statuses and return Problem Details without exposing internal exception details.
   Custom logging is deferred until a consistent approach is introduced across the application.

3. [x] **Authentication and bearer validation.**
   Register Auth services/data sources and add registration, login, refresh, session revocation, and revocation of all sessions.
   Configure JWT validation and obtain the current account ID from validated claims.

4. [x] **User profile.**
   Expose the existing profile read/update scenarios for the current account.

5. [x] **Animal catalog.**
   Expose external/local search and animal details using the existing catalog service and pagination contracts.

6. [ ] **Parks and park animals — next.**
   Expose park management and animal lists, details, addition/restoration, editing, and removal.
   Preserve the agreed open park flows and publication/availability rules.

7. [ ] **CSV import.**
   Decode uploaded CSV into Application contracts; expose preview and apply through the existing import service.

8. [ ] **Park connection requests.**
   Expose the existing submission, read, approval, manual payment confirmation, and closing scenarios.

9. [ ] **Recognition uploads.**
   Validate image size/signature, adapt the upload to a stream, and pass language and optional ParkId to the service.
   Keep the .NET adapter aligned with the [Python protocol](../backend/recognition-protocol.md).
   Live inference verification requires the Python service, which is still pending.

10. [ ] **Discussions and messages.**
    Expose room operations and message history/writes over HTTP.
    Add an API-owned SignalR Hub and IDiscussionEventPublisher implementation, with room join/leave checks.
    Start with one backend process; reconnecting clients rejoin and reload history.
    Persistence precedes publication; v1 has no outbox or guaranteed event replay.

11. [ ] **Final verification and database initialization.**
    After schema agreement, create one Initial migration and apply it to a development database.
    Verify authentication, permissions, CRUD, pagination, imports, uploads, integrations, and SignalR with live dependencies.
    Add automated tests at the final development stage according to the development rules.

For steps 3–10, register each module's existing services and data sources as its endpoints are introduced.
Controllers remain thin: adapt HTTP input/output, forward cancellation, and call Application services.
Proceed one numbered step at a time and explain new methods during review.
