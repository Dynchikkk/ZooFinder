# ZooFinder

ZooFinder is a web platform that combines basic animal information, community discussions, and AI-assisted animal recognition.

This README is the main source of project-level decisions and development principles. Detailed backend notes are available in [docs/backend.md](docs/backend.md).

## Product Direction

The initial product includes:

- animal pages with an optional short description, an optional image, and a link to Wikipedia;
- animal search;
- an anonymous discussion room associated with each animal;
- animal identification from a user-uploaded image.

Planned later stages include:

- user registration and profiles;
- multiple thematic discussion rooms associated with individual animals;
- moderation tools;
- replacement of the initial recognition model with a more capable provider or model.

The initial version intentionally keeps animal information small. It does not import or parse the complete Wikipedia catalog.

## Technology Decisions

### Backend

- .NET 10
- ASP.NET Core Web API
- OpenAPI
- Entity Framework Core
- Microsoft SQL Server
- JWT access tokens with refresh-token sessions

### Frontend

- Angular
- TypeScript

Angular is selected as the project frontend framework to provide a consistent, opinionated application structure.

### External Integrations

- MediaWiki API for animal information
- Ollama API for the initial image recognition prototype
- `qwen3-vl:2b` as the initial replaceable vision model

### Environment

- Docker Compose for the complete application environment
- the recognition model always runs in a container;
- no local Python or machine learning runtime is required outside Docker;
- the API and frontend may still be started outside Docker during development;
- the recognition container is accessed only through the internal Docker network.

## Repository Structure

```text
ZooFinder/
├─ apps/
│  ├─ backend/
│  │  └─ ZooFinder/
│  │     ├─ ZooFinder.Api/
│  │     ├─ ZooFinder.Application/
│  │     ├─ ZooFinder.Domain/
│  │     ├─ ZooFinder.Infrastructure/
│  │     └─ ZooFinder.slnx
│  └─ frontend/
├─ docs/
├─ infra/
│  └─ docker/
└─ scripts/
```

Backend projects remain directly inside the backend solution directory. Separate `src` and `tests` directories are not used at the current project stage.

## Backend Architecture

The backend follows Clean Architecture boundaries with pragmatic DDD principles. Clean Architecture controls dependency direction, while DDD concepts are used only where they improve the domain model. Full tactical DDD is not a project requirement.

### ZooFinder.Domain

Contains domain entities, value objects, domain rules, and domain exceptions.

Principles:

- has no dependency on ASP.NET Core, Entity Framework Core, SQL Server, Ollama, or MediaWiki;
- models domain behavior and invariants rather than HTTP or persistence concerns;
- does not expose infrastructure-specific types;
- remains usable independently from the selected persistence implementation.

Initial domain concepts may include:

- `Animal`;
- `DiscussionRoom`;
- `Message`;
- `User`;
- `UserProfile`;
- `UserRefreshSession`.

Every discussion room belongs to an animal. An animal may have multiple discussion rooms, but a room cannot exist without an animal.

### ZooFinder.Application

Contains use cases, application services, application contracts, and abstractions required by the use cases.

Examples of application abstractions:

```text
IAnimalRepository
IWikipediaClient
IAnimalRecognitionClient
```

The application layer must not depend on concrete database or HTTP client implementations.

### ZooFinder.Infrastructure

Contains implementations of application abstractions:

- Entity Framework Core persistence;
- SQL Server configuration;
- repository implementations;
- MediaWiki API client;
- Ollama API client;
- external file storage if introduced later.

### ZooFinder.Api

Contains HTTP and application hosting concerns:

- controllers;
- request validation;
- middleware and exception handling;
- authentication and authorization configuration;
- OpenAPI configuration;
- dependency injection composition.

Controllers must remain thin and delegate application work to the application layer.

### Dependency Direction

```text
ZooFinder.Api
    → ZooFinder.Application
    → ZooFinder.Domain

ZooFinder.Infrastructure
    → ZooFinder.Application
    → ZooFinder.Domain
```

`ZooFinder.Api` may reference `ZooFinder.Infrastructure` only to register concrete implementations during startup.

## Initial Domain Model

The initial domain model contains six entities and three enums. The structures below are language-independent pseudocode and describe data rather than a concrete C# implementation.

### Relationships

