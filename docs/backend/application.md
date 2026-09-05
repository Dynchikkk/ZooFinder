# Application Layer

`ZooFinder.Application` defines use cases and the ports required to execute them. It depends only on `ZooFinder.Domain`.

## Structure

```text
ZooFinder.Application/
├─ Common/
│  ├─ Constants/
│  ├─ Contracts/
│  ├─ Exceptions/
│  ├─ Extensions/
│  └─ Interfaces/
└─ Features/
   ├─ Animals/
   │  ├─ Common/
   │  ├─ Catalog/
   │  └─ Recognition/
   ├─ Users/
   ├─ Auth/
   └─ Discussions/
```

Each use case contains only the required `Contracts`, `Interfaces`, `Mappers`, `Services`, and validators.

Feature-specific shared code belongs to `Features/<Feature>/Common`. Code shared by multiple top-level features belongs to `Application/Common`.

## Conventions

- Application services coordinate use cases.
- Infrastructure implements repository and provider interfaces.
- I/O operations are asynchronous and accept `CancellationToken`.
- Requests entering a use case use the `Request` suffix.
- Responses leaving a use case use the `Response` suffix.
- Results returned by internal providers use the `Result` suffix.
- Immutable contracts are records.
- Mappers contain contract and entity transformations.
- Services do not call neighboring feature services.

## Pagination

### Offset pagination

Offset pagination is zero-based.

```text
FirstPage = 0
DefaultPageSize = 20
MaximumPageSize = 100
```

Contracts:

```text
OffsetPageRequest
OffsetPageResponse<T>
```

### Cursor pagination

A cursor is an opaque continuation value. Its internal representation belongs to the data source.

Contracts:

```text
CursorPageRequest
CursorPageResponse<T>
```

Search results and message history use cursor pagination.

## Errors

| Exception | Meaning |
| --- | --- |
| `RequestValidationException` | Invalid use-case input |
| `NotFoundException` | Requested resource was not found |

The API maps Application exceptions to HTTP responses.

## Animals

### Common

`IAnimalInformationService` provides external animal information:

```text
SearchAsync
GetDetailsAsync
```

Its contracts use provider-neutral identity:

```text
InformationSource
SourceItemId
LanguageCode
```

`IAnimalRepository` provides local animal operations:

```text
GetByIdAsync
GetBySourceItemAsync
SearchAsync
AddAsync
UpdateAsync
```

Supported language codes are `en` and `ru`.

### Catalog

`IAnimalCatalogService` provides:

```text
SearchAsync(AnimalSearchRequest)
GetAnimalAsync(AnimalPageRequest)
```

Search scopes:

| Scope | Source |
| --- | --- |
| `ExternalCatalog` | `IAnimalInformationService` |
| `LocalCatalog` | `IAnimalRepository` |

Search returns compact `AnimalCardResponse` items. Animal retrieval returns `AnimalPageResponse` with full provider information.

`LocalAnimalId` identifies a locally persisted animal. `HasStartedDiscussion` is derived from its presence.

Catalog operations do not persist animals or rooms.

### Recognition

```text
AnimalRecognitionRequest
    → IAnimalRecognitionService
    → IAnimalRecognitionProvider
    → AnimalRecognitionProviderResult
    → AnimalRecognitionResponse
```

The request contains a stream, file name, content type, declared length, and language code.

The response contains:

- whether an animal was recognized;
- common name;
- scientific name;
- alternative candidates.

Provider result and service response are separate contracts. Candidate records are declared in the same files as their parent contracts.

Recognition validates the stream, file name, declared length, language, and provider result.

> **Status:** File-format validation is in progress and may change. It must inspect the file contents instead of trusting the declared content type.

Recognition does not call Catalog and does not persist the image. The client uses the returned name in a separate catalog search.

## Users

> **Status:** In progress. This section may change.

`IUserService` provides:

```text
GetPublicProfileAsync
GetCurrentProfileAsync
UpdateProfileAsync
```

Contracts:

```text
UserProfileResponse
UpdateUserProfileRequest
```

Profile reads select profile data without requiring the full account graph. Profile updates modify `DisplayName` for the authenticated account.

## Authentication

> **Status:** In progress. This section may change.

`IAuthService` provides:

```text
RegisterByNameAsync
RefreshSessionAsync
RevokeSessionAsync
RevokeAllSessionsAsync
```

Registration creates `UserAccount`, `UserProfile`, and `UserRefreshSession` in one transaction.

Security ports:

```text
IAccessTokenProvider
IRefreshTokenGenerator
IRefreshTokenHasher
```

Repository ports shared by Users and Auth belong to:

```text
Common/Interfaces/Repositories/
├─ IUserAccountRepository.cs
├─ IUserProfileRepository.cs
└─ IUserRefreshSessionRepository.cs
```

Refresh-token rotation replaces the previous token hash. A user may revoke one session or all sessions.

## Discussions

> **Status:** In progress. This section may change.

Discussions are divided into shared contracts, rooms, and messages:

```text
Features/Discussions/
├─ Common/
├─ Rooms/
└─ Messages/
```

Use cases:

- retrieve an animal's `General` room;
- retrieve cursor-paginated message history;
- send a message;
- edit a message;
- soft-delete a message.

The first message for an external animal creates the local `Animal`, its `General` room, and the message in one transaction.

Discussion writes verify the account status, room state, message content, and author permissions.

Real-time delivery uses an `IDiscussionEventPublisher` port. SignalR remains outside Application.

## Transactions and Registration

> **Status:** In progress. This section may change.

`IUnitOfWork` defines the transaction boundary for multi-entity operations.

`DependencyInjection.cs` registers Application services. Infrastructure registers repository, provider, security, and transaction implementations.
