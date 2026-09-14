# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-09-14 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-document-upload-management/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add secure, searchable document storage to the existing offline Blazor Server dashboard. The
implementation will add integer-keyed EF Core entities, an authorized document service, a local
filesystem storage adapter behind `IFileStorageService`, upload validation with a local
quarantine/validation gate, and UI integration for document browsing, projects, tasks, dashboard,
notifications, sharing, and administrator reporting.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# on .NET 10.0, nullable enabled  
**Primary Dependencies**: ASP.NET Core Blazor Server, EF Core 10 SQLite, existing cookie mock authentication  
**Storage**: SQLite metadata plus local filesystem under an application-data directory outside `wwwroot`  
**Testing**: No test project currently exists; focused service/scenario checks plus `dotnet build` are required, with automated tests added where practical  
**Target Platform**: Offline local web application on the existing ContosoDashboard host
**Project Type**: Single web application  
**Performance Goals**: List/search within 2 seconds for up to 500 documents; upload up to 25 MB within 30 seconds; preview within 3 seconds  
**Constraints**: 25 MB per file, allowlisted formats, integer document IDs, text categories, no cloud dependency, protected non-public files, service-level authorization  
**Scale/Scope**: Existing training users, projects, tasks, notifications, and up to 500 documents per list view in the initial release

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

*GATE: PASS.* The design remains offline-first, keeps business logic in services, protects all
document operations at service and request boundaries, and defines focused verification for file
validation, storage failure, IDOR protection, and role permissions. The local malware gate is an
explicit training substitute and is documented as insufficient for production deployment.

**Post-design re-check: PASS.** `research.md`, `data-model.md`, the storage/document/UI contracts,
and `quickstart.md` preserve the constitution's training scope, layered boundaries, security
requirements, simplicity expectations, and reproducible verification workflow. No unjustified
complexity or governance exception was introduced.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/ApplicationDbContext.cs             # Document, share, activity relationships and indexes
├── Models/Document.cs                        # Document metadata and category values
├── Models/DocumentShare.cs                   # User/team sharing permissions
├── Models/DocumentActivity.cs                # Audit events
├── Services/DocumentService.cs               # Authorization, validation, orchestration, queries
├── Services/FileStorageService.cs            # IFileStorageService and local implementation
├── Services/DocumentScanService.cs           # Offline validation/quarantine gate
├── Pages/Documents.razor                     # Browse, search, filter, upload, edit, share
├── Pages/DocumentDownload.cshtml(.cs)        # Authorized stream/preview endpoint
├── Pages/ProjectDetails.razor                 # Project document integration
├── Pages/Tasks.razor                          # Task document integration
├── Pages/Index.razor                          # Recent documents and count
└── Pages/Notifications.razor                 # Document notification rendering

specs/001-document-upload-management/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
```

**Structure Decision**: Extend the existing single-project Blazor Server application. Keep
document business rules in `Services`, persistence in the existing EF Core context and `Models`,
and expose file bytes only through an authorized Razor endpoint. No new project or external API is
needed for the offline training release.

## Complexity Tracking

No constitution violations. The storage interface and scan boundary are required by the feature's
offline/cloud-migration and security requirements, and are kept to the smallest useful abstractions.