```text
Animal 1 ─────── * DiscussionRoom
                         │
                         │ 1
                         ▼
                      * Message * ─────── 1 User
                                              │
                                              ├──── 1 UserProfile
                                              │
                                              └──── * UserRefreshSession
```

### Animal

```text
Animal
{
    Id: UUID

    WikipediaPageId: Integer64
    WikipediaLanguageCode: String
    Title: String

    ScientificName: String?
    ShortDescription: String?
    ImageUrl: String?

    LastSynchronizedAtUtc: DateTime
    CreatedAtUtc: DateTime
    UpdatedAtUtc: DateTime
}
```

Constraints:

- `WikipediaLanguageCode + WikipediaPageId` is unique;
- `ScientificName`, `ShortDescription`, and `ImageUrl` are optional;
- `WikipediaUrl` is resolved through the MediaWiki API and is not persisted;
- a new animal and its `General` discussion room are created in the same transaction.

### DiscussionRoom

```text
DiscussionRoom
{
    Id: UUID
    AnimalId: UUID

    Type: DiscussionRoomType
    Name: String
    Description: String?

    IsClosed: Boolean

    CreatedAtUtc: DateTime
    UpdatedAtUtc: DateTime
}
```

```text
DiscussionRoomType
{
    General = 1
    Topic = 2
}
```

Constraints:

- `AnimalId` is required;
- a room cannot exist without an animal;
- each animal has exactly one `General` room;
- each animal may have multiple `Topic` rooms;
- `AnimalId + Name` is unique;
- a room cannot be moved to another animal;
- a `General` room cannot be deleted.

### Message

```text
Message
{
    Id: UUID

    DiscussionRoomId: UUID
    AuthorUserId: UUID

    Content: String

    CreatedAtUtc: DateTime
    EditedAtUtc: DateTime?
    DeletedAtUtc: DateTime?
}
```

Constraints:

- every message belongs to one discussion room;
- every message references `User` directly;
- content cannot be empty and has a maximum length;
- deleted messages remain persisted, but their content is not returned to regular clients;
- messages cannot be created in closed rooms or by blocked users.

### User

`User` contains the internal account, authorization state, and optional login credentials. It does not contain public profile data or refresh-token data.

```text
User
{
    Id: UUID

    Login: String?
    PasswordHash: String?

    Role: UserRole
    Status: UserStatus

    CreatedAtUtc: DateTime
    UpdatedAtUtc: DateTime
}
```

```text
UserRole
{
    User = 1
    Moderator = 2
    Administrator = 3
}
```

```text
UserStatus
{
    Active = 1
    Blocked = 2
}
```

Constraints:

- initial name-only registration creates a user without `Login` and `PasswordHash`;
- `Login` becomes unique when it is introduced;
- only a password hash is persisted, never the original password;
- new users receive the `User` role and `Active` status;
- messages use `User.Id` as the author identifier.

### UserProfile

`UserProfile` contains public user data.

```text
UserProfile
{
    UserId: UUID

    DisplayName: String

    CreatedAtUtc: DateTime
    UpdatedAtUtc: DateTime
}
```

Constraints:

- `UserId` is both the profile primary key and a foreign key to `User`;
- the relationship between `User` and `UserProfile` is one-to-one;
- `DisplayName` is not a login credential and does not have to be unique.

### UserRefreshSession

`UserRefreshSession` represents authorization on one browser or device. It does not contain login or password data.

```text
UserRefreshSession
{
    Id: UUID
    UserId: UUID

    RefreshTokenHash: String

    CreatedAtUtc: DateTime
    ExpiresAtUtc: DateTime
    LastUsedAtUtc: DateTime?
    RevokedAtUtc: DateTime?
}
```

Constraints:

- one user may have multiple refresh sessions;
- only a refresh-token hash is persisted;
- raw refresh tokens and JWT access tokens are not persisted;
- sessions may expire or be revoked independently;
- refresh-token rotation replaces the previous token when a session is refreshed.

### Name-only Registration

```text
DisplayName
    → create User without login credentials
    → create UserProfile
    → create UserRefreshSession
    → issue a short-lived JWT access token
    → issue a refresh token
```

Until login credentials are introduced, access to the account depends on the refresh session. Losing or expiring that session means the account cannot be recovered.

The following values are contracts or transient data and are not domain entities:

