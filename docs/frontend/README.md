# Frontend Architecture

The ZooFinder frontend is an Angular application written in TypeScript.

> **Status:** In progress. This document may change.

## Responsibilities

- animal search and result cards;
- animal details pages;
- image upload and recognition results;
- user registration and profile editing;
- discussion history and message composition;
- authentication session handling.

## Feature Structure

```text
src/app/
├─ core/
├─ shared/
└─ features/
   ├─ animals/
   ├─ recognition/
   ├─ users/
   ├─ auth/
   └─ discussions/
```

`core` contains application-wide services and configuration. `shared` contains reusable presentation components and utilities. Business UI is grouped by feature.

## Animal Flow

```text
Text search → catalog cards → animal page → discussion

Image upload → recognition result → catalog search → animal page → discussion
```

Recognition and catalog are separate operations. The frontend starts catalog search with the scientific name and uses the common name as a fallback.

## API Boundary

- The frontend communicates only with `ZooFinder.Api`.
- MediaWiki and Ollama are not called directly.
- API cursors are opaque and returned unchanged for the next page.
- `LocalAnimalId` indicates that the animal has a local discussion.
- Recognition results are displayed as suggestions.
- Authentication state is managed through access-token and refresh-session flows.

## UI Rules

- Search cards contain title, scientific name, and image when available.
- Animal pages contain full provider information and discussion state.
- Existing discussions show recent messages on the animal page.
- Animals without a discussion show a start-discussion action.
- Missing descriptions and images use explicit empty states.
