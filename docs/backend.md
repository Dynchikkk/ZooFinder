# ZooFinder Backend

> Working draft. This document describes the current backend direction and will evolve with the project.

## Purpose

The ZooFinder backend provides an HTTP API for:

- searching for animals and retrieving basic information;
- retrieving an optional short description, an optional image, and a source link from Wikipedia;
- identifying an animal from an uploaded image through a containerized vision model;
- supporting animal-specific discussions and user accounts in later stages.

## Technology

- .NET 10
- ASP.NET Core Web API
- OpenAPI
- Entity Framework Core for the initial persistence implementation
- Microsoft SQL Server
- JWT access tokens with refresh-token sessions
- MediaWiki API for animal information
- Ollama API for the initial image recognition prototype
- Docker Compose for the complete application environment

The initial recognition model is expected to be `qwen3-vl:2b`. The model is replaceable and is never executed directly by the ASP.NET application.

## Solution Structure

```text
apps/backend/ZooFinder/
├─ ZooFinder.Api/
├─ ZooFinder.Application/
├─ ZooFinder.Domain/
├─ ZooFinder.Infrastructure/
└─ ZooFinder.slnx
```

### ZooFinder.Api

The application entry point and HTTP transport layer.

Responsibilities:

- controllers and API contracts;
- request validation;
- authentication and authorization configuration;
- middleware and exception handling;
- OpenAPI configuration;
- dependency injection composition.

The API project must not contain business rules or external service implementation details.

### ZooFinder.Application

Contains application use cases and abstractions required by those use cases.

Responsibilities:

- animal search and retrieval workflows;
- image recognition workflow;
- discussion workflows;
- request and response models used by application services;
- interfaces for persistence and external integrations.

Examples of external integration abstractions:

```text
IAnimalInformationService
IAnimalRecognitionProvider
```

### ZooFinder.Domain

Contains the core domain model and business rules.

Initial domain concepts may include:

- `Animal`;
- `DiscussionRoom`;
- `DiscussionMessage`;
- `UserAccount`;
- `UserProfile`;
- `UserRefreshSession`.

Every `DiscussionRoom` belongs to exactly one `Animal`. An `Animal` may have multiple discussion rooms. A room without an animal is not valid. `DiscussionRoomType` distinguishes the single `General` room from additional `Topic` rooms.

Discussion messages reference `UserAccount` directly. Public user data belongs to `UserProfile`, while the initial name-only access is maintained through `UserRefreshSession`. The API issues short-lived JWT access tokens and refresh tokens. Access tokens are not persisted, and `UserRefreshSession` stores only a hash of each refresh token. Optional login and password-hash fields, role, and status belong to `UserAccount`, not to the public profile or a refresh session. Application queries should select only the account or profile data required by a use case instead of always loading all three user-related entities.

The domain project must not depend on ASP.NET Core, Entity Framework Core, Ollama, or MediaWiki clients.

### ZooFinder.Infrastructure

Contains implementations of application abstractions.

Responsibilities:

- database access;
- Entity Framework Core configuration;
- MediaWiki API client;
- Ollama API client;
- external file storage if it is introduced later;
- other external service integrations.

## Dependency Direction

The intended dependency direction is:

```text
ZooFinder.Api
    → ZooFinder.Application
    → ZooFinder.Domain

ZooFinder.Infrastructure
    → ZooFinder.Application
    → ZooFinder.Domain
```

`ZooFinder.Api` references `ZooFinder.Infrastructure` only to register infrastructure implementations during application startup.

## Persistence

The initial persistence implementation uses Entity Framework Core with Microsoft SQL Server.

Repository abstractions are defined outside the infrastructure layer, while Entity Framework Core implementations, the database context, entity configurations, and migrations belong to `ZooFinder.Infrastructure`.

```text
Application repository abstraction
    → Infrastructure implementation
        → Entity Framework Core
            → Microsoft SQL Server
```

