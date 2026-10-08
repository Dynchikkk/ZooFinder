# Application Layer

`ZooFinder.Application` contains use cases, validation, contracts, and the interfaces required to execute them.
It depends on Domain and has no EF, HTTP, SignalR, or dependency-injection framework dependency.

**Status:** Eleven services and their contracts are implemented. Infrastructure providers and API wiring
are still pending. Global layer boundaries are defined in [General architecture](../architecture.md).
Application-specific placement and cross-feature rules are defined below; formatting is defined in
[Code style](../code-style.md).

## Application Organization Rules

The feature-based (FSD) organization described here applies to Application.
It is not a required folder structure for Domain, Infrastructure, API, or frontend.

### Features, Modules, and Common

Top-level features are Animals, Parks, Discussions, Auth, and Users.
Modules inside one feature may share its sources, contracts, and rules. Feature-local shared functionality
stays under that feature; it does not move to global Common.

Features and Common use the same internal structure. Where an area has modules, placement follows
area/module/purpose/component. An area that already serves as a standalone module needs no artificial
extra module. Area/module roots contain purpose folders, not individual C# files.

Module names are brief in their area's context and do not repeat its prefix. Component names remain specific.
Do not change the agreed structure only to remove repeated words from a path.

Common contains stable functionality genuinely used by multiple features, including technical modules
and named subject modules. A single query against another feature's data does not justify a Common module.
Do not move entire features or private DTOs there.

### Names and Component Placement

Namespaces match file placement. Entity-based folder/namespace segments are plural, while entity and service
class names remain singular. Task names remain unchanged. Resolve namespace/type collisions through naming,
not aliases such as AnimalEntity.

Each service belongs to Services/<Name>/ with its interface, implementation, and <Name>ServiceContracts.cs.
The folder name omits the Service suffix. Related requests, responses, and service types share that contracts file.

Storage interfaces have the DataSource suffix. They serve a coherent scenario, may handle several Domain entities,
and are not created per table, per method, or automatically per service.
If an existing source fits a new operation, extend it. Separate sources are appropriate for separate tasks.

DataSources folders are flat. Optional <Name>DataSourceContracts.cs sits beside the interface.
Use suitable Domain entities rather than unnecessary DTOs; do not create empty contract files.

Providers, hashing, token generation, and publishers belong to Dependencies/<Name>/.
Each folder holds the interface and its own contracts when needed. Their technical implementations remain
outside Application. Recognition/information providers retain the Provider suffix.

Provider/publisher contracts belong to the dependency; it must not import service requests/responses.
Shared types are defined once according to their purpose. Constants, validators, settings, exceptions,
and extensions stay in their module's purpose folders.
Common pagination contracts remain in Common/Pagination/Contracts.
Separate Interfaces and service Contracts folders are not used.

### Cross-Feature Access

Shared Domain entities/enums may be used across features without depending on another feature's implementation.

For reading another feature's data, define the source in the consuming feature by default.
Infrastructure reads the required projection/entities through DbContext.
Do not import a foreign source or another scenario's response just to perform one read.
Do not copy an entire source or bypass visibility rules.

To change another feature's state or invoke its business operation, use an explicit narrow owner contract
or an existing shared scenario. A new reading source must not become a way to bypass the owner's business rules.

Business-operation dependencies must be explicit and one-way. Do not call another feature's concrete service
or consume its full CRUD interface. Resolve cycles through a coordinating scenario or revised ownership.
A coordinating scenario used by only one feature stays in that feature; stable reused logic may move to Common.

Concrete ownership of information, shared registration, recognition context, and message events is described
in the scenario sections below.

## Current Structure

