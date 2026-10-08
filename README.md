# ZooFinder

ZooFinder is a web-platform project for animal discovery, photo-based species recognition in nature and
safari parks, and shared animal discussions.

**Status:** Domain, Application, and EF persistence are implemented. The API remains a scaffold.
Wiki integration, the local Python recognition service/client, security implementations, event publishing,
frontend, and deployment configuration are pending. There is no working end-to-end application yet.

## Product Scope

- Search external and saved animal catalogs; view information, images, and source links.
- Recognize a species from a photograph, optionally using a selected park's published catalog.
- Maintain parks, shared-species memberships, local descriptions, and publication state.
- Preview/apply decoded species imports and track connection requests/manual payment confirmation.
- Register/login, edit profiles, and participate in animal discussions.

Search and recognition do not save animals automatically. Adding a species through a park or explicitly
creating its discussion registers one shared Animal card and an empty General room. Other parks reuse them.
Animal represents a sourced species card, not an individual animal; matching is scoped to language.

A park works without a species list. Park operations are open to everyone in v1; owner/role restrictions and
payment gating are deferred. Payment records demonstrate the business process without blocking park use.
Existing authentication and discussion-write rules remain in force.

Neural inference will run locally using ready pretrained models, without training or fine-tuning.
Wiki remains an external information source. Model comparison and evaluation belong to a later research stage.

## Technology and Availability

| Technology | Current state |
| --- | --- |
| .NET 10 | Backend projects |
| ASP.NET Core | API scaffold, without business endpoints or composition |
| EF Core / Microsoft SQL Server | Context, configurations, and data sources; no current migrations |
| Angular / TypeScript | Planned; frontend directory contains a placeholder |
| MediaWiki API | Provider interface only |
| Local Python inference | Provider contract only; service and HTTP adapter pending |
| Docker Compose | Planned; no deployment configuration yet |

## Repository

```text
ZooFinder/
├─ apps/
│  ├─ backend/ZooFinder/
│  └─ frontend/
├─ docs/
├─ infra/docker/
├─ scripts/
└─ AGENTS.md
```

## Documentation

- [Documentation index](docs/README.md).
- [Development process](docs/development.md), [Architecture rules](docs/architecture.md), [Code style](docs/code-style.md).
- [Backend layers](docs/backend/README.md), [Frontend plan](docs/frontend/README.md).
- [Product and research roadmap](docs/planning/parks-and-recognition-roadmap.md).

`AGENTS.md` is a short entry point to the mandatory rules rather than a second copy of them.

## Build

From the repository root with the .NET 10 SDK:

```powershell
dotnet build apps/backend/ZooFinder/ZooFinder.slnx
```

Automatic tests are deferred until the final development stage or an explicit user request.
While the schema is forming, do not generate migrations or snapshots; one `Initial` migration follows model
agreement. See [Development process](docs/development.md) and
[database configuration](docs/backend/infrastructure.md#connection-and-migrations).

A build does not require or initialize the database and does not demonstrate working provider integration.
