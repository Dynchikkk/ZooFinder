# ZooFinder Development Process

## Verification

- Before the final development stage, do not add automated tests, test projects, dependencies, or runners.
  Add them at the final stage or when explicitly requested.
- Verify code changes by building and performing necessary manual checks.
- Compilation does not verify SQL Server behavior or external integrations.
- For documentation-only changes, check links, paths, and consistency with code.

From the repository root:

```powershell
dotnet build apps/backend/ZooFinder/ZooFinder.slnx
```

With dependencies already restored:

```powershell
dotnet build apps/backend/ZooFinder/ZooFinder.slnx --no-restore -m:1 -p:UseAppHost=false
```

## Database Migrations

- Do not create migrations or a snapshot while the model is forming.
- After model agreement, create one Initial migration for the full schema.
- Keep the context, configurations, data sources, and design-time factory.
- Mappings do not initialize the database.

See [database settings](backend/infrastructure.md#connection-and-migrations).

## Documentation and Changes

### Content and Ownership

- Write documentation in English.
- Update affected behavior/rules within the same task; omit update dates.
- Document decisions, responsibilities, behavior, and important limits.
- Avoid file inventories, method walkthroughs, standard-library tutorials, and duplicate completion reports.
- Keep each decision in its owning document and link to it elsewhere.
- Keep AGENTS.md concise.
- Distinguish current implementation, future plans, and verified behavior.

| Topic | Document |
| --- | --- |
| Development process | development.md |
| Global architecture | architecture.md |
| Formatting | code-style.md |
| Layer/scenario decisions | Relevant layer document |

### Approval and Scope

- Root README.md, AGENTS.md, and documents directly under docs/ require prior discussion before changes.
- Discuss required documentation updates with the corresponding decision; do not leave them outdated.
- Clarify significant architectural uncertainty.
- Routine work within agreed rules needs no repeated approval.
- The user's latest instructions take precedence; plans do not override mandatory rules.
- Do not reformat unrelated code without a separate request.
