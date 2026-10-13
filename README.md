# ZooFinder

ZooFinder is a web platform for species discovery, photo recognition in nature/safari parks,
and shared animal discussions.

## Scope

- Search animal catalogs and view information, images, and source links.
- Recognize species from photographs, with an optional park catalog.
- Manage parks, species memberships, local descriptions, publication, and imports.
- Track connection requests and manual payment confirmation.
- Register/login, edit profiles, and participate in discussions.

## Product Rules

- Animal cards and General chats are shared across parks.
- Cards are identified by source/item/language; park memberships supply local text.
- Search and recognition do not save cards automatically.
- Parks work without a species list or payment.
- Park operations are open to everyone in v1.
- Authentication and message permissions still apply to their own scenarios.
- Inference is planned locally with pretrained models, without training or fine-tuning.
- Wiki remains an external information source.

See [Backend](docs/backend/README.md) for current implementation
and the [roadmap](docs/planning/parks-and-recognition-roadmap.md) for remaining work.

## Repository

| Location | Purpose |
| --- | --- |
| apps/backend/ZooFinder/ | .NET 10 backend |
| apps/frontend/ | Planned Angular/TypeScript frontend |
| infra/docker/ and scripts/ | Deployment placeholders |

Start with the [documentation index](docs/README.md) and [mandatory development rules](AGENTS.md).

## Build

With the .NET 10 SDK, from the repository root:

```powershell
dotnet build apps/backend/ZooFinder/ZooFinder.slnx
```

Builds do not initialize a database or verify external integrations.
[Test and migration policy](docs/development.md) and
[database settings](docs/backend/infrastructure.md#connection-and-migrations) apply.
