# Backend Architecture

The backend contains four .NET 10 projects:

```text
apps/backend/ZooFinder/
├─ ZooFinder.Api/
├─ ZooFinder.Application/
├─ ZooFinder.Domain/
├─ ZooFinder.Infrastructure/
└─ ZooFinder.slnx
```

## Dependencies and Responsibilities

Current project references point inward:

```text
Api → Infrastructure → Application → Domain
```

Api can access Application through the transitive reference and will invoke its services when composition
is implemented. Infrastructure has no reference to Api; Domain references no other project.

| Project | Responsibility | Current state |
| --- | --- | --- |
| Domain | Entities, relationships, enums | Nine entities implemented |
| Application | Scenarios, validation, contracts, dependency interfaces | Eleven services implemented |
| Infrastructure | Persistence and technical dependency implementations | Nine data sources and EF mappings implemented |
| Api | HTTP transport, authentication, exception mapping, composition | Program/configuration scaffold only |

Wiki/Python providers, security implementations, event transport, runtime DI, controllers, and upload handling
remain pending. There is no initialized migration set or complete runnable business flow.

Mandatory rules are maintained separately in [Architecture rules](../architecture.md),
[Development process](../development.md), and [Code style](../code-style.md).

## Feature Areas

| Area | Modules |
| --- | --- |
| Animals | Catalog, Recognition |
| Parks | Catalog, Animals, Import, Connections |
| Discussions | Common, Rooms, Messages |
| Auth | Registration/login/refresh/revocation services and security dependencies |
| Users | Profile reads/updates |

Common contains shared animal Information/Registration and technical Language/Pagination/ErrorHandling modules.
It uses the same area/module structure as Features.

## Implemented Application Flows

These flows describe callable services and persistence, not existing HTTP endpoints.

```text
AnimalCatalogService
    ├─ external search/details → IAnimalInformationProvider (implementation pending)
    └─ local catalog → IAnimalCatalogDataSource

AnimalRecognitionService
    ├─ optional park context → IAnimalRecognitionDataSource
    └─ image + candidates → IAnimalRecognitionProvider (implementation pending)

Park species addition / import apply / explicit discussion creation
    → IAnimalRegistrationService
    → reuse or create shared Animal + General room
    → park operation upserts its ParkAnimal association

DiscussionMessageService
    → persist message through IDiscussionDataSource
    → publish DiscussionMessageEvent (implementation pending)
```

Search/recognition do not create cards or discussions. Shared registration returns actual Animal/General-room
IDs, including reuse. A shared card and room may exist outside every park.

Parks are active immediately, may have no species catalog, and do not depend on payment.
All new park operations are open in v1. Recognition uses published associations as optional context;
unknown and suspended parks are distinguished from an empty catalog.

## Detailed Documentation

- [Domain](domain.md): entities and relationships.
- [Application](application.md): ownership, service contracts, and scenario behavior.
- [Infrastructure](infrastructure.md): actual DataSource paths, persistence, and planned integrations.
- [API](api.md): scaffold status and transport requirements.
- [Completed park-model changes](../planning/domain-application-changes.md).
- [Remaining product/research work](../planning/parks-and-recognition-roadmap.md).
