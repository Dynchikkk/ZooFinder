# ZooFinder Documentation

Documentation describes the current project state and is maintained alongside project changes.

## Development Rules

These documents contain the mandatory rules referenced by [AGENTS.md](../AGENTS.md).

- [Development process](development.md): verification, tests, migrations, synchronization, and discussion of root-document changes.
- [General architecture](architecture.md): layer responsibilities, contract ownership, and dependency direction.
- [Code style](code-style.md): line length, braces, parameters, LINQ, and blank lines.

## Current State

- [Backend overview](backend/README.md).
    - [Domain](backend/domain.md): entities, relationships, and model constraints.
    - [Application](backend/application.md): feature-based organization rules, modules, services, data sources, and scenarios.
    - [Infrastructure](backend/infrastructure.md): implemented persistence and planned integrations.
    - [API](backend/api.md): the current scaffold and requirements for the future HTTP layer.
- [Frontend overview](frontend/README.md).

Layer descriptions distinguish implemented components from future work. An Application contract does not
mean that the corresponding HTTP endpoint, provider, or screen has been implemented.

## Plans

- [Completed Domain/Application changes for parks](planning/domain-application-changes.md).
- [Product and model-research roadmap](planning/parks-and-recognition-roadmap.md).

Plans describe future work and proposals. They do not replace mandatory development rules or confirm
that the listed capabilities are implemented.
