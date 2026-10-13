# Application Layer

Application contains business scenarios, validation, contracts, and dependency interfaces.
It depends on Domain and has no EF, HTTP, SignalR, or DI-framework dependency.

See [Architecture](../architecture.md) for global boundaries and [Backend](README.md) for implementation status.

## Organization Rules

### Module Roots

| Root | Purpose |
| --- | --- |
| Modules | Primary business functionality |
| Common | Business functionality shared across primary modules |
| Shared | Technical components independent of business modules |

Modules and Common support recursive child modules.

- Module roots contain purpose folders, not individual C# files.
- Create only required Services, DataSources, Dependencies, Contracts, Validators, Constants,
  Settings, Extensions, Exceptions, and Modules folders.
- Parent components and children may coexist.
- Nesting follows responsibility; adding a child does not require moving or wrapping existing services.

### Component Placement

| Component | Placement |
| --- | --- |
| Service | Services/<Name>/ with interface, implementation, and service contracts |
| Data source | Flat DataSources/ folder; optional contracts beside the interface |
| Technical dependency | Dependencies/<Name>/ with its own contracts |
| Constants, helpers, settings | With their owning module or dependency |

- Service folders omit the Service suffix. A service may implement several related operations.
- Data sources serve coherent tasks across entities; do not create one per table, method, or service.
- Reuse suitable contracts; avoid redundant DTOs and empty scaffolding.
- Dependency implementations stay outside Application.
- Dependency contracts must not import service requests/responses.
- One type per C# file, except related DTOs in `*Contracts.cs`.
- Service implementations contain no additional nested classes or records.

### Naming

- Namespaces match placement.
- Entity-based folders/namespaces are plural; entity/service classes are singular.
- Task names stay unchanged.
- Resolve naming collisions without aliases.

### Validation and Settings

| Responsibility | Owner |
| --- | --- |
| Normalize input | Service; reusable normalization in the owner's Extensions |
| Validate prepared values | Validator |
| Scenario configuration | Plain Application settings |
| Connection/transport configuration | Infrastructure implementation |

Validators do not normalize or mutate input.
Reusable normalization neither validates nor throws request-validation errors.
Application settings do not read IConfiguration or appsettings.

## Ownership

Follow the recursive sibling/ancestor rules in [Architecture](../architecture.md).

- Cross-branch workflows belong to the nearest common parent or an appropriate Common scenario.
- Components shared only by one parent's children stay at that parent.
- Consumers may read other modules' Domain data through their own sources, preserving visibility.
- Do not copy foreign sources or bypass another module's business operation.
- Atomic operations use explicit source methods; no generic UnitOfWork is introduced.

| Current owner | Shared responsibility |
| --- | --- |
| Common/Animals | Information and registration; no extra Registration/Information wrapper |
| Parks | Catalog/membership sources and shared park/membership validators |
| Discussions | Shared source and account validation |
| Auth | Storage/security dependencies; complete AuthService remains under Authentication |
| Animals/Recognition | Its own park-context source, without a Parks-private dependency |

Import/Connections and Rooms/Messages retain their specific components.
Auth has no separate Sessions service.

## Animals

### Shared Registration

- Supported information languages: en and ru.
- Park addition/import and explicit discussion creation use shared registration.
- Registration atomically creates/reuses an Animal and General room, then returns their IDs.
- An exact saved identity can be reused without a provider call.
- Deleted cards or General rooms produce a conflict.

Identity rules are in [Domain](domain.md#animals-and-parks).

### Catalog

- Search supports external and saved cards; it does not persist them.
- LocalAnimalId/HasStartedDiscussion means a shared card/room exists, even with no messages.
- Catalog detail requires provider information and currently has no cached fallback.

### Recognition

Input: image stream, language, and optional ParkId.
Application checks readability, file metadata, declared size up to 10 MiB, and provider-output consistency.
Actual file validation belongs to API.

| Park context | Behavior |
| --- | --- |
| No park or empty published catalog | General recognition |
| Unknown park | Not-found |
| Suspended park | Conflict |
| Populated published catalog | Candidates supplied as context |

Candidates do not force an in-catalog answer.

- Returned known IDs must belong to the supplied context.
- Known names come from saved cards; a missing scientific name remains null.
- Outside species may return by name without a park link.
- Reject duplicate/foreign IDs, contradictory NoAnimal output, empty recognized names, and invalid execution metadata.
- Recognition does not save images/cards/memberships or query Wiki.
- Model output does not supply trusted source links.

## Parks

All park operations are open in v1, without owner, role, or payment gates.
[Domain](domain.md#animals-and-parks) defines park states and connection transitions.

### Membership Reads

- Lists use cached cards and hide unpublished links by default.
- Direct reads allow editing unpublished links.
- Park-card details use live information.
- Provider unavailability/null falls back to cached data without SourceUrl.
- Cancellation and invalid output are not converted into provider unavailability.

### Addition and Editing

| Operation/state | Behavior |
| --- | --- |
| AddOrRestore: missing link | Create with supplied local description/publication |
| AddOrRestore: deleted link | Restore original ID with supplied local fields |
| AddOrRestore: active link | Return unchanged |
| Update | Explicitly edit local description/publication |

The batch variant follows the same rules.
Repeated addition/import preserves active links' settings.

### Imports

Imports accept up to 200 decoded rows. CSV decoding belongs to API.

1. Preview performs no writes and requires an unambiguous provider match or explicit identity.
2. Apply validates/retrieves all selected details before shared registration.
3. Links are saved as one atomic batch.

Shared registration uses earlier separate transactions.
Cards/rooms may remain after a link-batch failure.
Empty imports and catalogs are supported.

### Connections

- Approval/payment confirmation is idempotent for identical repeated input.
- Conditional writes reject stale transitions.
- Finished requests cannot reopen.
- Payment does not change park status.

## Authentication and Profiles

| Scenario | Behavior |
| --- | --- |
| Registration | Lowercase login; active User, profile, and first session created atomically |
| Initial profile | Display name equals login |
| Login | Creates a separate session |
| Refresh | Checks account/session activity and rotates the expected token hash |
| Profile update | Requires an active owner |

Only password/refresh-token hashes are stored.
AuthSettings controls session lifetime; TimeProvider supplies UTC.

## Discussions

- Explicit room creation requires an active account; automatic park registration does not.
- Messages require an active account, open room, and 1–4,000 normalized characters.
- Authors, moderators, and administrators may edit/delete messages.
- Deleted history retains metadata with null content.
- Event contracts belong to the publisher dependency.
- Persistence precedes publication without a shared transaction or outbox.

## Shared Contracts

Shared owns Pagination, ErrorHandling, and Language.

| Pagination | Rules |
| --- | --- |
| Offset | Zero-based; default size 20, maximum 100 |
| Cursor | Opaque values; used by local catalog/history |

RequestValidation, NotFound, Conflict, Unauthorized, and Forbidden exceptions represent scenario outcomes.
[API](api.md) owns their HTTP mapping.
