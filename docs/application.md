# ZooFinder Application Layer

> Working roadmap. Tests are intentionally deferred until the initial Application implementation is complete.

## Purpose

`ZooFinder.Application` contains use cases, application contracts, mapping, and the interfaces implemented by Infrastructure. It depends only on `ZooFinder.Domain`.

The layer follows the service-oriented, feature-based structure used by MotorsportERP. MediatR and CQRS are not used at the current project stage.

## Boundaries

- All operations that perform I/O are asynchronous.
- Asynchronous I/O methods accept a `CancellationToken`.
- Application code does not use `DbContext`, `DbSet`, `IQueryable`, `HttpClient`, `IFormFile`, controllers, or other ASP.NET Core types.
- Application owns interfaces; Infrastructure owns their concrete implementations.
- DTOs use records when mutable state is not required.
- Repository methods describe explicit use-case operations instead of exposing generic persistence behavior.
- Neighboring use cases do not call each other directly. Shared feature logic belongs to the feature's `Common` directory.
- Contracts shared by different top-level features belong to the global `Application/Common` directory.

## Structure

```text
ZooFinder.Application/
├─ Common/
│  ├─ Constants/
│  ├─ Contracts/
│  ├─ Exceptions/
│  ├─ Extensions/
│  └─ Interfaces/
│     ├─ ExternalServices/
│     ├─ Repositories/
│     └─ Security/
└─ Features/
   ├─ Animals/
   │  ├─ Common/
   │  ├─ Catalog/
   │  └─ Recognition/
   ├─ Auth/
   ├─ Discussions/
   └─ Users/
```

A use case may contain `Contracts`, `Interfaces`, `Mappers`, and `Services`. Directories are added only when they contain real code.

## Pagination

Application defines pagination semantics; Infrastructure implements them for a database or external provider.

Offset pagination is zero-based and is intended for stable tables that require numbered pages:

```text
FirstPage = 0
DefaultPageSize = 20
MaximumPageSize = 100
```

Cursor pagination is intended for external searches and changing feeds such as discussion history. A cursor is an opaque continuation value. Application passes it between the client and the relevant provider without depending on its internal representation.

Generic repositories such as `IPagedRepository<T>` are not introduced. Each repository exposes a specifically named paginated operation with deterministic ordering.

## Animal Information Source

Application does not depend on MediaWiki or any other concrete animal-information website. It uses `IAnimalInformationService` and provider-neutral contracts containing:

```text
InformationSource
SourceItemId
LanguageCode
SourceUrl
```

The initial Infrastructure implementation will use MediaWiki. A future provider can implement the same interface without changing Application use cases.

## Development Stages

### 1. Application foundation — completed

- Reference `ZooFinder.Domain`.
- Add common pagination contracts.
- Add supported language constants.
- Add request validation exception.
- Establish cancellation and dependency-boundary conventions.

### 2. Animal catalog — completed

- Add `IAnimalInformationService`.
- Make the Domain animal identity provider-neutral.
- Add `IAnimalRepository` with explicit source-identity and local-search operations.
- Add provider-neutral search and details results for the information provider.
- Add `AnimalSearchRequest`, `AnimalCardResponse`, `AnimalPageRequest`, and `AnimalPageResponse`.
- Add `IAnimalCatalogService` and `AnimalCatalogService`.
- Search either the external provider or the local database according to `AnimalSearchScope`.
- Return compact cards from search and full information from the animal page operation.
- Report whether a local animal with a started discussion exists through `LocalAnimalId`.
- Keep search and page operations read-only. They never create or update animals or discussion rooms.

### 3. Animal recognition — completed

- Add neutral image input containing `Stream`, file name, content type, and length.
- Add recognition suggestion and response contracts.
- Add `IAnimalRecognitionProvider` for the replaceable vision implementation.
- Add `IAnimalRecognitionService` and its implementation.
- Validate that the image stream is readable and its declared length is within the accepted limit.
- Add validation of the actual image format before exposing the recognition endpoint publicly; do not rely only on the declared content type.
- Return recognized common and scientific names without calling the catalog.
- Let the client start a separate catalog search using the recognition result.
- Do not persist uploaded recognition images.

### 4. Users

- Add public and current-user profile contracts.
- Add profile update request.
- Add `IUserService`, mapper, and implementation.
- Add explicit account and profile repository operations.
- Support public profile retrieval and display-name updates.

Repository interfaces shared by Users and Auth belong to global Common:

```text
Common/Interfaces/Repositories/
├─ IUserAccountRepository.cs
├─ IUserProfileRepository.cs
└─ IUserRefreshSessionRepository.cs
```

### 5. Name-only authentication

- Add registration, refresh, and authentication response contracts.
- Add `IAuthService` and its implementation.
- Add access-token, refresh-token generation, and refresh-token hashing interfaces.
- Create account, profile, and refresh session atomically.
- Implement refresh-token rotation.
- Revoke one session or all sessions for an account.
- Add an Application-facing transaction abstraction when the first atomic workflow requires it.

### 6. Discussions

Split Discussions into shared feature elements, Rooms, and Messages.

Initial use cases:

- retrieve an animal's `General` room;
- retrieve cursor-paginated message history;
- create the local `Animal` and its `General` room atomically when the first message starts a discussion;
- send a message;
- edit an owned message;
- soft-delete a message;
- reject writes from blocked users or to closed rooms;
- omit deleted content from regular client responses.

Define an event-publishing interface only when real-time delivery is implemented. SignalR remains outside Application.

Animal search and page viewing never persist an animal. The first meaningful discussion write is the initial persistence boundary.

### 7. Application service registration

Add `DependencyInjection.cs` after the service set becomes stable. It registers Application services only. Repository, provider, and security implementations are registered by Infrastructure.

### 8. Final boundary review

Before starting Infrastructure, verify that:

- Application depends only on Domain;
- no ASP.NET Core, EF Core, MediaWiki, or Ollama transport models are present;
- repositories do not expose `IQueryable` or persistence-specific types;
- external operations accept cancellation tokens;
- provider-specific behavior is hidden behind Application interfaces;
- shared logic is placed at the narrowest valid `Common` level.

## Deferred Work

Automated tests are deliberately postponed by the current development decision. No test project or test dependencies should be added until this roadmap is revised.
