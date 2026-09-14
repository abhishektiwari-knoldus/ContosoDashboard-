# Quickstart Validation: Document Upload and Management

## Prerequisites

- .NET 10 SDK
- Repository checked out at the feature branch
- No cloud credentials or external storage service
- A clean local training database may be created automatically by the application

## Build validation

From the repository root:

```bash
dotnet build ContosoDashboard/ContosoDashboard.csproj
```

Expected result: the project builds successfully. Existing dependency or nullable warnings may remain, but document feature errors must be absent.

## Run the application

```bash
dotnet run --project ContosoDashboard/ContosoDashboard.csproj
```

Open the displayed local HTTP/HTTPS URL and sign in through the existing mock login page. Use the seeded roles: Administrator, Project Manager, Team Lead, and Employee.

## Core upload and access scenarios

1. Sign in as an Employee and upload a supported PDF or image smaller than 25 MB with a title and category.
   - Expected: progress and success are shown; the document appears in My Documents.
2. Upload a file over 25 MB and an unsupported extension.
   - Expected: each file is rejected with a clear reason; no document row or accessible file is created.
3. Upload multiple valid files.
   - Expected: every file has an independent result and generated storage name.
4. Sign in as another user and attempt to browse, search, preview, or download the first user's private document.
   - Expected: no result or file disclosure.
5. Associate a document with the seeded project and sign in as a project member.
   - Expected: the member can see and download the project document; an unrelated user cannot.
6. Share a document with a selected user.
   - Expected: the recipient sees it in Shared with Me and receives an in-app notification.
7. Edit metadata, replace the file, then delete with confirmation.
   - Expected: metadata/file changes are visible; after deletion, list, search, preview, download, and known direct requests no longer work.

## Integration scenarios

1. Upload or attach a document from a task.
   - Expected: the document is associated with the task's project and visible from the task and project views.
2. Open the dashboard after uploading several documents.
   - Expected: Recent Documents contains the five newest documents uploaded by the current user and the summary count is correct.
3. Add a project document as a project manager.
   - Expected: eligible project members receive an in-app notification.
4. Sign in as Administrator and open document reporting.
   - Expected: upload, download, deletion, and share activity is visible with actor and time summaries; non-administrators are denied.

## Security and failure checks

- Try a path traversal-like filename and duplicate original filename: generated storage names must remain unique and outside `wwwroot`.
- Interrupt or force a storage/database failure during upload: no accessible partial file or misleading database row may remain.
- Submit a file with a permitted extension but invalid content signature: the local scan gate must reject or quarantine it.
- Verify all direct download/preview requests resolve through the authorized document service and never accept a browser-supplied filesystem path.

## Performance checks

Using a seeded dataset of up to 500 documents:

- Document list and search complete within 2 seconds.
- A valid upload up to 25 MB completes within 30 seconds under typical local/network conditions.
- An accessible PDF or image preview loads within 3 seconds.

Record observed timings and the dataset size with the validation result. The local scan substitute is training-only and does not establish production malware protection.

## MVP implementation validation record

- Upload, validation, My Documents, search/filter/sort, project access, preview/download, metadata edit, replacement, deletion, sharing, task links, dashboard recent documents/count, notifications, and administrator report surfaces are implemented in the current training build.
- Verify direct unauthorized document IDs return no document data and that storage files remain outside `wwwroot`.
- Record the date, role used, document IDs, observed timings, and any rejected-file reasons when running the scenarios above.
