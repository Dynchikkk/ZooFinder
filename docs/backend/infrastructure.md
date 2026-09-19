# Infrastructure Layer

`ZooFinder.Infrastructure` implements Application ports for persistence, external information, recognition, security, and discussion events.

> **Status:** Persistence is implemented. External providers, security implementations, and discussion-event publishing are still pending.

## Structure

```text
ZooFinder.Infrastructure/
├─ Persistence/
│  ├─ ZooFinderDbContext.cs
│  ├─ ZooFinderDbContextFactory.cs
│  ├─ Configurations/
│  ├─ Pagination/
│  ├─ Repositories/
│  └─ Migrations/
├─ AnimalInformation/
├─ Recognition/
└─ Security/
```

## Port Implementations

| Application port | Infrastructure implementation |
| --- | --- |
| `IAnimalCatalogRepository` | Entity Framework Core catalog repository |
| `IDiscussionRepository` | Entity Framework Core discussion repository |
| `IUserProfileRepository` | Entity Framework Core repository |
| `IAuthRepository` | Entity Framework Core authentication repository |
| `IAnimalInformationProvider` | MediaWiki provider |
| `IAnimalRecognitionProvider` | Ollama provider |
| `IAccessTokenProvider` | JWT provider |
| `IPasswordHasher` | Password hash implementation |
| `IRefreshTokenGenerator` | Cryptographic token generator |
| `IRefreshTokenHasher` | Refresh-token hash implementation |
| `IDiscussionEventPublisher` | SignalR publisher |

## Persistence

The context, entity configurations, and repositories use provider-neutral EF Core APIs. SQL Server is currently selected only by the design-time factory and its migrations. Runtime provider selection belongs to the API composition root.

Infrastructure owns:

- `DbContext`;
- entity configurations;
- indexes and constraints;
- repository implementations;
- migrations;
- transactions;
- soft-delete filtering.

Required database constraints:

| Entity | Constraint |
| --- | --- |
| `Animal` | Unique `(InformationSource, LanguageCode, SourceItemId)` |
| `DiscussionRoom` | Unique `(AnimalId, Name)` |
| `UserProfile` | Unique `UserAccountId` |
| `UserAccount` | Unique non-null `Login` |
| `UserRefreshSession` | Indexed `UserAccountId`; unique `RefreshTokenHash` |

`TryAddUserAccountAsync` atomically stores the account, profile, and initial refresh session. A duplicate normalized login returns `false`.

`TryRotateRefreshSessionAsync` uses a conditional update. It succeeds only when the stored hash equals the expected hash and the session is active at the supplied use time.

`GetOrCreateDiscussionAsync` uses the sourced-animal uniqueness constraint to atomically create the animal and its `General` room or return the existing room. Messages are stored by separate operations.

Application creates General rooms with the fixed name `General`, so the unique `(AnimalId, Name)` index protects this creation flow without a filtered index. The shared model does not enforce one room per type independently of its name. Business-input checks live in Application; the shared EF configurations do not contain SQL `CHECK` expressions.

Column types and database-specific index conventions are selected by the provider. Explicit collations are not configured in the shared model. Application normalizes logins before repository calls; other string comparisons follow the selected database's comparison rules. Changing providers requires reviewing those rules, null handling in unique indexes, and translation of LINQ/bulk-update queries; changing a connection string alone is not sufficient.

### Context and soft deletion

`ZooFinderDbContext` maps six separate tables. Relationships use `ClientNoAction`: neither EF nor the database cascades deletes or clears required foreign keys. Both synchronous and asynchronous `SaveChanges` convert tracked deletions to `IsDeleted = true`, set `DeletedAtUtc`, and update `UpdatedAtUtc`. Inserts receive creation and update timestamps from `TimeProvider`. Updates preserve the stored creation timestamp. Date converters write UTC values and restore UTC `DateTime.Kind` when reading.

Global filters hide deleted rows. Profiles and refresh sessions also hide rows belonging to deleted accounts; rooms and messages hide rows belonging to deleted animals or rooms. This is visibility filtering, not cascading modification of child rows. Unique identities remain reserved after soft deletion. Application exposes no operation for deleting General rooms; the shared model does not use a SQL check constraint to prevent direct deletion.