```text
ZooFinder.Application/
├─ Common/
│  ├─ Animals/
│  │  ├─ Information/
│  │  │  ├─ Dependencies/AnimalInformation/
│  │  │  ├─ Constants/
│  │  │  ├─ Exceptions/
│  │  │  ├─ Extensions/
│  │  │  └─ Validators/
│  │  └─ Registration/
│  │     ├─ Services/AnimalRegistration/
│  │     ├─ DataSources/
│  │     └─ Constants/
│  ├─ ErrorHandling/Exceptions/
│  ├─ Language/
│  │  ├─ Constants/
│  │  ├─ Extensions/
│  │  └─ Validators/
│  └─ Pagination/
│     ├─ Constants/
│     ├─ Contracts/
│     └─ Extensions/
└─ Features/
   ├─ Animals/
   │  ├─ Catalog/
   │  └─ Recognition/
   ├─ Parks/
   │  ├─ Catalog/
   │  ├─ Animals/
   │  ├─ Import/
   │  └─ Connections/
   ├─ Discussions/
   │  ├─ Common/
   │  ├─ Rooms/
   │  └─ Messages/
   ├─ Auth/
   └─ Users/
```

Services are grouped with their interface and contracts. For example:

```text
Features/Animals/Recognition/
├─ Services/AnimalRecognition/
│  ├─ IAnimalRecognitionService.cs
│  ├─ AnimalRecognitionService.cs
│  └─ AnimalRecognitionServiceContracts.cs
├─ Dependencies/AnimalRecognition/
│  ├─ IAnimalRecognitionProvider.cs
│  └─ AnimalRecognitionProviderContracts.cs
└─ DataSources/
   ├─ IAnimalRecognitionDataSource.cs
   └─ AnimalRecognitionDataSourceContracts.cs
```

Other service folders are `AnimalCatalog`, `AnimalRegistration`, `Parks`, `ParkAnimals`,
`ParkAnimalImport`, `ParkConnections`, `Auth`, `UserProfiles`, `DiscussionRooms`, and `DiscussionMessages`.
There are no separate `Interfaces` or service-level `Contracts` directories.

## Data Sources

A DataSource defines storage operations needed by a scenario; it may work with several Domain entities.
Infrastructure implements these interfaces. An interface is not required for every entity or every method.

| Module | Interface | Responsibility |
| --- | --- | --- |
| Animals/Catalog | `IAnimalCatalogDataSource` | Local catalog search and sourced identity lookup |
| Animals/Recognition | `IAnimalRecognitionDataSource` | Park state and published recognition context |
| Parks/Catalog | `IParkCatalogDataSource` | Park catalog and state changes |
| Parks/Animals | `IParkAnimalDataSource` | Park membership reads, upserts, removal, and batch apply |
| Parks/Connections | `IParkConnectionDataSource` | Requests and conditional state transitions |
| Discussions/Common | `IDiscussionDataSource` | Rooms, messages, and accounts needed by discussions |
| Auth | `IAuthDataSource` | Account registration and refresh-session operations |
| Users | `IUserProfileDataSource` | Profile reads and updates |
| Common/Animals/Registration | `IAnimalRegistrationDataSource` | Atomic shared Animal/General-room registration |

`ParkRecognitionContextResult` and `ParkRecognitionAnimalResult` are declared beside the recognition
DataSource interface. Other sources use Domain entities and existing appropriate types where sufficient;
empty contract files are not created.

Required atomic operations are explicit DataSource methods. Application has no general-purpose UnitOfWork.

## Shared Animal Information and Registration

`Common/Animals/Information` owns normalization, validation, constraints, the provider interface,
provider contracts, and `AnimalInformationUnavailableException`. Language handling belongs to
`Common/Language`; supported codes are `en` and `ru`.

`IAnimalInformationProvider` exposes `SearchAsync` and `GetDetailsAsync`.
Its identity is `InformationSource + SourceItemId + LanguageCode`; results include `SourceUrl`.

`Common/Animals/Registration` owns `AnimalRegistrationService`, its interface and service contracts,
`IAnimalRegistrationDataSource`, and `AnimalRegistrationDefaults`.

