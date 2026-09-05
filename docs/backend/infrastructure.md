# Infrastructure Layer

`ZooFinder.Infrastructure` implements Application ports for persistence, external information, recognition, security, and transactions.

> **Status:** In progress. This document may change.

## Structure

```text
ZooFinder.Infrastructure/
├─ Persistence/
│  ├─ Configurations/
│  ├─ Repositories/
│  └─ Migrations/
├─ AnimalInformation/
├─ Recognition/
├─ Security/
└─ DependencyInjection.cs
```

## Port Implementations

| Application port | Infrastructure implementation |
| --- | --- |
| `IAnimalRepository` | Entity Framework Core repository |
| `IUserAccountRepository` | Entity Framework Core repository |
| `IUserProfileRepository` | Entity Framework Core repository |
| `IUserRefreshSessionRepository` | Entity Framework Core repository |
| `IAnimalInformationService` | MediaWiki service |
| `IAnimalRecognitionProvider` | Ollama provider |
| `IAccessTokenProvider` | JWT provider |
| `IRefreshTokenGenerator` | Cryptographic token generator |
| `IRefreshTokenHasher` | Refresh-token hash implementation |
| `IUnitOfWork` | Entity Framework Core transaction |

## Persistence

Entity Framework Core uses Microsoft SQL Server.

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
| `UserRefreshSession` | Indexed `UserAccountId` and `RefreshTokenHash` |

The first discussion transaction creates the animal when absent, creates its `General` room, and stores the first message.

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

`DependencyInjection.cs` registers Infrastructure implementations and their configuration. `ZooFinder.Api` calls this registration from the composition root.
