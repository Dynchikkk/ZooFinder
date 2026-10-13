# Application Layer

`ZooFinder.Application` contains use cases, validation, contracts, and the interfaces required to execute them.
It depends on Domain and has no EF, HTTP, SignalR, or dependency-injection framework dependency.

**Status:** Eleven services and their contracts are implemented. Infrastructure Auth security implementations
are available; animal providers, event publishing, and API wiring are still pending.
Global layer boundaries are defined in [General architecture](../architecture.md).
Application-specific placement and module-dependency rules are defined below; formatting is defined in
[Code style](../code-style.md).

## Recursive Module Organization

Application has three root sections:

- Modules: primary business modules.
- Common: business functionality genuinely reused across primary modules.
- Shared: technical Application components independent of business modules.

Modules and Common use the same recursive module structure.
Every module can directly contain Services, DataSources, Dependencies, Contracts, Validators, Constants,
Settings, Extensions, Exceptions, and a Modules folder for child modules.
Create only the folders required by actual components; no empty scaffolding is needed.

Module roots contain purpose folders rather than individual C# files.
A module may have its own services/sources and child modules at the same time.
Nesting expresses responsibility, not a requirement to wrap every service in another module.
Adding a child does not require moving existing parent components.

### Ownership and Dependencies

Ordinary sibling modules must not import each other's services, data sources, request/response contracts,
or other private components. A dependency through an interface still counts as a module dependency.

A child can use explicitly shared components of its ancestors.
A parent can coordinate its children through their public operation contracts.
Shared parent components must not depend on private child types or implementations.

Common and Shared are explicit exceptions for shared functionality.
Shared must not depend on Modules or Common.
Common can use Domain, Shared, and its own components, but cannot import private primary-module functionality.
All dependency cycles are prohibited.

Shared Domain types/enums can be used across modules. Reading another module's stored data uses a source
owned by the consumer; its Infrastructure implementation reads the required Domain entities/projection.
Preserve visibility and business rules. Do not copy a foreign source or use a new source merely to bypass
another module's business operation.

Cross-branch business workflows are coordinated by the nearest common parent or by an appropriate
shared Common scenario. Do not introduce direct sibling calls, including broad CRUD interfaces.
Functionality used by children of one parent stays at that parent rather than global Common.

### Components and Names

Namespaces match placement. Entity-based folder/namespace segments are plural; entity/service classes remain
singular. Task names remain unchanged. Avoid namespace/type collisions through naming rather than aliases.

Each service is grouped in Services/<Name>/ with its interface, implementation, and service-contract file.
The folder omits the Service suffix. A service can implement several related use cases; one service per module
is not mandatory.

DataSources folders stay flat. A source serves a coherent task and can work with several Domain entities.
Do not create a source per table, method, or service.
Optional <Name>DataSourceContracts.cs belongs beside the interface.
Use existing appropriate types and do not create empty contracts or redundant DTOs.

Providers, hashing, token generation, and publishers belong to Dependencies/<Name>/ with their interface and
own contracts when needed. Implementations remain outside Application.
Dependency contracts must not import service requests/responses.
Related constants, validators, settings, and helpers belong to their owner.

Settings are plain configuration classes needed by Application logic; they do not read IConfiguration
or appsettings.json. Technical connection/transport settings belong to the Infrastructure implementation.

## Current Structure

```text
ZooFinder.Application/
├─ Shared/
│  ├─ Pagination/
│  │  ├─ Contracts/
│  │  ├─ Constants/
│  │  └─ Extensions/
│  ├─ ErrorHandling/Exceptions/
│  └─ Language/
│     ├─ Constants/
│     ├─ Validators/
│     └─ Extensions/
├─ Common/
│  └─ Animals/
│     ├─ Services/AnimalRegistration/
│     ├─ DataSources/
│     ├─ Dependencies/AnimalInformation/
│     ├─ Constants/
│     ├─ Validators/
│     ├─ Extensions/
│     └─ Exceptions/
└─ Modules/
   ├─ Animals/
   │  └─ Modules/
   │     ├─ Catalog/
   │     │  ├─ Services/AnimalCatalog/
   │     │  └─ DataSources/
   │     └─ Recognition/
   │        ├─ Services/AnimalRecognition/
   │        ├─ DataSources/
   │        ├─ Dependencies/AnimalRecognition/
   │        ├─ Constants/
   │        └─ Validators/
   ├─ Auth/
   │  ├─ DataSources/
   │  ├─ Dependencies/
   │  ├─ Settings/
   │  ├─ Validators/
   │  └─ Modules/
   │     └─ Authentication/
   │        └─ Services/Auth/
   ├─ Parks/
   │  ├─ DataSources/
   │  ├─ Constants/
   │  ├─ Validators/
   │  └─ Modules/
   │     ├─ Catalog/Services/Parks/
   │     ├─ Animals/Services/ParkAnimals/
   │     ├─ Import/
   │     │  ├─ Services/ParkAnimalImport/
   │     │  ├─ Constants/
   │     │  └─ Validators/
   │     └─ Connections/
   │        ├─ Services/ParkConnections/
   │        ├─ DataSources/
   │        ├─ Constants/
   │        └─ Validators/
   ├─ Discussions/
   │  ├─ DataSources/
   │  ├─ Validators/
   │  └─ Modules/
   │     ├─ Rooms/
   │     │  ├─ Services/DiscussionRooms/
   │     │  └─ Validators/
   │     └─ Messages/
   │        ├─ Services/DiscussionMessages/
   │        ├─ Dependencies/DiscussionEvents/
   │        ├─ Constants/
   │        └─ Validators/
   └─ Users/
      └─ Modules/
         └─ Profiles/
            ├─ Services/UserProfiles/
            ├─ DataSources/
            ├─ Constants/
            └─ Validators/
```

