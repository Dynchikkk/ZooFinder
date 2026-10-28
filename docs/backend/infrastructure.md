# Infrastructure Layer

Infrastructure implements Application dependencies with EF Core/SQL Server, Auth security, Wikipedia, and Python recognition HTTP.
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

- The HTTP adapter uses the MediaWiki Action API on the fixed en/ru Wikipedia hosts.
- Identity is `wikipedia` + canonical positive numeric page ID + language; page moves preserve identity.
- Search preserves provider ranking and binds continuation to the exact query, language, and page size.
- Missing, redirect, non-article, and disambiguation pages cannot supply card details.
  Search can return fewer items than requested while still having a continuation.
- Search is general Wikipedia search, without taxonomic verification. ScientificName stays null;
  no name is guessed from article text. Wikidata enrichment is not implemented.
- Details contain a plain-text introductory extract (approximately 1,200 characters), an optional Wikimedia
  thumbnail, and an attribution URL derived from the verified page ID. Empty extracts/images are allowed.
- HTTP/network failures, timeouts, and temporary API errors map to AnimalInformationUnavailableException.
  Caller cancellation propagates; malformed, oversized, or inconsistent output raises InvalidOperationException.
- Only successful search/details results are cached; missing details and errors are not cached.
  There are no automatic retries or stale-cache fallback.

Transport DTOs and settings stay inside Dependencies/AnimalInformation.
API composition must supply HttpClient, settings, and a shared IMemoryCache with a size limit.
Each cached response has size 1; the host chooses the maximum number of entries.
UserAgent is required and must identify the client and provide real operator contact information, following
the [Wikimedia policy](https://foundation.wikimedia.org/wiki/Policy:Wikimedia_Foundation_User-Agent_Policy).
Defaults: 10-second request timeout, 2 MiB response limit, 2-minute search cache, 15-minute details cache.
The dependency's settings validator checks header syntax and configuration limits at client construction without mutating settings.
The timeout covers both headers and body; an earlier HttpClient timeout may also end a request.

Basic en/ru search, continuation, details, missing pages, disambiguation, and cache reuse have been checked
manually against Wikipedia. Runtime API composition and end-to-end flows remain pending.

### Recognition

- The Python HTTP adapter implements IAnimalRecognitionProvider using the [recognition protocol](recognition-protocol.md).
- The Python service/model and live integration remain pending.

Model/research plans belong to the [roadmap](../planning/parks-and-recognition-roadmap.md).

## Connection and Migrations

| Configuration | Source |
| --- | --- |
| Factory override | ConnectionStrings__ZooFinder |
| Factory fallback | ConnectionStrings:ZooFinder; local Windows authentication |
| Runtime connection | API registration using ConnectionStrings:ZooFinder |

Do not commit credentials.
The application neither initializes nor migrates the database at startup.

Follow [migration policy](../development.md#database-migrations):
retain the factory/context and create Initial only after model agreement.