```text
JWT access token
MediaWiki API response
Ollama recognition result
Uploaded recognition image
WikipediaUrl
```

## Persistence Principles

The initial persistence implementation uses Entity Framework Core with Microsoft SQL Server and the repository pattern.

Repository abstractions belong outside the infrastructure layer. Entity Framework Core implementations, `DbContext`, entity configurations, migrations, and SQL Server-specific behavior belong to `ZooFinder.Infrastructure`.

ADO.NET will be explored later as an alternative repository implementation. Switching between EF Core and ADO.NET should not require controller, application use case, or domain changes.

To preserve this boundary:

- repository interfaces must not expose `DbContext`, `DbSet`, `IQueryable`, or EF Core-specific types;
- repository methods should express explicit domain or application operations;
- application code must not depend on EF Core change tracking or lazy loading;
- transaction boundaries must be exposed through an application-facing abstraction;
- SQL and provider-specific behavior must remain in the infrastructure layer;
- implementations must support cancellation for asynchronous I/O operations.

Possible implementations may use names such as:

```text
EfAnimalRepository
AdoNetAnimalRepository
```

Only the Entity Framework Core implementation is required for the initial version.

## Wikipedia Integration

ZooFinder uses the official MediaWiki API. HTML scraping is not part of the initial implementation.

The first version requests only:

- the article title;
- a short introductory description, when available;
- one preview image, when available;
- the Wikipedia article URL.

The domain entity stores the Wikipedia page identifier and language code. The current article URL is resolved through the MediaWiki API and does not need to be persisted in `Animal`.

Responses may be cached to avoid repeated external requests. The exact persistence and expiration strategy will be selected later.

## Image Recognition

The initial recognition feature uses a ready-to-run vision-language model exposed through the Ollama HTTP API.

```text
Angular
    → ZooFinder.Api
    → Ollama container
    → vision model result
    → MediaWiki API lookup
    → ZooFinder.Api response
```

Principles:

- ZooFinder does not train a model in the initial version;
- ZooFinder does not calculate custom embeddings or maintain a custom classification index;
- the ASP.NET application sends the uploaded image and a fixed prompt to Ollama;
- Ollama returns a structured suggestion containing common and scientific names;
- the scientific name is used for the primary Wikipedia lookup;
- the result is presented as a suggestion, not a guaranteed identification;
- generated confidence percentages must not be presented as real probabilities;
- the frontend never communicates with Ollama directly;
- the concrete recognition client is replaceable through `IAnimalRecognitionClient`.

Changing the model name or introducing another recognition provider must not require controller or frontend changes.

## Discussion Direction

The first version exposes one `General` discussion room for each animal. The domain model supports a one-to-many relationship from `Animal` to `DiscussionRoom`, allowing additional `Topic` rooms to be introduced later.

A discussion room cannot exist without an animal. `DiscussionRoomType` contains `General` and `Topic` values. When a new animal is created, its `General` room is created in the same transaction. Each animal must have exactly one `General` room and may have multiple `Topic` rooms. Room names must be unique within an animal, while different animals may use the same room names.

The initial name-only registration creates a `User`, a public `UserProfile`, and a `UserRefreshSession`. Messages reference `User` directly. The API issues a short-lived JWT access token and a refresh token. Access tokens are not persisted; only a hash of the refresh token is stored in `UserRefreshSession`. Login and password-hash fields may be filled later without moving profile data into `User`.

Real-time message delivery is expected to use ASP.NET Core SignalR. REST endpoints remain responsible for message history and other request-response operations.

## Project Conventions

- All source code, identifiers, comments, documentation, and README files are written in English.
- Comments explain non-obvious decisions and must not repeat the code.
- Domain entities must not depend on persistence or transport concerns.
- External services are accessed through abstractions.
- Infrastructure implementation details must not leak into application or domain contracts.
- Controllers coordinate HTTP concerns and do not contain business logic.
- Initial implementations should remain simple and replaceable; speculative infrastructure is postponed until required.

## Current Open Questions

- Which animal information should be persisted and which should only be cached?
- How long should MediaWiki responses remain cached?
- What image size and file types should the recognition endpoint accept?
- Should uploaded images be deleted immediately after recognition?
- How long should name-only user sessions remain valid?
- How should rate limiting and discussion moderation work?