History queries explicitly bypass the filters to retain deleted messages and author profiles, then reapply room and animal visibility. Application replaces deleted message content with `null` in its response. A deleted author does not erase discussion history.

Bulk repository updates use `ExecuteUpdateAsync`, so they explicitly set audit timestamps and include visibility/activity predicates. Physical `ExecuteDelete` and raw SQL deletes bypass the context's soft-delete handling and are not used by repositories. See [EF Core query filters](https://learn.microsoft.com/en-us/ef/core/querying/filters) and [bulk updates](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete).

### Repository behavior

- Read operations use `AsNoTracking`. Profile reads do not load the account graph.
- Registration persists the account, profile, and initial session in one `SaveChanges` transaction. After `DbUpdateException`, the failed graph is detached and the repository checks for a competing account with the same login, including deleted accounts. A confirmed conflict returns `false`; without that competing account the exception is rethrown. No database-specific exception types or error numbers are inspected.
- Refresh rotation uses one conditional update, checking the expected hash, expiration, revocation, deletion, and current account status. Only one concurrent rotation can succeed.
- Discussion creation persists the animal and General room in one transaction. After `DbUpdateException`, the losing graph is detached and the repository looks up the requested discussion by animal identity. An existing room is returned; a deleted animal produces a conflict; otherwise the exception is rethrown.
- Adding a session or message tracks only the new row, so detached account/room navigations are not inserted again.
- Profile updates require an active account and update only the display name and timestamp. Message updates cannot restore a concurrently deleted message or write to a closed room.

Catalog results and message history are ordered by `CreatedAtUtc DESC, Id DESC`. Their versioned Base64 JSON cursors contain the last ordering key and a hash binding the cursor to the query (search term/language or discussion room). The cursor is not an authorization credential. Invalid or mismatched cursors produce `RequestValidationException`. Each query requests one extra row to determine whether a next page exists.

### Connection and migrations

Runtime registration of the context, `TimeProvider`, and repositories is deferred to the API composition root. Development configuration contains a local SQL Server connection string with Windows authentication under `ConnectionStrings:ZooFinder`. Other environments can supply it via `ConnectionStrings__ZooFinder`; credentials must not be committed.

The design-time factory reads the same environment variable and otherwise uses the local development connection. `InitialPersistence` creates the original SQL Server schema. `RemoveProviderSpecificConfiguration` removes the explicit collations, SQL check constraints, and filtered General-room index; the snapshot reflects the current shared model. Both migrations are generated for SQL Server and are not interchangeable with another provider's migrations. The application does not create or migrate the database on startup.

Run these commands from the repository root with the EF CLI available (EF Core packages use version `10.0.8`):

```powershell
# Review the migration SQL without connecting to a database.
dotnet ef migrations script --idempotent --project apps/backend/ZooFinder/ZooFinder.Infrastructure

# Apply to the database selected by ConnectionStrings__ZooFinder.
dotnet ef database update --project apps/backend/ZooFinder/ZooFinder.Infrastructure
```

## MediaWiki

The MediaWiki implementation:

- searches articles by normalized animal name;
- retrieves article details by source item identifier and language;
- returns title, scientific name, short description, preview image, and source URL;
- maps MediaWiki pagination data to the Application cursor;
- maps transport models to `AnimalInformationSearchResult` and `AnimalInformationDetailsResult`.

MediaWiki transport models remain internal to Infrastructure.

## Ollama

The Ollama implementation sends the uploaded image and a fixed structured prompt to a vision model. `qwen3-vl:2b` is the initial model.

The provider maps the model response to `AnimalRecognitionProviderResult`:

```text
IsAnimal
CommonName
ScientificName
Alternatives
```

Model name, service URL, timeout, and response limits are configuration values. Uploaded images are not persisted.

## Security

- Access tokens are short-lived JWTs.
- Refresh tokens are generated with a cryptographically secure random source.
- Only refresh-token hashes are persisted.
- Refresh-token rotation invalidates the previous token.
- Account role is included in authorization claims.
- Account status is checked for protected write operations.

## Dependency Registration

Infrastructure contains implementations without dependency-registration helpers. The API composition root will register the context, repositories, external providers, security services, and discussion-event publisher when API wiring is implemented. Runtime persistence registration is not configured yet.
