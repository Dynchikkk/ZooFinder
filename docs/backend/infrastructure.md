# Infrastructure Layer

`ZooFinder.Infrastructure` implements Application data sources and technical dependencies.

**Status:** EF persistence is implemented. Wiki/Python providers, security implementations, event publishing,
and runtime dependency registration are not implemented. Global boundaries are in
[General architecture](../architecture.md). Placement rules for this layer are defined below;
tests and migration policy are in [Development rules](../development.md).

## DataSource Placement Rules

Storage implementations belong to Persistence/DataSources/Features and Persistence/DataSources/Common.
Under each branch, repeat the Application interface owner's area/module hierarchy.
Files remain flat in final module folders. Ownership, rather than the queried tables, determines placement.

Preserve soft-deletion, publication, and scenario-specific visibility when reading.
Reuse repeated EF query details within Infrastructure when useful, without copying a complete foreign source.
This mapping does not impose Application's FSD structure on the rest of Infrastructure.
DbContext, entity configurations, cursors, and integration implementations follow their technical responsibility.

## Current Structure

```text
ZooFinder.Infrastructure/
└─ Persistence/
   ├─ ZooFinderDbContext.cs
   ├─ ZooFinderDbContextFactory.cs
   ├─ Configurations/
   ├─ Pagination/DatabaseCursor.cs
   └─ DataSources/
      ├─ Features/
      │  ├─ Animals/
      │  │  ├─ Catalog/AnimalCatalogDataSource.cs
      │  │  └─ Recognition/AnimalRecognitionDataSource.cs
      │  ├─ Parks/
      │  │  ├─ Catalog/ParkCatalogDataSource.cs
      │  │  ├─ Animals/ParkAnimalDataSource.cs
      │  │  └─ Connections/ParkConnectionDataSource.cs
      │  ├─ Discussions/
      │  │  └─ Common/DiscussionDataSource.cs
      │  ├─ Auth/AuthDataSource.cs
      │  └─ Users/UserProfileDataSource.cs
      └─ Common/
         └─ Animals/
            └─ Registration/AnimalRegistrationDataSource.cs
```

The DataSource path repeats the area/module of the Application interface owner. Recognition's source reads
parks and memberships but belongs to Animals/Recognition. Common registration belongs to Common/Animals/Registration.
Files remain flat in each final module folder.

Configurations map Domain entities and remain under `Persistence/Configurations`.
The context and cursor implementation are shared persistence infrastructure.

## Implementations

| Application interface | Implementation | Status |
| --- | --- | --- |
| `IAnimalCatalogDataSource` | `AnimalCatalogDataSource` | Implemented |
| `IAnimalRecognitionDataSource` | `AnimalRecognitionDataSource` | Implemented |
| `IParkCatalogDataSource` | `ParkCatalogDataSource` | Implemented |
| `IParkAnimalDataSource` | `ParkAnimalDataSource` | Implemented |
| `IParkConnectionDataSource` | `ParkConnectionDataSource` | Implemented |
| `IDiscussionDataSource` | `DiscussionDataSource` | Implemented |
| `IAuthDataSource` | `AuthDataSource` | Implemented |
| `IUserProfileDataSource` | `UserProfileDataSource` | Implemented |
| `IAnimalRegistrationDataSource` | `AnimalRegistrationDataSource` | Implemented |
| `IAnimalInformationProvider` | Wiki adapter | Pending |
| `IAnimalRecognitionProvider` | HTTP adapter to local Python | Pending |
| Auth hashing/token interfaces | Security implementations | Pending |
| `IDiscussionEventPublisher` | Discussion event transport | Pending |

Future provider implementations can mirror their Application owners under Infrastructure `Features` and
`Common`, using area/module/dependency/component grouping. These branches are not present yet.
HTTP models and provider settings belong to the concrete integration; Application contracts remain in Application.
The Python model is selected inside Python, without requiring a separate .NET provider for each model.

A future `AddInfrastructure(...)` registration helper is planned. It has not been added; API is still a scaffold.
The concrete SignalR/Hub placement and transport composition must be resolved during that implementation;
Infrastructure must not reference Api.

## Persistence and Provider Assumptions

Sources and context use EF Core. SQL Server is selected by the design-time factory and referenced by the project.
Runtime context registration is still pending. The unfinished-connection unique index uses a SQL Server filter;
changing providers requires reviewing index syntax, null handling, comparison behavior, and transactions.

Infrastructure owns mappings, indexes, transactions, filtering, sources, and the future Initial migration.
No general-purpose repository base class or UnitOfWork is introduced.

| Entity | Constraint |
| --- | --- |
| `Animal` | Unique `(InformationSource, LanguageCode, SourceItemId)` |
| `DiscussionRoom` | Unique `(AnimalId, Name)` |
| `UserProfile` | Unique `UserAccountId` |
| `UserAccount` | Unique non-null `Login` |
| `UserRefreshSession` | Indexed `UserAccountId`; unique `RefreshTokenHash` |
| `Park` | Unique `Slug`, including deleted parks |
| `ParkAnimal` | Unique `(ParkId, AnimalId)`, including deleted associations |
| `ParkConnectionRequest` | Unique `ParkId` where not deleted and status is Submitted/AwaitingPayment |

Application creates General rooms with the fixed name `General`; the unique animal/name index protects that flow.
The database does not independently enforce one room per type or prohibit direct General-room deletion.
Business-input validation lives in Application; mappings do not add SQL CHECK expressions.
Explicit collations are not configured, so other string comparisons follow the database's rules.
Application normalizes logins before storage calls.

## Context and Visibility

