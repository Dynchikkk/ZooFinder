# Domain Layer

Domain contains the subject model without dependencies on other layers.

## Organization

- Group entities by subject: Animals, Parks, Discussions, Users, BaseEntities.
- Entity-based folders/namespaces are plural; classes are singular.
- Namespaces match placement.
- Resolve naming collisions without aliases.

## Entities and Relationships

| Entity | Purpose |
| --- | --- |
| Animal | Sourced species card shared across parks |
| Park | Park information and Active/Suspended state |
| ParkAnimal | Park/Animal association with local description/publication |
| ParkConnectionRequest | Request, agreed price, and manual payment state |
| DiscussionRoom | General or Topic room belonging to an Animal |
| DiscussionMessage | Room message authored by a UserAccount |
| UserAccount | Login/password hash, role, and activity state |
| UserProfile | One public profile per account |
| UserRefreshSession | Multiple refresh sessions per account, stored as hashes |

All entities inherit IdEntity<TId>: identity, creation/update timestamps, and soft-deletion fields.

- Each Animal has rooms; each room has messages.
- Each message belongs to an account.
- An Animal may belong to several parks or none.
- ParkAnimal navigations hold associations.
- OwnerUserAccountId is optional park metadata, not an authorization rule.

## Animals and Parks

### Animal Identity

An Animal is identified by InformationSource + SourceItemId + LanguageCode.

- Scientific names/titles do not merge different identities.
- English and Russian cards are separate.
- SourceUrl is resolved by the provider, not stored.
- Search results are transient.
- Shared registration creates/reuses a card with an empty General room.

### Park Membership

ParkAnimal describes species membership, not an individual animal.

| Action | Result |
| --- | --- |
| Edit/remove membership | Shared card/chat stays |
| Re-add deleted membership | Restore original ID with supplied local fields |
| Re-add active membership | Return unchanged |

New parks are Active and may have no catalog.
Park operations are open to everyone in v1.

### Connection Requests

Connections record the commercial workflow separately from park availability.

| Current state | Allowed next states |
| --- | --- |
| Submitted | AwaitingPayment, Rejected, Cancelled |
| AwaitingPayment | Activated, Rejected, Cancelled |
| Activated, Rejected, Cancelled | None |

- Only one unfinished request exists per park.
- Agreement records amount/currency; payment is manual.
- Connection states do not gate park use.

## Discussions and Accounts

### Rooms and Messages

- Each Animal has one General room and may have Topic rooms.
- Room names are unique per Animal.
- Application exposes no General-room deletion.
- The database index protects the fixed General name, not uniqueness by room type or a deletion prohibition.
- Empty messages are invalid.
- Closed rooms and blocked accounts cannot receive writes.
- Deleted content is hidden; history metadata stays.

### Account States

| Property | Values |
| --- | --- |
| Role | None, User, Moderator, Administrator |
| State | None, Active, Blocked |

New local accounts are active Users.
None is invalid for persisted roles/states.

### Stored User Data

- Normalized logins are unique.
- Passwords and refresh tokens are stored as hashes.
- Access tokens and raw refresh tokens are transient.
- Each profile has one owner.
- DisplayName is public, non-unique, and 1–100 normalized characters.

[Infrastructure](infrastructure.md) owns mappings and database constraints.
[Application](application.md) defines scenario permissions and behavior.
