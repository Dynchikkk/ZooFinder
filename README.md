# ZooFinder

ZooFinder is a web platform that combines basic animal information, community discussions, and AI-assisted animal recognition.

This README is the main source of project-level decisions and development principles. Detailed backend notes are available in [docs/backend.md](docs/backend.md), and the Application-layer roadmap is maintained in [docs/application.md](docs/application.md).

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
- `DiscussionMessage`;
- `UserAccount`;
- `UserProfile`;
- `UserRefreshSession`.

Every discussion room belongs to an animal. An animal may have multiple discussion rooms, but a room cannot exist without an animal.

### ZooFinder.Application

Contains use cases, application services, application contracts, and abstractions required by the use cases.

Examples of application abstractions:

```text
IAnimalRepository
IAnimalInformationService
IAnimalRecognitionProvider
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

The initial domain model contains six entities and three enums. The C# entities are simple mutable models with public getters and setters. They are grouped by domain area into `Animals`, `Discussions`, and `Users`, while shared entity fields belong to `BaseEntities`.

The structures below are language-independent pseudocode and describe data rather than a concrete C# implementation.

### Common Entity Fields

All six entities inherit from `IdEntity<TId>`:

```text
IdEntity<TId>
{
    Id: TId
    IsDeleted: Boolean

    CreatedAtUtc: DateTime
    UpdatedAtUtc: DateTime
    DeletedAtUtc: DateTime?
}
```

New entities are not deleted by default. Soft deletion sets `IsDeleted` and `DeletedAtUtc` without physically removing the entity from persistence.

### Relationships

```text
Animal 1 ─────── * DiscussionRoom
                         │
                         │ 1
                         ▼
              * DiscussionMessage * ─────── 1 UserAccount
                                              │
                                              ├──── 1 UserProfile
                                              │
                                              └──── * UserRefreshSession