The existing AuthService stays intact under Authentication, including registration, login, refresh,
and revocation. There is no separate Sessions module or split service implementation at this stage.

Common/Animals directly contains shared registration and information support.
A separate Registration/Information module wrapper is not required for these components.

## Data Sources and Shared Parent Components

| Owner | Interface | Responsibility |
| --- | --- | --- |
| Modules/Animals/Modules/Catalog | IAnimalCatalogDataSource | Local catalog search/identity |
| Modules/Animals/Modules/Recognition | IAnimalRecognitionDataSource | Park state and published recognition context |
| Modules/Parks | IParkCatalogDataSource | Park reads/state and shared guards |
| Modules/Parks | IParkAnimalDataSource | Membership operations used by Animals and Import |
| Modules/Parks/Modules/Connections | IParkConnectionDataSource | Connection requests/transitions |
| Modules/Discussions | IDiscussionDataSource | Shared room/message/account operations |
| Modules/Auth | IAuthDataSource | Account/refresh-session storage used by Authentication |
| Modules/Users/Modules/Profiles | IUserProfileDataSource | Profile reads/updates |
| Common/Animals | IAnimalRegistrationDataSource | Atomic shared Animal/General registration |

ParkValidator and ParkAnimalValidator remain at Parks: catalog, membership, connection, and import scenarios
reuse their respective checks. Import/connection-only validators and constraints belong to those child modules.
DiscussionUserValidator and IDiscussionDataSource stay at Discussions; room/message-specific components
belong to Rooms/Messages.

These parent-owned components avoid sibling imports without pushing module-local functionality into Common.
DataSource contracts remain adjacent to their interface.
Atomic operations are explicit source methods; no general-purpose UnitOfWork is introduced.

## Shared Animal Information and Registration

`Common/Animals` owns shared information normalization, validation, constraints, the provider interface,
provider contracts, and `AnimalInformationUnavailableException`. Language handling belongs to
`Shared/Language`; supported codes are `en` and `ru`.

`IAnimalInformationProvider` exposes `SearchAsync` and `GetDetailsAsync`.
Its identity is `InformationSource + SourceItemId + LanguageCode`; results include `SourceUrl`.
The interface and provider contracts are grouped under Common/Animals/Dependencies/AnimalInformation.

`Common/Animals/Services/AnimalRegistration` contains `AnimalRegistrationService`, its interface and service contracts;
`IAnimalRegistrationDataSource` and `AnimalRegistrationDefaults` belong to the area's DataSources and Constants folders.

`RegisterAsync` accepts either `RegisterAnimalRequest` or previously retrieved
`AnimalInformationDetailsResult`. Exact existing source identity can be reused without a provider call.
Otherwise the service retrieves or validates details and delegates atomic card/room registration to its source.

Matching checks source identity, scientific name, then normalized title within the same language.
Ambiguous matches and conflicting scientific names produce a conflict. Reuse retains canonical stored identity.
The service returns `AnimalRegistrationResult(AnimalId, GeneralRoomId)`, rather than another module's response.

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

`AnimalRecognitionService` coordinates request normalization, park-context loading, provider invocation,
result validation, and response mapping. `AnimalRecognitionConstraints` owns the image and file-name limits;
`AnimalRecognitionValidator` checks the request, park ID, main candidate, alternatives, and execution metadata.
The service maps the main candidate and alternatives through the same helper, using one lookup of park animals
for canonical names and park-membership IDs. A known animal's missing scientific name remains null rather than
falling back to a provider-supplied name.

With `ParkId`, `IAnimalRecognitionDataSource.GetParkContextAsync` loads park status and published memberships.
Its interface and context contracts are in Modules/Animals/Modules/Recognition/DataSources.
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
Existing account and discussion-message rules still apply to those existing modules.

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
Modules/Auth/Dependencies/
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

`TimeProvider` is the standard .NET abstract class, not a project-defined service. Its virtual `GetUtcNow()`
provides a replaceable clock; `TimeProvider.System` uses the system clock. Passing this abstraction into services
allows later verification of expiration without waiting for real time. No custom clock interface or implementation
is currently defined in the project.

Concrete password hashing, refresh-token generation/hashing, and JWT access-token implementations are available
in Infrastructure. The boolean password verification port does not support automatic legacy-hash upgrades.
Runtime composition, bearer validation, and HTTP endpoints remain pending.

## Discussions

Modules/Discussions owns shared DataSources and account validation.
Modules/Rooms contains DiscussionRoomService and its validator.
Modules/Messages contains DiscussionMessageService, message constraints/validation, and Dependencies/DiscussionEvents.

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

Shared/ErrorHandling owns `RequestValidationException`, `NotFoundException`, `ConflictException`,
`UnauthorizedException`, and `ForbiddenException`. Pagination contracts/constants/extensions are under Shared/Pagination.
API exception mapping remains pending.

## Composition and Verification

Application does not register itself through a DI framework. The future composition root registers services,
sources, providers, settings, and `TimeProvider`. Runtime registration is not implemented.

Tests and migrations follow [Development rules](../development.md); current validation uses builds and
necessary manual checks. A successful build is not an integration or concurrency test.
