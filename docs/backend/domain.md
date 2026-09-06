# Domain Layer

`ZooFinder.Domain` defines the project entities, relationships, and domain rules. It has no dependencies on other projects.

## Structure

```text
ZooFinder.Domain/
├─ Animals/
├─ BaseEntities/
├─ Discussions/
└─ Users/
```

## Base Entity

All entities inherit from `IdEntity<TId>`.

| Field | Type | Purpose |
| --- | --- | --- |
| `Id` | `TId` | Primary identifier |
| `IsDeleted` | `bool` | Soft-deletion state |
| `CreatedAtUtc` | `DateTime` | Creation timestamp |
| `UpdatedAtUtc` | `DateTime` | Last update timestamp |
| `DeletedAtUtc` | `DateTime?` | Deletion timestamp |

## Relationships

```text
Animal 1 ─────── * DiscussionRoom
                         │
                         │ 1
                         ▼
              * DiscussionMessage * ─────── 1 UserAccount
                                              │
                                              ├──── 1 UserProfile
                                              │
                                              └──── * UserRefreshSession
```

## Animal

| Field | Type |
| --- | --- |
| `InformationSource` | `string` |
| `SourceItemId` | `string` |
| `LanguageCode` | `string` |
| `Title` | `string` |
| `ScientificName` | `string?` |
| `ShortDescription` | `string?` |
| `ImageUrl` | `string?` |
| `LastSynchronizedAtUtc` | `DateTime` |

Rules:

- `(InformationSource, LanguageCode, SourceItemId)` identifies one sourced animal.
- `SourceUrl` is resolved by the information provider and is not persisted.
- Search results are not persisted.
- An animal is persisted when its General discussion room is created. The room may initially contain no messages.

## DiscussionRoom

| Field | Type |
| --- | --- |
| `AnimalId` | `Guid` |
| `Type` | `DiscussionRoomType` |
| `Name` | `string` |
| `Description` | `string?` |
| `IsClosed` | `bool` |

`DiscussionRoomType` values:

```text
None = 0
General = 1
Topic = 2
```

Rules:

- A room belongs to one animal.
- Each animal has one `General` room.
- An animal may have multiple `Topic` rooms.
- Room names are unique within an animal.
- A `General` room cannot be deleted.

## DiscussionMessage

| Field | Type |
| --- | --- |
| `DiscussionRoomId` | `Guid` |
| `AuthorUserAccountId` | `Guid` |
| `Content` | `string` |
| `EditedAtUtc` | `DateTime?` |

Rules:

- A message belongs to one room and one user account.
- Empty messages are invalid.
- Closed rooms reject new messages.
- Blocked users cannot write messages.
- Deleted message content is hidden from regular users.

## UserAccount

`UserAccount` stores identity and authorization state.

| Field | Type |
| --- | --- |
| `Login` | `string?` |
| `PasswordHash` | `string?` |
| `Role` | `UserRole` |
| `Status` | `UserStatus` |

`UserRole` values are `None`, `User`, `Moderator`, and `Administrator`.

`UserStatus` values are `None`, `Active`, and `Blocked`.

New locally registered accounts use the `User` role and `Active` status. Their normalized login is unique and their password is persisted only as a hash. `None` is not a valid persisted role or status.

## UserProfile

`UserProfile` stores public user data.

| Field | Type |
| --- | --- |
| `UserAccountId` | `Guid` |
| `DisplayName` | `string` |

One account has one profile. `DisplayName` is not a credential, is not unique, and contains between 1 and 100 characters after normalization.

## UserRefreshSession

| Field | Type |
| --- | --- |
| `UserAccountId` | `Guid` |
| `RefreshTokenHash` | `string` |
| `ExpiresAtUtc` | `DateTime` |
| `LastUsedAtUtc` | `DateTime?` |
| `RevokedAtUtc` | `DateTime?` |

One account may have multiple refresh sessions. Only the refresh-token hash is persisted. Access tokens and raw refresh tokens are transient.