```

### Animal

```text
Animal : IdEntity<UUID>
{
    InformationSource: String
    SourceItemId: String
    LanguageCode: String
    Title: String

    ScientificName: String?
    ShortDescription: String?
    ImageUrl: String?

    LastSynchronizedAtUtc: DateTime
}
```

Constraints:

- `InformationSource + LanguageCode + SourceItemId` is unique;
- `ScientificName`, `ShortDescription`, and `ImageUrl` are optional;
- `SourceUrl` is resolved through the configured animal-information provider and is not persisted;
- a new animal and its `General` discussion room are created in the same transaction.

### DiscussionRoom

```text
DiscussionRoom : IdEntity<UUID>
{
    AnimalId: UUID

    Type: DiscussionRoomType
    Name: String
    Description: String?

    IsClosed: Boolean
}
```

```text
DiscussionRoomType
{
    None = 0
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

### DiscussionMessage

```text
DiscussionMessage : IdEntity<UUID>
{
    DiscussionRoomId: UUID
    AuthorUserAccountId: UUID

    Content: String

    EditedAtUtc: DateTime?
}
```

Constraints:

- every discussion message belongs to one discussion room;
- every discussion message references `UserAccount` directly;
- content cannot be empty and has a maximum length;
- deleted discussion messages remain persisted, but their content is not returned to regular clients;
- discussion messages cannot be created in closed rooms or by blocked users.

### UserAccount

`UserAccount` contains the internal account, authorization state, and optional login credentials. It does not contain public profile data or refresh-token data.

```text
UserAccount : IdEntity<UUID>
{
    Login: String?
    PasswordHash: String?

    Role: UserRole
    Status: UserStatus
}
```

```text
UserRole
{
    None = 0
    User = 1
    Moderator = 2
    Administrator = 3
}
```

```text
UserStatus
{
    None = 0
    Active = 1
    Blocked = 2
}
```

Constraints:

- `None` represents an unassigned value and is not a valid persisted role or status;
- initial name-only registration creates a user without `Login` and `PasswordHash`;
- `Login` becomes unique when it is introduced;
- only a password hash is persisted, never the original password;
- new users receive the `User` role and `Active` status;
- discussion messages use `UserAccount.Id` as the author identifier;
- reading public profile data does not require loading `UserAccount`; application queries should select only the data required by a use case;
- role claims may be included in short-lived JWT access tokens, while current account status may be checked through the database or a cache for protected operations.

### UserProfile

`UserProfile` contains public user data.

```text
UserProfile : IdEntity<UUID>
{
    UserAccountId: UUID

    DisplayName: String
}
```

Constraints:

- `Id` is the profile primary key;
- `UserAccountId` is a unique foreign key to `UserAccount`;
- the relationship between `UserAccount` and `UserProfile` is one-to-one;
- `DisplayName` is not a login credential and does not have to be unique.

### UserRefreshSession

`UserRefreshSession` represents authorization on one browser or device. It does not contain login or password data.

```text
UserRefreshSession : IdEntity<UUID>
{
    UserAccountId: UUID

    RefreshTokenHash: String

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
    → create UserAccount without login credentials
    → create UserProfile
    → create UserRefreshSession
    → issue a short-lived JWT access token
    → issue a refresh token
```

Until login credentials are introduced, access to the account depends on the refresh session. Losing or expiring that session means the account cannot be recovered.

The following values are contracts or transient data and are not domain entities:

```text
JWT access token
Animal information provider response
Ollama recognition result
Uploaded recognition image
SourceUrl
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

The domain entity stores a provider-neutral source name, source item identifier, and language code. The initial provider uses MediaWiki, but Application and Domain do not depend on that choice. The current source URL is resolved by the configured provider and does not need to be persisted in `Animal`.

Responses may be cached to avoid repeated external requests. The exact persistence and expiration strategy will be selected later.

## Image Recognition

The initial recognition feature uses a ready-to-run vision-language model exposed through the Ollama HTTP API.

```text
Angular
    → ZooFinder.Api
    → Ollama container
    → vision model result
    → ZooFinder.Api response
    → Angular starts a separate animal catalog search
```

Principles:

- ZooFinder does not train a model in the initial version;
- ZooFinder does not calculate custom embeddings or maintain a custom classification index;
- the ASP.NET application sends the uploaded image and a fixed prompt to Ollama;
- Ollama returns a structured suggestion containing common and scientific names;
- the response contains names that the client may use in a separate catalog search;
- the result is presented as a suggestion, not a guaranteed identification;
- generated confidence percentages must not be presented as real probabilities;
- the frontend never communicates with Ollama directly;
- the concrete recognition implementation is replaceable through `IAnimalRecognitionProvider`.

The Application contracts deliberately separate the use-case boundary from the provider boundary:

```text
AnimalRecognitionRequest
    → IAnimalRecognitionService
    → IAnimalRecognitionProvider
    → AnimalRecognitionProviderResult
    → AnimalRecognitionResponse
```

`AnimalRecognitionProviderResult` protects the public response from provider-specific changes. Its candidate type is declared in the same file because it is only a component of that result. The public candidate response follows the same rule and is declared next to `AnimalRecognitionResponse`.

The current Application service validates that the stream is readable and that the declared length is within the configured limit. Validation of the actual image format is intentionally deferred and must be added before the recognition endpoint is exposed publicly; the declared content type alone must not be treated as proof of the file format.

Changing the model name or introducing another recognition provider must not require controller or frontend changes.

## Discussion Direction

The first version exposes one `General` discussion room for each animal. The domain model supports a one-to-many relationship from `Animal` to `DiscussionRoom`, allowing additional `Topic` rooms to be introduced later.

A discussion room cannot exist without an animal. `DiscussionRoomType` contains `None`, `General`, and `Topic` values. `None` represents an unassigned value and is not a valid persisted room type. When a new animal is created, its `General` room is created in the same transaction. Each animal must have exactly one `General` room and may have multiple `Topic` rooms. Room names must be unique within an animal, while different animals may use the same room names.

The initial name-only registration creates a `UserAccount`, a public `UserProfile`, and a `UserRefreshSession`. Discussion messages reference `UserAccount` directly. The API issues a short-lived JWT access token and a refresh token. Access tokens are not persisted; only a hash of the refresh token is stored in `UserRefreshSession`. Login and password-hash fields may be filled later without moving profile data into `UserAccount`.

Real-time discussion message delivery is expected to use ASP.NET Core SignalR. REST endpoints remain responsible for discussion message history and other request-response operations.

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
