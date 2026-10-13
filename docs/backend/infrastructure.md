# Infrastructure Layer

`ZooFinder.Infrastructure` implements Application data sources and technical dependencies.

**Status:** EF persistence and Auth security implementations are implemented. Auth registration belongs to API;
Wiki/Python providers, event publishing, and complete runtime composition are not implemented. Global boundaries are in
[General architecture](../architecture.md). Placement rules for this layer are defined below;
tests and migration policy are in [Development rules](../development.md).

## Implementation Placement Rules

Infrastructure has Persistence and Modules. There is no separate Common root.
Dependency-injection registration belongs to the API composition root; Infrastructure contains implementations
and their technical settings, not IServiceCollection registration helpers.

Map an Application interface under Modules to the same recursive path in Infrastructure.
Map Application/Common/<Module>/... to Infrastructure/Modules/<Module>/...
and merge matching directories without overwriting conflicting files.

```text
Application/Common/Animals/DataSources/
    → Infrastructure/Modules/Animals/DataSources/

Application/Common/Animals/Dependencies/AnimalInformation/
    → Infrastructure/Modules/Animals/Dependencies/AnimalInformation/

Application/Modules/Animals/Modules/Recognition/DataSources/
    → Infrastructure/Modules/Animals/Modules/Recognition/DataSources/
```

The interface's Application owner determines business responsibility.
Physical merging in Infrastructure does not permit sibling dependencies in Application.
Keep DataSources flat and dependency implementations under Dependencies/<Name>.
Application services and their contracts remain in Application.

Shared technical Application components generally need no Infrastructure counterpart.
DbContext, its factory, EF configurations, and database cursors stay under Persistence.
Preserve visibility and transaction rules when reading/writing through sources.

## Current Structure

```text
ZooFinder.Infrastructure/
├─ Modules/
│  ├─ Animals/
│  │  ├─ DataSources/AnimalRegistrationDataSource.cs
│  │  └─ Modules/
│  │     ├─ Catalog/DataSources/AnimalCatalogDataSource.cs
│  │     └─ Recognition/DataSources/AnimalRecognitionDataSource.cs
│  ├─ Auth/
│  │  ├─ DataSources/AuthDataSource.cs
│  │  └─ Dependencies/
│  │     ├─ AccessToken/
│  │     │  ├─ JwtAccessTokenProvider.cs
│  │     │  ├─ JwtAccessTokenSettings.cs
│  │     │  └─ JwtAccessTokenSettingsValidator.cs
│  │     ├─ PasswordHashing/IdentityPasswordHasher.cs
│  │     ├─ RefreshTokenGeneration/CryptographicRefreshTokenGenerator.cs
│  │     └─ RefreshTokenHashing/Sha256RefreshTokenHasher.cs
│  ├─ Parks/
│  │  ├─ DataSources/
│  │  │  ├─ ParkCatalogDataSource.cs
│  │  │  └─ ParkAnimalDataSource.cs
│  │  └─ Modules/
│  │     └─ Connections/DataSources/ParkConnectionDataSource.cs
│  ├─ Discussions/
│  │  └─ DataSources/DiscussionDataSource.cs
│  └─ Users/
│     └─ Modules/
│        └─ Profiles/DataSources/UserProfileDataSource.cs
└─ Persistence/
   ├─ ZooFinderDbContext.cs
   ├─ ZooFinderDbContextFactory.cs
   ├─ Configurations/
   └─ Pagination/DatabaseCursor.cs
```

Recognition reads park data through its own source without importing the Parks module.
Shared registration maps to the Animals parent while catalog/recognition sources keep their child owners.
Parent-level park/discussion/auth sources stay at their corresponding parent.
Namespaces match placement; sources explicitly import the shared Persistence context.

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
| `IPasswordHasher` | `IdentityPasswordHasher` | Implemented |
| `IRefreshTokenGenerator` | `CryptographicRefreshTokenGenerator` | Implemented |
| `IRefreshTokenHasher` | `Sha256RefreshTokenHasher` | Implemented |
| `IAccessTokenProvider` | `JwtAccessTokenProvider` | Implemented |
| `IDiscussionEventPublisher` | Discussion event transport | Pending |

