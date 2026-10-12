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

## Business Modules

| Parent | Current child modules |
| --- | --- |
| Animals | Catalog, Recognition |
| Auth | Authentication |
| Parks | Catalog, Animals, Import, Connections |
| Discussions | Rooms, Messages |
| Users | Profiles |

Application roots are Modules, Common, and Shared.
Every business module can own purpose folders and a Modules folder for recursive children.
There is no fixed nesting depth and no requirement to create a child for each service.

Common/Animals owns shared animal information support and AnimalRegistrationService directly.
Shared owns technical Pagination, ErrorHandling, and Language components.
Parent-owned components remain close to their consumers: park sources/guards and discussion sources/account
checks serve multiple children without sibling imports.

Ordinary sibling modules are independent. Children use explicitly shared ancestor components;
parents coordinate children through public contracts. Common/Shared are shared exceptions, with no cycles.
The existing AuthService retains all its related operations inside Authentication; there is no separate Sessions service.

Infrastructure places implementations under Modules. Application/Common/<Module> merges into that same
Infrastructure module, while nested primary modules preserve their recursive path.
Shared DbContext/configurations/database cursors remain in Persistence.

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
