# Backend Overview

The backend has four .NET 10 projects under `apps/backend/ZooFinder/`:
API → Infrastructure → Application → Domain.
Layer responsibilities and dependency rules are in [Architecture](../architecture.md).

## Current Status

| Component | Available | Pending |
| --- | --- | --- |
| Domain/Application | Nine entities; eleven services and their contracts | End-to-end verification |
| Persistence | EF Core/SQL Server context, mappings, nine data sources, runtime DB configuration | Agreed schema, Initial migration, live verification |
| Auth security | Password hashing, refresh-token generation/hashing, JWT issuance/validation, Auth HTTP endpoints | Live session verification |
| Integrations | Wiki/Python HTTP adapters and client configuration; event interface | Python service/model; event transport |
| API | Core DI, configuration, startup validation, Problem Details; Auth, profile, catalog, parks and park animals | Imports, connections, recognition uploads, discussions |

There is no complete runnable business flow. Basic Wikipedia calls have been checked manually;
live SQL Server concurrency and end-to-end integration are not verified. Frontend and deployment remain pending.

## Layer Documents

- [Domain](domain.md): identities, relationships, and states.
- [Application](application.md): placement rules and business scenarios.
- [Infrastructure](infrastructure.md): persistence and technical integrations.
- [API](api.md): transport and composition.
- [Remaining work and research](../planning/parks-and-recognition-roadmap.md).

Tests and migrations follow [Development](../development.md).
