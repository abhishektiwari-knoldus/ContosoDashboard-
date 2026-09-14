# Data Model: Document Upload and Management

## Document

Represents one accepted work-related file and its searchable metadata.

| Field | Type | Rules |
|---|---|---|
| DocumentId | integer | Primary key; required for consistency with existing entities |
| Title | text | Required; bounded length; user-editable |
| Description | text nullable | Optional; bounded length |
| Category | text | Required; one of Project Documents, Team Resources, Personal Files, Reports, Presentations, Other |
| Tags | text nullable | Optional serialized tag values; bounded input and normalized for search |
| OriginalFileName | text | Display-only source name; never used as a storage path |
| FilePath | text | Required relative storage name; generated identifier and extension only |
| FileType | text | Required MIME type; maximum 255 characters |
| FileSize | integer | Required; greater than zero and no more than 25 MB |
| UploadedDate | UTC datetime | Required; set by the service |
| UploadedByUserId | integer | Required foreign key to User |
| ProjectId | integer nullable | Optional foreign key to Project |
| TaskId | integer nullable | Optional foreign key to TaskItem; when set, ProjectId matches the task project |
| IsDeleted | boolean | Required; deleted documents are excluded from all normal queries |
| CreatedDate | UTC datetime | Required |
| UpdatedDate | UTC datetime | Required |

### Document validation and state

- A document enters the persisted state only after extension/MIME/size validation and the local scan gate succeed.
- `FilePath` is generated before database insertion and is never derived from an untrusted path.
- A replacement writes a new generated file and updates metadata only after the new file passes validation; the prior file is removed after the new state is persisted.
- Deletion requires a confirmed user action, marks the record unavailable, removes bytes from local storage, and records activity.
- All normal queries add `!IsDeleted` and an access predicate.

## DocumentShare

Represents explicit access granted to a user or team.

| Field | Type | Rules |
|---|---|---|
| DocumentShareId | integer | Primary key |
| DocumentId | integer | Required foreign key to Document |
| UserId | integer nullable | Direct recipient; mutually exclusive with TeamId |
| TeamId | text nullable | Team recipient identifier; compatible with existing department/team representation |
| SharedByUserId | integer | Required foreign key to User |
| SharedDate | UTC datetime | Required |
| IsActive | boolean | Required; inactive shares no longer grant access |

- A unique constraint should prevent duplicate active shares for the same document and recipient.
- A recipient removed from the relevant team loses access through that team share unless another active permission applies.
- The initial UI must prevent a share from having neither or both recipient types.

## DocumentActivity

Immutable audit record for document actions.

| Field | Type | Rules |
|---|---|---|
| DocumentActivityId | integer | Primary key |
| DocumentId | integer | Required foreign key; retained for audit lookup |
| UserId | integer | Required actor foreign key |
| Action | text | Required: Upload, Download, Preview, MetadataEdit, Replace, Delete, Share |
| OccurredDate | UTC datetime | Required UTC timestamp |
| Details | text nullable | Optional safe summary; must not contain file bytes or secrets |

- Activity records are append-only from application behavior.
- Reports aggregate by file type, uploader, action, and time range.

## Relationships

- User 1-to-many Document through `UploadedByUserId`.
- Project 1-to-many Document through optional `ProjectId`.
- TaskItem 1-to-many Document through optional `TaskId`.
- Document 1-to-many DocumentShare.
- Document 1-to-many DocumentActivity.
- User 1-to-many DocumentShare as grantor and optional recipient.
- Project membership and existing role values remain the source of truth for project access.

## Query and indexing expectations

- Index `UploadedByUserId`, `ProjectId`, `TaskId`, `Category`, `UploadedDate`, `FileType`, and `IsDeleted` as needed for list/search workloads.
- Use a bounded query with filtering, sorting, and pagination before materializing results.
- Search predicates must be combined with the access predicate before execution; filtering after materialization is prohibited for protected data.