Provider/security/event implementations follow the same merged-path rule under Dependencies.
Examples are Modules/Animals/Modules/Recognition/Dependencies/AnimalRecognition,
Modules/Animals/Dependencies/AnimalInformation, and Modules/Auth/Dependencies/PasswordHashing.
Provider/event implementations and directories are not present yet; do not create empty scaffolding for them.
HTTP models and provider settings belong to the concrete integration; Application contracts remain in Application.
The Python model is selected inside Python, without requiring a separate .NET provider for each model.

API owns dependency-injection registration. No business DI helpers are implemented;
registration is deferred until API development. Program remains a scaffold.
The concrete SignalR/Hub placement and transport composition must be resolved during that implementation;
Infrastructure must not reference Api.

## Persistence and Provider Assumptions

Sources and context use EF Core. SQL Server is selected by the design-time factory and referenced by the project.
Runtime context registration is still pending. The unfinished-connection unique index uses a SQL Server filter;
changing providers requires reviewing index syntax, null handling, comparison behavior, and transactions.

Infrastructure owns mappings, indexes, transactions, filtering, sources, and the future Initial migration.
No general-purpose repository base class or UnitOfWork is introduced.

Business reads use `AsNoTracking`; write methods query tracked entities and assign only fields owned by that operation.
All data-source writes go through `SaveChangesAsync`. There are no direct ExecuteUpdate/ExecuteDelete writes.
Data sources depend on DbContext; timestamps are centralized in DbContext rather than supplied by a second clock dependency.

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

Normal updates preserve deletion fields. `Restore(...)` accepts a tracked, previously deleted entity;
SaveChanges recognizes its deleted-to-active transition, clears deletion time, and preserves ID/creation time.
Deleting a tracked entity still uses EF's Deleted state and is converted to a soft-delete update.

`IsDeleted` is an EF concurrency token for all entities. Refresh sessions additionally use their hash,
revocation time, and expiration time; connection requests use their status. These are existing fields, not new
version columns, and configuring their concurrency role does not require a new database schema.

Global filters hide deleted rows. Related visibility filters hide profiles/sessions of deleted accounts,
rooms/messages of deleted animals or rooms, memberships of deleted parks/animals, and requests of deleted parks.
Filtering does not modify or cascade-delete child rows. Unique identities remain reserved.

Membership addition creates a missing association or restores a deleted one with its original ID and the supplied
local description/publication. Existing active associations are returned unchanged; UpdateAsync edits their local fields.
It does not delete, recreate, or edit the shared animal or room. Recognition context excludes unpublished links;
Application rejects a suspended park after reading its status.

History queries bypass filters to retain deleted messages and author information, then reapply room/animal
visibility. Application hides deleted content by returning null. Deleted authors do not erase history.

Write queries apply activity/visibility predicates before changing tracked rows. Auth, profile, message, and connection
writes use explicit transactions for related-row checks. Membership writes use the default SaveChanges transaction;
their prior park/animal checks do not lock parent rows until saving. A parent can be soft-deleted after the check,
while a membership write still succeeds; global filters then hide that association. A child concurrency token does
not protect its parents.
Sources do not use physical ExecuteDelete or raw SQL deletes.

## Atomic Operations and Concurrency

- Account registration stores account, profile, and initial session in one SaveChanges transaction.
  After a write conflict, it checks for a competing normalized login,
  including deleted accounts. Only a confirmed duplicate returns false; other database errors are rethrown.
- Refresh rotation queries the expected hash while checking session expiry, revocation, deletion, and account state
  inside RepeatableRead, then changes the tracked session and saves. Concurrency conflicts return false;
  competing rotations cannot both successfully save the same expected hash. Other storage errors are rethrown.
  Session revocation also uses tracked writes; revoke-all uses Serializable to protect the selected account's session range.
- Shared card/room registration uses a serializable transaction. One query matches only
  `InformationSource + SourceItemId + LanguageCode` and loads the card's rooms, including deleted records.
  Deleted cards or General rooms produce a conflict. The operation returns an existing room, adds a missing
  room to the matched card, or creates both records. Storage errors end the operation; registration does not
  recover through a second lookup after a failed write.