ADO.NET will be explored as an alternative persistence implementation in a later project stage. It should implement the same repository abstractions and be selected through dependency injection without changing controllers or application use cases.

To keep the persistence implementation replaceable:

- repository interfaces must not expose `DbContext`, `DbSet`, `IQueryable`, or EF Core-specific types;
- repository methods should describe application queries and operations explicitly;
- transaction boundaries must be controlled through an application-facing abstraction;
- SQL and provider-specific behavior must remain in `ZooFinder.Infrastructure`;
- application and domain code must not depend on change tracking or lazy loading.

Possible infrastructure implementations:

```text
EfAnimalRepository
AdoNetAnimalRepository
```

Entity Framework Core is the only implementation required for the initial version. The ADO.NET implementation is a future learning and experimentation task, not an initial delivery requirement.

## Animal Information Flow

The first version does not import or parse the entire Wikipedia animal catalog.

```text
Client request
    → ZooFinder.Api
    → MediaWiki API
    → short description, thumbnail, and Wikipedia URL
    → client response
```

The backend may cache successful responses to reduce repeated external requests. The exact caching and persistence strategy has not been selected yet.

The initial animal data returned to the client should contain:

```text
Title
ScientificName, when available
Description, when available
ImageUrl, when available
SourceUrl
```

`Animal` stores a provider-neutral information source, source item identifier, and language code, but does not persist the current source URL. The initial provider uses MediaWiki. Its Infrastructure implementation resolves the source URL when the response is created. Optional description and image values may be cached in the animal record.

## Image Recognition Flow

The ASP.NET application does not host or execute the model directly. The model runs only inside a dedicated container with an HTTP API.

```text
Angular client
    → ZooFinder.Api
    → Ollama container
    → vision model result
    → ZooFinder.Api response
    → Angular client starts a separate animal catalog search
```

The initial implementation follows these steps:

1. The client uploads an image to ZooFinder.Api.
2. The API validates the file type and size.
3. The infrastructure recognition client sends the image and a fixed prompt to Ollama.
4. Ollama returns structured JSON with a common name, scientific name, and optional alternatives.
5. The API returns the recognition suggestion without calling the animal catalog.
6. The client may start a separate catalog search using the scientific name first and the common name as a fallback.

Example internal recognition result:

```json
{
  "isAnimal": true,
  "commonName": "Red fox",
  "scientificName": "Vulpes vulpes",
  "alternatives": []
}
```

Recognition results are suggestions and must not be presented as guaranteed identification. The initial generative model does not provide a reliable probability score.

## Initial API Direction

Possible endpoints for the first iteration:

```text
GET  /api/animals/search?query={query}
GET  /api/animals/{title}
POST /api/animal-recognitions
```

Discussion, authentication, and administration endpoints will be designed separately when those features are started. The first discussion iteration creates one `General` room for each animal, while the model allows additional `Topic` rooms later.

## Container Environment

The complete environment is expected to contain:

```text
frontend
api
database
recognition
```

The recognition container is available only through the internal Docker network. The frontend never calls Ollama directly.

The selected model name and recognition service URL must be provided through configuration. Replacing the model or recognition provider must not require controller or frontend changes.

## Conventions

- All code, identifiers, comments, documentation, and README files are written in English.
- Comments explain non-obvious decisions and must not repeat what the code already expresses.
- External services are accessed through interfaces defined outside the infrastructure layer.
- API controllers coordinate HTTP concerns and delegate application work to application services.
- Infrastructure details must not leak into the domain model.

## Open Questions

- Which animal information should be persisted and which should only be cached?
- How long should Wikipedia responses remain cached?
- What image size and file types should the recognition endpoint accept?
- Should uploaded images be deleted immediately after recognition?
- How long should name-only user sessions remain valid?
- How will discussion moderation work?
- Who will be allowed to create additional discussion rooms for an animal?
