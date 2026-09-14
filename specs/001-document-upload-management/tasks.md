---

description: "Executable task list for document upload and management"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `specs/001-document-upload-management/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`
**Tests**: No automated test project is currently present. Tasks include focused scenario/build validation required by the specification and constitution.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish configuration and source locations required by the feature.

- [X] T001 Add the document storage root, 25 MB upload limit, and allowed file type settings to `ContosoDashboard/appsettings.json` and `ContosoDashboard/appsettings.Development.json`.
- [X] T002 [P] Add the document feature navigation entry and route placeholder in `ContosoDashboard/Shared/NavMenu.razor` and `ContosoDashboard/Pages/Documents.razor`.
- [X] T003 [P] Add the configured application-data upload directory to `ContosoDashboard/.gitignore` without excluding document metadata source files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement shared persistence, storage, validation, and authorization seams before any user story work.

**Checkpoint**: Foundation is ready when the database can represent documents, local storage is outside `wwwroot`, all generated paths are safe and unique, and the application can resolve the shared services through dependency injection.

- [X] T004 Create the integer-keyed `Document` entity with required title, text category, optional description/tags/project/task links, original filename, generated relative file path, MIME type up to 255 characters, file size, UTC timestamps, uploader, and soft-delete state in `ContosoDashboard/Models/Document.cs`.
- [X] T005 [P] Create `DocumentShare` with mutually exclusive user/team recipient fields, grantor, timestamps, active state, and integer key in `ContosoDashboard/Models/DocumentShare.cs`.
- [X] T006 [P] Create append-only `DocumentActivity` and the `DocumentActivityAction` values for upload, download, preview, metadata edit, replacement, deletion, and sharing in `ContosoDashboard/Models/DocumentActivity.cs`.
- [X] T007 Extend `ContosoDashboard/Data/ApplicationDbContext.cs` with document, share, and activity DbSets, foreign-key relationships to users/projects/tasks, indexes for uploader/project/task/category/date/file type/deleted state, and duplicate-share constraints.
- [X] T008 Implement `IFileStorageService` and `LocalFileStorageService` in `ContosoDashboard/Services/FileStorageService.cs` using a configured root outside `wwwroot`, relative paths, GUID-generated filenames, traversal protection, non-overwriting writes, streamed reads, existence checks, and deletes.
- [X] T009 Implement `IDocumentScanService` and the offline validation/quarantine implementation in `ContosoDashboard/Services/DocumentScanService.cs` for size, extension allowlist, MIME consistency, and basic content-signature checks; document the production malware-scanner limitation in `README.md`.
- [X] T010 Register the document storage, scan, and document service dependencies and configure the document upload root in `ContosoDashboard/Program.cs`.
- [X] T011 Add shared document metadata validation constants and request/response records for title, description, category, tags, project, task, file, and per-file upload result in `ContosoDashboard/Services/DocumentService.cs` or a dedicated `ContosoDashboard/Services/DocumentContracts.cs`.
- [X] T012 Add service-level authorization helpers in `ContosoDashboard/Services/DocumentService.cs` for owner, project membership, Team Lead team scope, Project Manager project scope, Administrator access, active shares, and non-disclosing unauthorized results.

---

## Phase 3: User Story 1 - Upload and Organize a Document (Priority: P1) MVP

**Goal**: Authenticated employees can upload valid documents with required metadata, see progress/results, and find successful uploads in My Documents.

**Independent Test**: As an Employee, upload valid PDF/image and multiple files, then attempt an oversized file, unsupported type, invalid content signature, duplicate filename, and failed-storage scenario; verify per-file outcomes, metadata, unique paths, and no orphaned records/files.

### Implementation for User Story 1

- [X] T013 [US1] Implement authorized upload orchestration in `ContosoDashboard/Services/DocumentService.cs`: validate metadata and project/task access, scan before availability, generate the path, save bytes, persist metadata/activity after storage succeeds, and remove saved bytes when persistence fails.
- [X] T014 [US1] Implement My Documents query methods in `ContosoDashboard/Services/DocumentService.cs` with uploader scope, soft-delete exclusion, bounded ordering, and metadata projection.
- [X] T015 [US1] Build the upload form, required title/category fields, optional description/project/tags, multi-file selection, progress states, and per-file success/error results in `ContosoDashboard/Pages/Documents.razor`.
- [X] T016 [US1] Add supported category choices, 25 MB/file validation, allowlisted type messages, scan rejection messages, and storage/database failure handling to `ContosoDashboard/Pages/Documents.razor`.
- [X] T017 [US1] Add My Documents table rendering with title, category, upload date, file size, project, and uploader fields in `ContosoDashboard/Pages/Documents.razor`.
- [X] T018 [US1] Add deterministic seeded or manual validation data and document the US1 upload/access scenarios in `specs/001-document-upload-management/quickstart.md`.

