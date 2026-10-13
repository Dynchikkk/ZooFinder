# Infrastructure Layer

Infrastructure implements Application dependencies with EF Core/SQL Server and Auth security.
See [Backend](README.md) for pending integrations.

## Placement Rules

| Application owner | Infrastructure placement |
| --- | --- |
| Modules/<path> | Modules/<path> |
| Common/<Module>/<path> | Modules/<Module>/<path> |

- Infrastructure has Modules and Persistence roots; no Common root.
- Merge matching folders without overwriting files or changing Application contract ownership.
- Keep DataSources flat.
- Dependencies use Dependencies/<Name>/ with their technical settings and transport DTOs.
- Application service contracts remain in Application; avoid empty integration scaffolding.
- Context, factory, mappings, and database cursors belong to Persistence.
- DI helpers belong to API. Infrastructure must not reference API.
- Resolve Hub/event transport placement during implementation.
- Do not introduce a generic repository or UnitOfWork.

## Persistence Rules

### Reads and Writes

- Business reads use AsNoTracking.
- Writes use tracked entities and change only operation-owned fields.
- All writes go through SaveChangesAsync.
- Do not use direct ExecuteUpdate/ExecuteDelete or physical deletes.
- When navigations reference stored entities, track only new rows; avoid attaching detached graphs.

### Timestamps and Deletion

DbContext supplies UTC timestamps through TimeProvider.

| Operation | Behavior |
| --- | --- |
| Insert | Set creation/update timestamps |
| Update | Preserve creation/deletion fields |
| Remove | Soft-delete and set deletion/update timestamps |
| Restore | Clear deletion state/time; preserve ID/creation time |

Restore accepts only a tracked, previously deleted entity.
Relationships use ClientNoAction: deletion does not cascade or clear foreign keys.

### Visibility

- Filters hide deleted rows and children of deleted parents.
- History retains deleted messages/authors, while reapplying room/animal visibility.
- Application hides deleted message content.
- Deleted source identities, park slugs, and memberships remain reserved.

### Unique Keys

| Entity | Unique key |
| --- | --- |
| Animal | InformationSource, LanguageCode, SourceItemId |
| DiscussionRoom | AnimalId, Name |
| UserProfile | UserAccountId |
| UserAccount | Non-null Login |
| UserRefreshSession | RefreshTokenHash |
| Park | Slug, including deleted rows |
| ParkAnimal | ParkId, AnimalId, including deleted rows |
| ParkConnectionRequest | ParkId for non-deleted Submitted/AwaitingPayment requests |

The unfinished-request index uses SQL Server filter syntax.
Provider changes require reviewing constraints, comparisons, and transactions.
No explicit collations or SQL CHECK constraints are configured.

## Atomicity and Concurrency

### Concurrency Tokens

| Entity | Checked fields |
| --- | --- |
| All entities | IsDeleted |
| Refresh session | Token hash, revocation, and expiry in addition to IsDeleted |
| Connection request | Status in addition to IsDeleted |

Other field edits have no general row-version protection.

### Transaction Boundaries

| Operation | Transaction |
| --- | --- |
| Account registration | One SaveChanges for account/profile/initial session |
| Shared card/General-room registration | Serializable |
| Park slug reservation and connection submission | Serializable |
| Refresh rotation/revocation | RepeatableRead |
| Connection transition, profile/message update | RepeatableRead |
| Revoke all sessions | Serializable |
| Membership add/restore batch, update, removal | Default SaveChanges; no explicit isolation |

Explicit transactions protect related-row checks.

### Registration

- Shared registration recognizes only source/item/language.
- Deleted cards or General rooms produce a conflict.
- Storage errors stop shared registration; there is no second lookup/recovery.
- Account registration returns false only after confirming a competing login.
- Refresh rotation checks the expected token hash.

### Membership Batches

- Load canonical tracked Animals and existing links, including deleted rows.
- Preserve active links; restore deleted links with supplied fields; insert missing links.
- Return results in input order.
- Shared cards/rooms were committed earlier and may survive a failed link batch.
- The unique index rejects concurrent duplicate inserts; no automatic recovery/retry is implemented.

**Parent checks are not protected until commit.**
A park/animal can be soft-deleted after checking it, leaving a successfully saved but filtered-out link.

### Failure and Verification Limits

- Failed operations do not clear/detach the tracker; callers stop and the scope disposes the context.
- Deadlocks/database errors remain possible.
- No automatic retry or provider-specific deadlock mapping is implemented.
- Message persistence and event publication are separate, without an outbox.
- Builds do not verify live SQL Server concurrency or external integrations.

### Cursors

Local catalog/history cursors bind to a search/room scope and use CreatedAtUtc/Id ordering.
They are continuation values, not authorization.
Invalid or mismatched cursors produce a validation error.

## Auth Security

| Component | Behavior |
| --- | --- |
| Password hashing | Identity versioned format; malformed hashes fail verification |
| Refresh token | 32 random bytes, Base64url encoding; persist only SHA-256 hash |
| Access token | HS256; sub, role, jti, UTC lifetime, issuer, and audience |

- The boolean password-verification contract does not support automatic rehashing.
- Inactive/deleted accounts cannot receive access tokens.
- Refresh revocation does not invalidate issued access tokens.
- [API](api.md) owns bearer validation and composition.

### JWT Settings

Settings belong to Dependencies/AccessToken.

| Setting | Requirement |
| --- | --- |
| Issuer/audience | Required |
| Base64 signing key | At least 32 decoded bytes; secure runtime configuration |
| Lifetime | Default 15 minutes; range 1 second–1 hour |

Invalid settings fail construction.

## Integration Boundaries

### Wiki

- Keep transport DTOs/settings inside the adapter.
- Map unavailability to AnimalInformationUnavailableException.
- Keep cancellation and invalid output distinct.
- Source URLs come from verified provider identities, not model output.

### Recognition

- Use an HTTP adapter to local Python.
- Python selects/loads the model through configuration.
- Python has no database/Wiki access.
- Application contracts exist; the wire schema is not agreed.

Model/research plans belong to the [roadmap](../planning/parks-and-recognition-roadmap.md).

## Connection and Migrations

| Configuration | Source |
| --- | --- |
| Factory override | ConnectionStrings__ZooFinder |
| Factory fallback | ConnectionStrings:ZooFinder; local Windows authentication |
| Runtime connection | Registration pending |

Do not commit credentials.
The application neither initializes nor migrates the database at startup.

Follow [migration policy](../development.md#database-migrations):
retain the factory/context and create Initial only after model agreement.
