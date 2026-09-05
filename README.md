# ZooFinder

ZooFinder is a web platform for animal discovery, image-based animal recognition, and animal-specific discussions.

> **Status:** In progress. Unfinished modules may change during implementation.

## Core Features

- Search animals by name.
- View animal information, images, and source links.
- Recognize an animal from an uploaded image.
- Create and participate in animal-specific discussions.
- Register by display name and maintain a user session.

Animals returned by search are not stored automatically. An animal is added to the local database when its first discussion is created.

## Technology

- .NET 10 and ASP.NET Core
- Entity Framework Core and Microsoft SQL Server
- Angular and TypeScript
- MediaWiki API for animal information
- Ollama for image recognition
- Docker Compose

## Repository Structure

```text
ZooFinder/
├─ apps/
│  ├─ backend/
│  └─ frontend/
├─ docs/
├─ infra/
└─ scripts/
```

## Documentation

- [Backend architecture](docs/backend/README.md)
- [Frontend architecture](docs/frontend/README.md)

## Build

```powershell
dotnet build apps/backend/ZooFinder/ZooFinder.slnx
```