- Park slug reservation uses serializable transactions. Membership addition validates the batch before link writes
  and loads canonical tracked Animals and existing links, including soft-deleted links. AddOrRestoreAsync delegates
  to AddOrRestoreBatchAsync: active links keep their local fields, deleted links are restored with the supplied values,
  and missing links are inserted. One SaveChanges transaction persists the entire link batch; results use those
  tracked instances in input order without a second result query. Failed saving rolls back the batch and ends the operation.
  Shared cards/rooms are registered beforehand in separate transactions and may survive a later membership failure.
  Membership writes do not start explicit transactions or request stronger isolation. The unique park/animal index
  rejects duplicate inserts, including concurrent additions; storage errors propagate without recovery or retries.
- Connection submission prevents two unfinished requests. Transitions query the expected state inside RepeatableRead
  and save the tracked request with status/deletion concurrency tokens, without changing park availability.
- Adding a session/message tracks only the new row, avoiding reinsertion of detached account/room graphs.
- Profile updates require an active account and change display name/update time.
  Profile/message updates query tracked rows inside RepeatableRead so related visibility/state checks
  remain valid until commit. Message deletion uses the common soft-delete handling; saved timestamps are copied
  back to the supplied message so published events use the persisted deletion time.
  Message updates cannot restore a concurrently deleted message or write to a closed room.

Read methods retain detached snapshots; write results from registration/add-or-restore may be tracked in the current context.
Sources do not attach detached navigation graphs for updates. Failures that end an operation do not manually detach
entities or clear the tracker; the context is disposed at the end of its scope. Current callers of failed Try writes
throw a business error and do not continue saving. Shared registration also stops on a failed write;
there is no recovery path that continues saving with failed inserts in the tracker.
RepeatableRead/Serializable may produce database deadlocks under contention. No automatic retry or provider-specific
deadlock translation is implemented; these failures remain storage errors rather than successful operations.

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

## Auth Security — Implemented

Security implementations use the existing Application ports without adding an Identity user store or changing
AuthService's registration, login, refresh rotation, or revocation scenarios.

- `IdentityPasswordHasher` wraps ASP.NET Core Identity's versioned password format: PBKDF2-HMAC-SHA512,
  220,000 iterations, a random 128-bit salt, and a 256-bit derived key. Identity performs constant-time hash
  comparison. Empty, malformed, and unsupported stored hashes fail verification. A successful legacy hash
  verification is accepted, but the current boolean Application port does not support automatic rehashing.
- `CryptographicRefreshTokenGenerator` produces 32 cryptographically random bytes encoded as unpadded Base64url.
- `Sha256RefreshTokenHasher` hashes the exact UTF-8 token to a 64-character uppercase SHA-256 hexadecimal value.
  AuthService owns input normalization; only the hash reaches persistence. Passwords use the slow salted hasher,
  while refresh tokens already contain 256 bits of random entropy.
- `JwtAccessTokenProvider` signs HS256 JWTs. Claims are `sub` (account ID), `role` (enum name), unique `jti`,
  numeric `iat`, `nbf`, `exp`, `iss`, and `aud`. UTC time comes from `TimeProvider`. Deleted/inactive accounts,
  empty account IDs, and unsupported roles cannot receive tokens.

`JwtAccessTokenSettings` belongs to Dependencies/AccessToken. Issuer and audience are required;
`SigningKeyBase64` must decode to at least 32 bytes. Supply a cryptographically random key through secure runtime
configuration, never a committed/default secret. Lifetime defaults to 15 minutes and must be between one second
and one hour. Invalid settings fail when the provider is constructed, including resolution through DI.

The future API composition root will register the four security implementations, their settings, `IAuthDataSource`,
`ZooFinderDbContext`, `AuthSettings`, `IAuthService`/`AuthService`, and `TimeProvider`.
Business DI helpers are deliberately deferred until API development. There is no runtime database connection
registration or schema initialization.

Bearer validation remains API work: require HS256, validate signature, issuer, audience, and lifetime using the same
key/settings; disable inbound claim mapping and use `sub`/`role` as the name/role claim types.
Revoking refresh sessions does not invalidate already issued access tokens; they remain valid until expiry.
HTTP authentication, controllers, and an end-to-end SQL Server Auth flow are not implemented or verified yet.

## Discussion Events — Pending

The publisher uses dependency-owned `DiscussionMessageEvent`, not service responses.
Message persistence and publishing are separate operations; no outbox or reliable-delivery implementation exists.