`RegisterAsync` accepts either `RegisterAnimalRequest` or previously retrieved
`AnimalInformationDetailsResult`. Exact existing source identity can be reused without a provider call.
Otherwise the service retrieves or validates details and delegates atomic card/room registration to its source.

Matching checks source identity, scientific name, then normalized title within the same language.
Ambiguous matches and conflicting scientific names produce a conflict. Reuse retains canonical stored identity.
The service returns `AnimalRegistrationResult(AnimalId, GeneralRoomId)`, not another feature's response.

Both Parks and Discussions use this scenario. First registration creates an Animal and an empty General room;
later registration reuses them. The shared card and discussion may exist without any park membership.

## Animal Catalog

`AnimalCatalogService` provides `SearchAsync` and `GetAnimalAsync`.

| Search scope | Source |
| --- | --- |
| `ExternalCatalog` | `IAnimalInformationProvider` |
| `LocalCatalog` | `IAnimalCatalogDataSource` |

Search returns `AnimalCardResponse`; page retrieval returns `AnimalPageResponse`.
`LocalAnimalId` identifies a saved card. `HasStartedDiscussion` is derived from its presence and does not
mean that the General room already contains messages.

Catalog operations do not persist cards or rooms. `GetAnimalAsync` currently retrieves provider details first;
it has no cached fallback. The cached fallback implemented for park-card retrieval is a separate behavior.

## Animal Recognition

The service accepts an image stream, file name, content type, declared length, language code,
and optional `ParkId`. Application checks readability, file-name length, the 10 MiB declared-length limit,
language, and provider-result consistency. Actual file-size/signature/decode checks belong to the future
upload boundary and model adapter.

With `ParkId`, `IAnimalRecognitionDataSource.GetParkContextAsync` loads park status and published memberships.
Unknown parks produce not-found; suspended parks produce conflict. No park or an empty published catalog
produces an empty candidate list and general recognition.

Provider request candidates contain `AnimalId`, common name, and scientific name. These are contextual
suggestions, not a rule requiring the model to choose an in-catalog species.

The provider and service share these dependency-owned types:

- `AnimalRecognitionStatus`: `Recognized`, `Uncertain`, `NoAnimal`.
- Context and provider candidate records.
- `AnimalRecognitionExecutionResult`: `ModelId`, optional `Revision`, and `InferenceMilliseconds`.

Provider request/result records belong to `Dependencies/AnimalRecognition`.
The service's request, response, and displayed candidate records belong to `Services/AnimalRecognition`.

Application rejects returned IDs outside the supplied context, duplicate candidate IDs, contradictory
`NoAnimal` results, empty recognized results, and invalid execution metadata. Known IDs are mapped to
canonical names and `ParkAnimalId`; species outside the catalog may be returned by name without a park link.

Recognition does not register animals, add memberships, save photographs, or query Wiki.
The client follows up through Catalog or a park card. Concrete HTTP/Python inference is not implemented yet.

## Parks

All park operations are open to everyone in v1: no required user ID, ownership/role check, or payment gate.
Existing account and discussion-message rules still apply to those existing features.

| Service | Operations |
| --- | --- |
| `ParkService` | Get/search, create, update, set Active/Suspended status |
| `ParkAnimalService` | Get/search, add/reuse species, update local text/publication, remove membership |
| `ParkAnimalImportService` | Preview decoded rows and apply confirmed selections |
| `ParkConnectionService` | Get/search requests, submit, approve price, confirm payment, reject/cancel |

A park is active immediately and may have no species list.
Park searches use zero-based offset pagination and can include suspended parks explicitly.
Membership searches exclude unpublished links by default; direct card retrieval can read unpublished links
for editing. Public endpoints must preserve the visibility requirements of their scenario.

