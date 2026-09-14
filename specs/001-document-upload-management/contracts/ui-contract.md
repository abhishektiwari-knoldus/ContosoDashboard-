# Contract: Document UI States

## Documents page

The page exposes My Documents, Project Documents, and Shared with Me views using the same authorized query model. Each row displays title, category, upload date, file size, uploader, and project where applicable.

Required interactions:

- Upload modal with required title/category, optional description/project/tags, per-file progress, and per-file result.
- Search across title, description, tags, uploader, and project.
- Sort by title, upload date, category, and file size.
- Filter by category, project, and date range.
- Preview only PDF and image documents; offer download when preview is unavailable and access is allowed.
- Edit metadata, replace file, delete with confirmation, and share with user/team recipients when authorized.

Failure states must clearly indicate unsupported type, size limit, scan rejection, storage failure, authorization denial, missing document, and empty results without exposing protected metadata.

## Integration surfaces

- Project details shows authorized project documents and a project-manager upload action.
- Task details shows related documents and an upload/attach action that inherits the task's project.
- Dashboard shows five recent documents uploaded by the current user and the document count.
- Notifications shows share and project-document notifications using existing notification patterns.
- Administrator-only reporting is hidden or denied for non-administrators.
