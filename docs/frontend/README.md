# Frontend Plan

**Status:** `apps/frontend` contains only `.gitkeep`. Angular/TypeScript is the selected frontend direction;
there is no Angular project, dependency manifest, application code, or implemented screen yet.

Backend capabilities and remaining integrations are described in [Backend](../backend/README.md).
The product sequence is described in the [roadmap](../planning/parks-and-recognition-roadmap.md).

## Planned User Flows

- Select a park, photograph/upload an animal, view candidates, and open park information or Wiki.
- Recognize an animal without a selected park.
- Search external/local animal catalogs and view the shared card/discussion.
- Create/edit parks and memberships, including local text and publication.
- Preview and confirm CSV imports.
- Submit/view connection requests and record approval/manual payment.
- Register/login, edit a profile, and read/write discussions.

Parks work without a species list. New park-management and connection screens are available to everyone in v1.
Existing account/message-write requirements remain applicable to their own screens.

## Suggested Feature Organization

The following is a proposed layout, not an existing or separately finalized frontend architecture:

```text
src/app/
├─ core/
├─ shared/
└─ features/
   ├─ animals/
   │  ├─ catalog/
   │  └─ recognition/
   ├─ parks/
   │  ├─ catalog/
   │  ├─ animals/
   │  ├─ import/
   │  └─ connections/
   ├─ auth/
   ├─ users/
   └─ discussions/
```

Core handles application-wide composition/session infrastructure; shared contains reusable presentation
components. Business UI belongs to features. Final Angular setup and component conventions will be chosen
when frontend implementation starts.

## API Boundary and Display Rules

The browser communicates with ZooFinder.Api, not directly with Wiki, Python, or the database.
Recognition and catalog are separate operations. A returned ParkAnimalId opens a park card; otherwise
scientific/common names seed a separate catalog search.

Source URLs come from the information provider. Lists and fallback cards may lack a URL or image;
the UI must support explicit empty states. Local park descriptions supplement shared information.

LocalAnimalId/HasStartedDiscussion indicates a saved shared card and General room, which may have no messages.
A shared discussion belongs to the species card, not a park. Deleted messages display metadata without content.

Cursor values are returned unchanged to request the next page. Recognition suggestions, uncertain results,
technical errors, suspended parks, and a missing optional species list must remain distinguishable.