**Checkpoint**: US1 is independently demonstrable when valid uploads appear in My Documents and every invalid/failure path leaves no accessible partial document.

---

## Phase 4: User Story 2 - Find, Preview, and Use Accessible Documents (Priority: P1)

**Goal**: Users can search, filter, sort, preview, and download only documents they are authorized to access.

**Independent Test**: Seed private, shared, and project documents across the existing users; verify Employee, Team Lead, Project Manager, Administrator, project-member, and unrelated-user results for list/search/preview/download.

- [X] T019 [US2] Implement combined access predicates and bounded search/filter/sort query handling for title, description, tags, uploader, project, category, date range, file size, and upload date in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T020 [US2] Implement project-document queries that require project manager or active project membership in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T021 [US2] Add authorized stream retrieval for preview/download and Download/Preview activity recording in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T022 [US2] Add the authorized Razor Page download/preview endpoint in `ContosoDashboard/Pages/DocumentDownload.cshtml` and `ContosoDashboard/Pages/DocumentDownload.cshtml.cs`, returning safe content disposition and non-disclosing not-found/denied responses without accepting filesystem paths.
- [X] T023 [US2] Complete Documents page search, sort, category/project/date filters, empty results, preview controls for PDFs/images, and download controls in `ContosoDashboard/Pages/Documents.razor`.
- [X] T024 [US2] Render authorized project documents and project-member download access in `ContosoDashboard/Pages/ProjectDetails.razor`.
- [X] T025 [US2] Add focused IDOR and role-scope validation scenarios for direct IDs, search results, project membership, preview, and download to `specs/001-document-upload-management/quickstart.md`.

**Checkpoint**: US2 is independently demonstrable when inaccessible documents never appear in list/search and direct preview/download requests cannot bypass authorization.

---

## Phase 5: User Story 3 - Maintain and Share Documents (Priority: P2)

**Goal**: Owners and permitted project managers can edit, replace, delete, and share documents with controlled notifications.

**Independent Test**: Exercise metadata edit, replacement, deletion, and user/team sharing as owner, Project Manager, Team Lead, recipient, and unrelated Employee; verify permissions, cleanup, Shared with Me, and notifications.

- [X] T026 [US3] Implement authorized metadata update with title, description, category, and tag validation plus MetadataEdit activity in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T027 [US3] Implement authorized replacement with scan-before-availability, new generated path, persistence-safe cleanup of the previous file, and Replace activity in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T028 [US3] Implement confirmed soft-delete plus physical file removal, Delete activity, and inaccessible deleted-document behavior in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T029 [US3] Implement user/team share creation, duplicate prevention, recipient/team authorization, active-share checks, Share activity, and notification creation in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T030 [US3] Extend `ContosoDashboard/Models/Notification.cs` with document share and project-document notification types while preserving existing notification values and database behavior.
- [X] T031 [US3] Implement Shared with Me queries and recipient/team access revocation behavior in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T032 [US3] Add edit, replace, delete-confirmation, and user/team share controls with clear failure states to `ContosoDashboard/Pages/Documents.razor`.
- [X] T033 [US3] Render document share notifications using existing notification patterns in `ContosoDashboard/Pages/Notifications.razor`.
- [X] T034 [US3] Add focused maintenance, cleanup, sharing, and notification scenarios to `specs/001-document-upload-management/quickstart.md`.

**Checkpoint**: US3 is independently demonstrable when permissions are enforced for every mutation, recipients see active shares, and deleted/replaced files cannot leave stale access.

---

## Phase 6: User Story 4 - Use Documents from Tasks and the Dashboard (Priority: P2)

**Goal**: Documents become part of task and dashboard workflows with inherited project association, recent documents, counts, and project notifications.

**Independent Test**: Upload from a task, inspect task/project links, open the dashboard after multiple uploads, and add a project document as a Project Manager; verify association, five-item recency, count, and eligible notifications.

- [X] T035 [US4] Add task association and task-project consistency checks to upload and query operations in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T036 [US4] Add related-document queries and attach/upload actions to `ContosoDashboard/Pages/Tasks.razor`, ensuring task uploads inherit the task's project.
- [X] T037 [US4] Add `GetRecentDocumentsAsync` and document count queries with current-user scope in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T038 [US4] Add the Recent Documents widget showing the five newest current-user uploads and a document count to `ContosoDashboard/Pages/Index.razor`.
- [X] T039 [US4] Notify eligible project members after a project document is persisted, using the existing notification service in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T040 [US4] Add task/project/dashboard integration scenarios and notification expectations to `specs/001-document-upload-management/quickstart.md`.

**Checkpoint**: US4 is independently demonstrable when task uploads inherit project context, dashboard summaries are accurate, and project members receive authorized notifications.

---

## Phase 7: User Story 5 - Review Document Activity (Priority: P3)

