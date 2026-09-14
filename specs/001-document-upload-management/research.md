# Research: Document Upload and Management

## Decision 1: Use the existing Blazor Server and EF Core layering

- **Decision**: Add document models, services, and Razor pages to the existing single `ContosoDashboard` project.
- **Rationale**: The application already separates Models, Data, Services, and Pages, and the constitution requires compatibility with the current Blazor Server architecture without a major rewrite.
- **Alternatives considered**: A separate document API or new frontend project would add deployment and authentication boundaries without delivering value for the offline training scope.

## Decision 2: Store metadata in SQLite and bytes outside `wwwroot`

- **Decision**: Persist document metadata and permissions in SQLite; store file bytes beneath an application-data upload root outside publicly served content.
- **Rationale**: Existing data uses EF Core SQLite, while files outside `wwwroot` cannot be fetched without an authorization check. Relative storage names keep the metadata portable.
- **Alternatives considered**: Storing bytes in the database would complicate list performance and local operations; storing under `wwwroot` would create an unsafe bypass around authorization.

## Decision 3: Use a storage interface with a local implementation

- **Decision**: Define `IFileStorageService` with upload, download, delete, and URL/stream access operations; implement `LocalFileStorageService` using generated relative names and `System.IO`.
- **Rationale**: The feature explicitly requires offline operation and a future Azure Blob replacement without changing document business logic. GUID-based names prevent collisions and path traversal from user filenames.
- **Alternatives considered**: Calling `System.IO` directly from Razor pages would violate layered design and make a cloud migration require UI and service changes.

## Decision 4: Use a local quarantine/validation gate for malware handling

- **Decision**: Route every upload through an `IDocumentScanService` before it becomes available. The offline implementation performs allowlist, content-signature, and validation checks and reports unsafe files as rejected/quarantined. Production documentation must require a real malware scanner before deployment.
- **Rationale**: The application must work without cloud services, but the feature still needs an explicit safety gate rather than silently treating extension checks as malware scanning.
- **Alternatives considered**: Rejecting all uploads makes the training feature unusable; accepting extension/MIME checks without a scan boundary would conceal a production security gap.

## Decision 5: Enforce access through one document service plus authorized byte endpoint

- **Decision**: `DocumentService` owns query scoping, role/project/share authorization, mutation orchestration, and audit creation. A Razor Page endpoint resolves the document through that service before streaming preview/download bytes.
- **Rationale**: Search, list, and direct file requests must use the same permission rules to prevent IDOR. Keeping authorization in the service avoids duplicating business rules in pages.
- **Alternatives considered**: Relying only on page authorization or hiding links would not protect direct URLs or service calls.

## Decision 6: Use explicit cleanup and transaction ordering for uploads

- **Decision**: Validate and authorize first, generate a unique relative path, save bytes, then persist metadata and audit data. If database persistence fails after file save, delete the saved file and report failure; if storage fails, do not insert metadata.
- **Rationale**: This follows the stakeholder-required sequence and avoids orphaned records and inaccessible database rows.
- **Alternatives considered**: Inserting metadata before storage creates records that point to files that may never exist; a database transaction alone cannot roll back filesystem writes.

## Decision 7: Add focused automated seams despite no current test project

- **Decision**: Keep services injectable and add focused tests for validation, permission predicates, upload cleanup, and query scoping when a test project is introduced; until then, use reproducible seeded-user scenarios and `dotnet build`.
- **Rationale**: The constitution requires proportionate verification, while the current repository has no test project. The design preserves seams so tests do not require UI automation for core security behavior.
- **Alternatives considered**: Testing only through the UI would be slower and would not reliably cover direct endpoint authorization or cleanup failures.
