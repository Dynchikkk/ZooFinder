# Backend Architecture

## Structure

```text
apps/backend/ZooFinder/
├─ ZooFinder.Api/
├─ ZooFinder.Application/
├─ ZooFinder.Domain/
├─ ZooFinder.Infrastructure/
└─ ZooFinder.slnx
```

## Dependency Direction

```text
ZooFinder.Api ───────────→ ZooFinder.Application ─────→ ZooFinder.Domain
       │
       └───────────────→ ZooFinder.Infrastructure ───→ ZooFinder.Application
                                                    └→ ZooFinder.Domain
```

Dependencies point toward Domain. Domain contains the model, Application contains use cases and ports, Infrastructure implements ports, and API exposes use cases over HTTP.

## Layers

| Project | Responsibility |
| --- | --- |
| `ZooFinder.Domain` | Entities, relationships, enums, and domain rules |
| `ZooFinder.Application` | Use cases, contracts, validation, mapping, and ports |
| `ZooFinder.Infrastructure` | Persistence, external providers, security implementations, and configuration |
| `ZooFinder.Api` | HTTP transport, authentication, exception mapping, and composition |

## Modules

| Module | Responsibility |
| --- | --- |
| Animals | External and local catalog search, animal pages, image recognition |
| Users | Public and current-user profiles |
| Auth | Registration, access tokens, refresh sessions, session revocation |
| Discussions | Rooms, message history, message creation, editing, and deletion |

## Core Flows

### Animal catalog

```text
Client → API → AnimalCatalogService
                    ├─ external scope → IAnimalInformationService
                    └─ local scope ───→ IAnimalRepository
```

Catalog search and animal-page retrieval are read-only.

### Recognition

```text
Client → API → AnimalRecognitionService → IAnimalRecognitionProvider
```

Recognition returns names. The client starts a separate catalog search from the recognition result.

### Discussion creation

```text
First message
    → resolve animal
    → create local animal when absent
    → create General room
    → create message
    → commit one transaction
```

## Layer Documentation

- [Domain](domain.md)
- [Application](application.md)
- [Infrastructure](infrastructure.md)
- [API](api.md)
