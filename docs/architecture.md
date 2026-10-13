# ZooFinder Architecture

## Layers and Dependencies

| Layer | Responsibility |
| --- | --- |
| Domain | Subject model; no dependency on other layers |
| Application | Business scenarios and their dependency interfaces |
| Infrastructure | Persistence and technical integrations |
| API | Transport and composition root |

Dependencies point inward:

```text
API → Infrastructure → Application → Domain
```

- Application must not import Infrastructure or API.
- Infrastructure must not import API.
- Business scenarios remain independent of storage and transport.
- Technical implementations preserve business rules.
- Controllers adapt inputs/outputs without duplicating scenarios.
- Composition connects interfaces and implementations.
- Circular dependencies are prohibited.

## Module Boundaries

| Relationship | Allowed dependency |
| --- | --- |
| Siblings | No imports of each other's private services, sources, or use-case contracts |
| Child → ancestor | Explicitly shared ancestor components |
| Parent → child | Public operation contracts for coordination |
| Shared ancestor component → child | No dependency on private child components |

These rules also apply to interface-based dependencies.

- Modules may own components and recursive child modules.
- Shared contains independent technical components.
- Common contains shared business functionality and cannot import private primary-module components.
- Common/Shared exceptions do not permit cycles.
- Layer organization follows responsibility; folder rules do not automatically transfer between layers.

## Contract Ownership

- The consuming scenario owns a dependency interface.
- The responsible technical layer implements it.
- Dependency contracts must not import consuming-service responses.
- External transport DTOs stay at the integration boundary.
- Define shared types once by purpose.
- Do not copy types or move them into common folders solely to bypass dependencies.

## Consistency and Decisions

- Reads/writes preserve domain constraints and visibility.
- State each operation's atomicity explicitly.
- Sequential writes and event publication do not automatically share a transaction.
- Separate implemented guarantees, plans, and actual verification.
- Discuss significant uncertainty in ownership or dependencies.
- Record decisions at their proper level; do not infer a global rule from one case.

## Layer Rules

- [Domain](backend/domain.md).
- [Application](backend/application.md).
- [Infrastructure](backend/infrastructure.md).
- [API](backend/api.md).
- [Frontend](frontend/README.md).