`ZooFinderDbContext` maps nine tables. Relationships use `ClientNoAction`: deletes do not cascade through
the graph or clear required foreign keys. Both synchronous and asynchronous SaveChanges convert tracked
deletions to soft deletion, set deletion/update times, and use `TimeProvider` for UTC timestamps.
Inserts receive creation/update timestamps; normal updates preserve creation time.
Converters write UTC and restore `DateTime.Kind` when reading.

Global filters hide deleted rows. Related visibility filters hide profiles/sessions of deleted accounts,
rooms/messages of deleted animals or rooms, memberships of deleted parks/animals, and requests of deleted parks.
Filtering does not modify or cascade-delete child rows. Unique identities remain reserved.

Membership upsert restores a deleted association with its original ID and changes local description/publication.
It does not delete, recreate, or edit the shared animal or room. Recognition context excludes unpublished links;
Application rejects a suspended park after reading its status.

History queries bypass filters to retain deleted messages and author information, then reapply room/animal
visibility. Application hides deleted content by returning null. Deleted authors do not erase history.

Bulk updates explicitly apply activity/visibility predicates and timestamps because they bypass SaveChanges.
Sources do not use physical ExecuteDelete or raw SQL deletes.

## Atomic Operations and Concurrency

- Account registration stores account, profile, and initial session in one SaveChanges transaction.
  After a write conflict, it detaches the failed graph and checks for a competing normalized login,
  including deleted accounts. Only a confirmed duplicate returns false; other database errors are rethrown.
- Refresh rotation conditionally updates the expected hash while checking session expiry, revocation,
  deletion, and account state. Competing rotations cannot both update the same expected hash.
- Shared card/room registration uses a serializable transaction. Matching checks source identity,
  scientific name, then normalized title within a language. Ambiguous matches, conflicting scientific names,
  or deleted canonical records produce a conflict. After DbUpdateException, an existing exact sourced room
  can be returned; other failures are rethrown.
- Park slug reservation and membership batches use serializable transactions. Batch members are validated
  before link writes. Shared cards/rooms are registered beforehand in separate transactions and may survive
  a later membership failure.
- Connection submission prevents two unfinished requests. Transitions conditionally update the expected state
  without changing park availability.
- Adding a session/message tracks only the new row, avoiding reinsertion of detached account/room graphs.
- Profile updates require an active account and change display name/update time.
  Message updates cannot restore a concurrently deleted message or write to a closed room.

These behaviors describe implementation, not completed SQL Server concurrency testing.
Such testing belongs to the final verification stage.

## Pagination

Local catalog and message history order by `CreatedAtUtc DESC, Id DESC`.
Versioned Base64 JSON cursors contain the last key and a hash binding the cursor to search term/language or room.
They are continuation values, not authorization credentials.

Invalid or mismatched cursors produce `RequestValidationException`. Queries fetch one extra row to determine
whether another page exists. External Wiki cursors will be mapped independently by that provider.

## Connection and Migrations

Development settings contain a local Windows-authenticated SQL Server connection under
`ConnectionStrings:ZooFinder`. The factory reads `ConnectionStrings__ZooFinder`, otherwise using the local
development connection. Runtime configuration must be wired when the API composition root is implemented.

Previous migrations and snapshot were removed. Do not create replacements while the schema is still forming.
Keep the context, configurations, sources, and factory; after model agreement, create one `Initial` migration.
The application currently neither initializes nor migrates the database on startup.

The following commands are for that later stage only, from the repository root with the EF CLI installed:

```powershell
dotnet ef migrations add Initial --project apps/backend/ZooFinder/ZooFinder.Infrastructure

dotnet ef migrations script --idempotent --project apps/backend/ZooFinder/ZooFinder.Infrastructure

dotnet ef database update --project apps/backend/ZooFinder/ZooFinder.Infrastructure
```

Review generated SQL before applying it. Do not commit database credentials.

## Wiki Integration — Pending

The planned provider searches articles and retrieves title, scientific name, description, preview image,
and source URL using source identity/language. Transport DTOs remain internal to Infrastructure.
Results map to `AnimalInformationSearchResult` and `AnimalInformationDetailsResult`.

Transport unavailability must map to `AnimalInformationUnavailableException` for park-card cached fallback.
Cancellation and invalid/programming results must remain distinct from unavailable information.
A concrete provider, transport settings, caching, and runtime registration are not implemented.

## Python Recognition — Pending

The .NET HTTP adapter will send an image, language, and optional park candidates to a small local Python service.
Python loads ready pretrained weights and selects the model through configuration; no training or fine-tuning.
Python has no direct access to the ZooFinder database and does not retrieve Wiki information.

The existing Application result contains:

```text
Status: Recognized / Uncertain / NoAnimal
CommonName
ScientificName
Alternatives
AnimalId: optional, only from supplied context
Execution: optional ModelId / Revision / InferenceMilliseconds
```

The HTTP wire schema, service URL/timeouts, error mapping, and concrete model adapters remain to be implemented.
Images, recognition history, and experiment tables are not persisted.
Research plans and proposed extensions are documented separately in the [roadmap](../planning/parks-and-recognition-roadmap.md).

## Security and Events — Pending

Application already defines hashing, token generation, access-token production, and event publishing ports.
Planned security behavior includes short-lived bearer access tokens, cryptographic refresh tokens,
hash-only refresh-token storage, and rotation. No concrete JWT/hash implementation is present.

The publisher uses dependency-owned `DiscussionMessageEvent`, not service responses.
Message persistence and publishing are separate operations; no outbox or reliable-delivery implementation exists.
