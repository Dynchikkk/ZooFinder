# ZooFinder Development Process

Mandatory rules for the current development stage. General principles are described in
[Architecture](architecture.md), component organization in the layer documents,
and formatting in [Code style](code-style.md).

## Verification

- Before the final development stage, do not write automated tests, create test projects,
  or add test dependencies or runners.
- Add tests at the end, once the main functionality and model have stabilized, or when explicitly requested by the user.
- At this stage, verify changes by building the solution and performing necessary manual checks.

Build from the repository root:

```powershell
dotnet build apps/backend/ZooFinder/ZooFinder.slnx
```

If dependencies have already been restored, a sequential build can be used:

```powershell
dotnet build apps/backend/ZooFinder/ZooFinder.slnx --no-restore -m:1 -p:UseAppHost=false
```

For documentation-only changes, check links, paths, and consistency with the current code.
A build does not verify business behavior, SQL Server operation, or integration with external providers.

## Database Migrations

- While the database model is still being formed, do not create EF migrations or a model snapshot.
- After the model is agreed upon, create one initial migration named `Initial` for the complete current schema.
- Keep the DbContext, entity configurations, data sources, and design-time factory:
  they are required for the future initial migration.

Previous migrations and the snapshot have been removed. At this stage, EF mappings do not mean
that the schema has been created or updated in a live database.

Connection settings and commands for future database initialization are documented in
[Infrastructure](backend/infrastructure.md#connection-and-migrations).

## Documentation and Instructions

- Keep `AGENTS.md` concise: general instructions and links to mandatory topic-specific documents.
  Store detailed agreed rules in those documents rather than duplicating them in `AGENTS.md`.
- Write and maintain project documentation in English.
- Documentation must always match the current project. When changing code, update affected descriptions
  within the same task; do not defer synchronization until the end of development.
  Do not add synchronization or update dates.
- Root documentation includes README.md and AGENTS.md at the repository root, as well as general documents
  directly under docs/. Change these only after prior discussion with the developer.
  Do not independently change general rules or root descriptions.
- If a project change requires a root-document update, discuss that update together with the corresponding
  decision first. The discussion requirement does not permit leaving documentation outdated.
- Update layer and scenario documentation alongside project changes within the agreed task scope.
- Record general process rules here, global architecture principles in architecture.md,
  and formatting in code-style.md. Describe a layer's structure and specific decisions in its own documentation.
- When significant uncertainty arises, clarify the decision with the developer before making disputed changes.
  Routine decisions within already agreed rules do not require repeated discussion.
- In case of discrepancies, follow the user's latest instructions and mandatory development rules.
  An outdated layer description or roadmap proposal does not override those rules.
- Distinguish the current code state from agreed future behavior and research proposals.
- Do not reformat code unrelated to the current task without a separate request.
