# Completed Domain and Application Changes for Parks

This stage is implemented in code: park models and services, shared card/chat registration,
recognition context, and the required EF persistence.
This does not establish API/UI readiness or working scenarios with real Wiki/ML providers.

Mandatory rules are defined in [Architecture](../architecture.md),
[Development process](../development.md), and [Code style](../code-style.md).
Detailed current descriptions are in [Domain](../backend/domain.md),
[Application](../backend/application.md), and [Infrastructure](../backend/infrastructure.md).
This document records the stage outcome rather than alternative instructions.

## Agreed Scope

- Ready pretrained models run locally only; training and fine-tuning are not planned.
- The Python service remains simple; configuration selects the model.
- The species list is optional. Without ParkId or published species, recognition uses general mode.
- New park, import, and connection operations are open to everyone without role/owner checks.
- A park is active immediately; a request and manual payment confirmation do not gate its use.
- Wiki remains the information/link source. Park descriptions supplement the shared card.
- Auth/Users and message authorship/moderation rules remain in place.
- Recognition history, individual-animal tracking, organizations, employees, and pricing plans were not added.

## Domain

Three entities and two enums were added:

| Type | Implemented purpose |
| --- | --- |
| Park | Name, Slug, Description, Address, Active/Suspended; optional OwnerUserAccountId metadata |
| ParkAnimal | Park/Animal association, LocalDescription, IsPublished |
| ParkConnectionRequest | Contact, comment, state, price/currency, and PaidAtUtc |
| ParkStatus | Active, Suspended |
| ParkConnectionStatus | Submitted, AwaitingPayment, Activated, Rejected, Cancelled |

Identifier, timestamp, and soft-deletion fields are inherited from IdEntity.
Additional LocalImageReference, PublicationStatus, ApplicantUserAccountId, and reviewer identifiers
were not included in the current entities.

Animal remains a shared sourced species card. Its ParkAnimals contains park associations;
Park.ParkAnimals contains associations to species cards. One species in two parks has two ParkAnimal records.
Park.ConnectionRequests contains that park's connection requests.

The card and General room are shared across parks and may exist without park associations.
Matching is scoped to language; no language-independent Species/Taxon model was introduced.

## Application

| Area/module | Implemented functionality |
| --- | --- |
| Parks/Catalog | Create, read/search, edit, and change status |
| Parks/Animals | Add/reuse species, local description, publication, and association removal |
| Parks/Import | Preview decoded rows and apply confirmed matches |
| Parks/Connections | Request submission, price approval, manual payment, rejection/cancellation |
| Common/Animals/Registration | Shared Animal + General registration, returning actual IDs |
| Common/Animals/Information | Information-provider contract, normalization, and validation |
| Animals/Recognition | Optional ParkId, published context, statuses, and provider-result validation |

Services, interfaces, and service contracts are grouped under Services/<Name>/.
Dependencies are grouped under Dependencies/<Name>/; data sources use flat DataSources folders.
Separate Interfaces and service Contracts directories were removed.

### Shared Registration

AnimalRegistrationService is used by Parks and Discussions.
IAnimalRegistrationDataSource creates or reuses the card together with its General room.
Matching checks source identity, scientific name, then normalized title within the same language.
Ambiguous matches and conflicting scientific names are not merged automatically.
AnimalRegistrationResult returns AnimalId and GeneralRoomId.

HasStartedDiscussion remains derived from LocalAnimalId: a saved card has a room even if it has no messages.
Removing ParkAnimal does not remove the shared Animal or discussion.

### Recognition Context

IAnimalRecognitionDataSource and ParkRecognitionContextResult belong to
Features/Animals/Recognition/DataSources. There is no shared ParkInformation module.

An existing empty catalog is distinguished from an unknown/suspended park.
The provider receives only published candidates. Returned IDs are checked against that context
and mapped to ParkAnimalId. An out-of-catalog species can be returned by name without a park link.

Recognition does not save a card, photograph, or new park association.
The current contract includes Recognized/Uncertain/NoAnimal, alternatives, and optional
ModelId/Revision/InferenceMilliseconds. Confidence scores and context hash/version have not been added.

### Import and Connection

Import accepts up to 200 already decoded rows. No CSV parser exists.
Preview performs no writes; apply validates selected matches again.
The ParkAnimal batch is atomic, but previously registered Animal/General records may remain
if subsequent association application fails.

Repeated additions/imports reuse associations. Price and manual payment change the request, not park status.
Closed requests cannot be reopened; transitions use conditional writes against the expected state.

## Persistence

Nine DataSources, the DbContext, and configurations for nine entities are implemented.
Implementation paths mirror the interface owner:

```text
Persistence/DataSources/Features/<Area>/<Module>/
Persistence/DataSources/Common/<Area>/<Module>/
```

Auth and Users have no additional module level. Discussion sources belong to
Features/Discussions/Common; this Common is local to discussions.

Migrations and the snapshot were removed; this stage did not initialize the live schema.
One Initial migration follows model agreement.

## Remaining Work

- A Wiki provider and correct source-URL retrieval.
- A local Python service with a ready model and a .NET HTTP adapter.
- Security/event implementations, runtime DI, and API endpoints.
- Actual upload content/size validation and CSV decoding.
- Angular UI, deployment configuration, and the future Initial migration.
- Automated checks at the final stage and separate research runs.

The code was verified by building. Provider integration and concurrent SQL Server behavior
are not considered verified by compilation alone.
The remaining sequence is described in the [roadmap](parks-and-recognition-roadmap.md).