Park-card retrieval combines live provider details with local park text. On
`AnimalInformationUnavailableException`, it falls back to cached shared information without a source URL.
A null provider result also leaves cached details available. Cancellation and programming errors are not
silently treated as provider unavailability.
Membership lists use cached data and do not call Wiki for every row.

Imports accept decoded rows, up to 200. CSV parsing is not implemented.
Preview performs no writes and requires an unambiguous exact match or explicit source identity.
Apply validates and retrieves selected details again, registers shared cards/rooms, then applies the
membership batch in one transaction. Repeated imports reuse membership IDs.

Shared registration occurs in separate transactions before membership apply. Those cards/rooms may remain
if the link batch later fails. This is not a transaction covering the whole import.
Empty imports and empty park catalogs are supported.

Connection transitions are `Submitted → AwaitingPayment → Activated`; unfinished requests may also become
`Rejected` or `Cancelled`. Approval records price/currency; payment confirmation is manual.
Identical repeated approval/payment confirmation is idempotent. Conditional writes reject stale state;
finished requests cannot be reopened. Payment does not change park status.

## Users and Authentication

`UserProfileService` reads and updates a profile. Reads use profile data without loading the full account graph;
updates change the authenticated active account's display name. Normalized names contain 1–100 characters.

`AuthService` supports registration, login, refresh, revoking one session, and revoking all sessions.
Its dependency interfaces are grouped under:

```text
Features/Auth/Dependencies/
├─ PasswordHashing/IPasswordHasher.cs
├─ AccessToken/IAccessTokenProvider.cs
├─ RefreshTokenGeneration/IRefreshTokenGenerator.cs
└─ RefreshTokenHashing/IRefreshTokenHasher.cs
```

Registration normalizes a unique login to lowercase and creates an active User account, profile,
and first refresh session atomically. The initial display name is the login.
Passwords and refresh tokens are persisted only as hashes.

Login verifies the password and creates a separate session. Invalid credentials and inactive accounts
produce an authentication error. Refresh validates the account/session and conditionally rotates the expected
token hash; a concurrent or inactive session fails rotation. `AuthSettings.RefreshSessionLifetime`
controls expiration; `TimeProvider` supplies UTC time.

Concrete hashing, token-generation, and access-token implementations remain pending.

## Discussions

`Features/Discussions/Common` owns the shared DataSource and account-write validation.
`Rooms` owns `DiscussionRoomService`; `Messages` owns `DiscussionMessageService`, message constraints,
validation, and `Dependencies/DiscussionEvents`.

Room operations retrieve a General room or create/reuse it through shared registration.
Explicit discussion creation requires an active account. Automatic registration by Parks does not.

Messages support cursor history, sending, editing, and soft deletion. Content contains 1–4,000 characters
after normalization. Writes require an active account and an open room. Editing/deletion is available to
the author, a moderator, or an administrator. Deleted history items retain metadata with null content.

`IDiscussionEventPublisher` receives `DiscussionMessageEvent` for creation/update; deletion publishes IDs
and deletion time. It does not import `DiscussionMessageResponse`. The service maps its response to the event.

Persistence precedes publication; these are separate operations without an outbox.
Application contains no SignalR implementation.

## Pagination and Errors

Offset pages are zero-based; defaults are page 0, page size 20, and maximum size 100.
Cursor contracts are `CursorPageRequest` and `CursorPageResponse<T>`; cursors are opaque to Application clients.
Local catalog search and message history use cursor pagination; provider cursors have provider-owned semantics.

Shared exceptions are `RequestValidationException`, `NotFoundException`, `ConflictException`,
`UnauthorizedException`, and `ForbiddenException`. API exception mapping remains pending.

## Composition and Verification

Application does not register itself through a DI framework. The future composition root registers services,
sources, providers, settings, and `TimeProvider`. Runtime registration is not implemented.

Tests and migrations follow [Development rules](../development.md); current validation uses builds and
necessary manual checks. A successful build is not an integration or concurrency test.
