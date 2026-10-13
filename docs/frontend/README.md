# Frontend Plan

Angular/TypeScript is selected. apps/frontend currently contains only .gitkeep.
Project setup and component conventions will be agreed when implementation starts.

## Main Flow

1. Select a park.
2. Upload a photograph.
3. View recognition candidates.
4. Open the park card or Wiki information.

Also support:

- General recognition and catalog search.
- Park/membership editing.
- Import preview and confirmation.
- Connection requests and manual payment tracking.
- Authentication, profiles, and discussions.

Park management is open in v1.
Protected account/message operations retain their permissions.

## Organization and API Boundary

| Area | Purpose |
| --- | --- |
| core | Composition and session infrastructure |
| shared | Reusable presentation components |
| Business modules | Scenario-specific UI |

Detailed nesting is not finalized.
The browser communicates with ZooFinder.Api, never directly with Wiki, Python, or SQL Server.

## Display Rules

- Recognition and catalog are separate operations.
- ParkAnimalId opens a park card; otherwise names seed catalog search.
- Source links come from the provider; support missing images/URLs.
- Local park text supplements the shared card.
- A saved card/General room may have no messages.
- Deleted messages retain metadata without content.
- Pass cursors back unchanged.

Distinguish these outcomes:

- Recognition uncertainty.
- No animal in the image.
- Technical error.
- Unknown or suspended park.
- Empty optional park catalog.

See [Backend](../backend/README.md) for status
and the [roadmap](../planning/parks-and-recognition-roadmap.md) for remaining work.