**Goal**: Administrators can review document activity and generate type, uploader, and access-pattern reports while non-administrators are denied.

**Independent Test**: Perform uploads, downloads, previews, edits, replacements, deletions, and shares, then compare administrator report totals and verify non-administrator denial.

- [X] T041 [US5] Implement administrator-only activity queries and report aggregation for document types, active uploaders, access patterns, and time ranges in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T042 [US5] Add administrator authorization and report navigation/page rendering in `ContosoDashboard/Pages/DocumentReports.razor`.
- [X] T043 [US5] Add report links and administrator-only visibility rules to `ContosoDashboard/Shared/NavMenu.razor`.
- [X] T044 [US5] Add activity/report validation scenarios and non-administrator denial checks to `specs/001-document-upload-management/quickstart.md`.

**Checkpoint**: US5 is independently demonstrable when administrators can reconcile report totals and non-administrators cannot access audit data.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Finish security, performance, documentation, and release validation across all stories.

- [X] T045 [P] Update `README.md` with document setup, local storage location, training-only malware validation, supported file types, and cleanup guidance.
- [ ] T046 [P] Add document storage, service registration, and authorization behavior comments/configuration descriptions to `ContosoDashboard/appsettings.json`, `ContosoDashboard/appsettings.Development.json`, and `ContosoDashboard/Program.cs` where needed for maintainability.
- [ ] T047 Add pagination/limited projections and verify the document indexes in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Data/ApplicationDbContext.cs` to meet the 500-document/2-second list and search targets.
- [ ] T048 Add safe content headers, filename sanitization, path traversal checks, and non-disclosing error logging review to `ContosoDashboard/Pages/DocumentDownload.cshtml.cs` and `ContosoDashboard/Services/FileStorageService.cs`.
- [ ] T049 Run `dotnet build ContosoDashboard/ContosoDashboard.csproj` and resolve any document-feature compile errors.
- [ ] T050 Execute all scenarios in `specs/001-document-upload-management/quickstart.md`, record observed timings and security outcomes, and update the validation notes.
- [ ] T051 Review all document list, mutation, notification, and report paths against `specs/001-document-upload-management/spec.md`, `data-model.md`, and the constitution before release.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies; configuration and navigation scaffolding can begin immediately.
- **Phase 2 Foundational**: Depends on Setup; blocks all user stories because models, storage, scanning, registration, and authorization are shared prerequisites.
- **Phase 3 US1**: Depends on Phase 2 and is the recommended MVP increment.
- **Phase 4 US2**: Depends on Phase 2 and the upload persistence contract from US1 for meaningful data; its query/security code can begin after Phase 2.
- **Phase 5 US3**: Depends on Phase 2 and the authorized document query/service contract from US1/US2.
- **Phase 6 US4**: Depends on US1 document persistence and existing task/dashboard/notification surfaces.
- **Phase 7 US5**: Depends on activity records from US1–US4.
- **Phase 8 Polish**: Depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: Foundational only; no other feature story is required for the MVP.
- **US2 (P1)**: Foundational plus US1's persisted document contract; can be developed in parallel with US1 after shared services are defined.
- **US3 (P2)**: Foundational plus US1/US2 authorization and document query surfaces.
- **US4 (P2)**: US1 upload service plus existing task/dashboard/notification pages; US3 sharing is not required.
- **US5 (P3)**: Activity records from each completed operation; final reporting follows the core workflows.

### Parallel Opportunities

- Setup T002 and T003 can run in parallel with T001 after the target paths are agreed.
- Foundational model tasks T005 and T006 can run in parallel; T008 and T009 can run in parallel before registration.
- After Phase 2, US1 upload UI/service work and US2 query/endpoint work can be split across developers once their shared contracts are stable.
- In US3, metadata, replacement, deletion, sharing, and notification model work can be developed in separate files before final Documents page integration.
- US4 task integration and dashboard integration can proceed in parallel after the document service exposes the required methods.
- US5 report service and report UI/navigation can proceed in parallel after the report contract is agreed.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 Setup.
2. Complete Phase 2 Foundational.
3. Complete Phase 3 US1.
4. Execute the US1 independent validation scenarios and `dotnet build`.
5. Demonstrate the offline upload and My Documents workflow before adding later stories.

### Incremental Delivery

1. Add US2 to make uploaded documents discoverable and safely downloadable.
2. Add US3 for controlled maintenance and sharing.
3. Add US4 for task/dashboard integration and project notifications.
4. Add US5 for administrator audit reporting.
5. Complete Phase 8 performance, security, documentation, and full quickstart validation.

### Notes

- Every task uses the required checkbox, sequential ID, optional `[P]` marker, story label for story phases, and an exact repository path.
- No automated test tasks were added because the feature specification did not explicitly request a TDD/test-first workflow; focused scenario validation remains mandatory.
