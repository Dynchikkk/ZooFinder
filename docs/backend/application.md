# Application Layer

`ZooFinder.Application` defines use cases and the ports required to execute them. It depends only on `ZooFinder.Domain`.

## Structure

```text
ZooFinder.Application/
├─ Common/
│  ├─ AnimalInformation/
│  │  ├─ Constants/
│  │  ├─ Contracts/
│  │  ├─ Extensions/
│  │  ├─ Interfaces/
│  │  └─ Validators/
│  ├─ ErrorHandling/
│  │  └─ Exceptions/
│  ├─ Language/
│  │  ├─ Constants/
│  │  └─ Validators/
│  └─ Pagination/
│     ├─ Contracts/
│     └─ Extensions/
└─ Features/
   ├─ Animals/
   │  ├─ Catalog/
   │  └─ Recognition/
   ├─ Users/
   ├─ Auth/
   └─ Discussions/
```

A directory is either a container or a module.

- A container groups modules and contains no C# files. `Common`, `Features`, and `Features/Animals` are containers.
- A module contains code and uses only the standard role directories listed below.
- Code is placed in its role directory even when that directory contains one file.
- A module does not introduce arbitrary role-directory names.

Standard module directories:

| Directory | Contents |
| --- | --- |
| `Constants` | Named constant values |
| `Contracts` | Requests, responses, results, and contract enums |
| `Exceptions` | Module-specific exception types |
| `Extensions` | Extension methods |
| `Interfaces` | Service, repository, provider, and publisher interfaces |
| `Services` | Application service implementations |
| `Settings` | Application-level settings contracts |
| `Validators` | Input and contract validation |

Directories outside this list require an architecture decision and an update to this document.

Feature-specific shared code belongs to a named module under the nearest feature container. Code shared by different top-level features belongs to a named module under `Application/Common`.

## Conventions

- Application services coordinate use cases.
- Infrastructure implements repository and provider interfaces.
- I/O operations are asynchronous and accept `CancellationToken`.
- Requests entering a use case use the `Request` suffix.
- Responses leaving a use case use the `Response` suffix.
- Results returned by internal providers use the `Result` suffix.
- Immutable contracts are records.
- Services create response contracts directly at the return site.
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
| `ConflictException` | Requested operation conflicts with existing state |
| `UnauthorizedException` | Authentication credentials or session are invalid |
| `ForbiddenException` | Authenticated account cannot perform the operation |

The API maps Application exceptions to HTTP responses.

## Animals

### Shared animal information

Animal-information contracts, normalization, and language validation belong to global `Application/Common`.

`IAnimalInformationProvider` and its result contracts belong to global `Application/Common` because both Animals and Discussions use the external animal-information boundary.

`IAnimalInformationProvider` provides:

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

`IAnimalCatalogRepository` belongs to Catalog and provides its local read operations:

```text
GetBySourceItemAsync
SearchAsync
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
| `ExternalCatalog` | `IAnimalInformationProvider` |
| `LocalCatalog` | `IAnimalCatalogRepository` |

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

`IUserProfileService` provides:

```text
GetProfileAsync
UpdateProfileAsync
```

Contracts:

```text
UserProfileResponse
UpdateUserProfileRequest
```

`IUserProfileRepository` provides:

```text
GetByUserAccountIdAsync
UpdateAsync
```

Public-profile and current-profile endpoints use the same read operation while their response data is identical. Profile reads select profile data without requiring the full account graph. Profile updates modify `DisplayName` for the authenticated account. A normalized display name contains between 1 and 100 characters.

## Authentication

`IAuthService` provides:

```text
RegisterAsync
LoginAsync
RefreshSessionAsync
RevokeSessionAsync
RevokeAllSessionsAsync
```

Contracts:

```text
RegisterRequest
LoginRequest
RefreshSessionRequest
RevokeSessionRequest
AuthResponse
```

`AuthResponse` contains the account ID, access token, refresh token, and refresh-session expiration time.

Security ports owned by Auth:

```text
IAccessTokenProvider
IPasswordHasher
IRefreshTokenGenerator
IRefreshTokenHasher
```

`IAuthRepository` provides:

```text
IsLoginTakenAsync
GetUserAccountByLoginAsync
AddUserAccountAsync
AddRefreshSessionAsync
GetRefreshSessionWithUserAccountByTokenHashAsync
UpdateRefreshSessionAsync
RevokeAllRefreshSessionsAsync
```

Registration accepts a login and password. It creates an active account with the `User` role, a profile, and the first refresh session. The initial display name equals the normalized login and may later be changed through Users. Logins are normalized to lowercase and must be unique. Passwords are passed to `IPasswordHasher`; only the resulting hash is persisted.

Login verifies the password hash and creates a separate refresh session. Invalid credentials and inactive accounts produce the same authentication error.

Only a refresh-token hash is persisted. Refreshing a session validates the session and account, replaces the token hash, extends the expiration time, and records the last-use time. A user may revoke one session by refresh token or all sessions by account ID.

`AuthSettings.RefreshSessionLifetime` defines the refresh-session lifetime. `TimeProvider` supplies the current UTC time.

## Discussions

Discussions are divided into shared persistence, rooms, and messages:

```text
Features/Discussions/
├─ Common/
│  ├─ Interfaces/
│  └─ Validators/
├─ Rooms/
│  ├─ Constants/
│  ├─ Contracts/
│  ├─ Interfaces/
│  ├─ Services/
│  └─ Validators/
└─ Messages/
   ├─ Constants/
   ├─ Contracts/
   ├─ Interfaces/
   ├─ Services/
   └─ Validators/
```

`IDiscussionRoomService` provides:

```text
GetGeneralRoomAsync
CreateDiscussionAsync
```

`GetGeneralRoomAsync` returns the persisted animal's General room or `null` when no discussion exists.

`CreateDiscussionAsync` is idempotent by sourced-animal identity. It returns an existing General room or obtains current animal information and creates the local animal and room through one repository operation. An empty General room is valid until the client sends the first message.

`IDiscussionMessageService` provides:

```text
GetMessagesAsync
SendMessageAsync
EditMessageAsync
DeleteMessageAsync
```

Message contracts:

```text
MessageHistoryRequest
SendMessageRequest
EditMessageRequest
DeleteMessageRequest
DiscussionMessageResponse
```

Message history uses cursor pagination. Deleted messages remain in history without their content. Message content contains between 1 and 4,000 characters after normalization.

`IDiscussionRepository` provides the persistence operations shared by Rooms and Messages. `GetGeneralRoomByAnimalSourceAsync` resolves an existing discussion by the provider-neutral animal identity. `AddDiscussionAsync` persists the animal and General room atomically.

Creating a discussion requires an active account. Message writes additionally require an open room. A message may be edited or soft-deleted by its author, a moderator, or an administrator.

The Messages-owned `IDiscussionEventPublisher` publishes created, updated, and deleted message events after persistence. SignalR remains outside Application.

## Transactions and Registration

> **Status:** In progress. This section may change.

`IUnitOfWork` defines the transaction boundary for multi-entity operations.

`DependencyInjection.cs` registers Application services. Infrastructure registers repository, provider, security, and transaction implementations.
