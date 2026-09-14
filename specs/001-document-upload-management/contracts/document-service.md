# Contract: Document Service and Authorized File Endpoint

## DocumentService operations

All operations receive the authenticated requesting user ID and return only authorized data.

```text
GetMyDocumentsAsync(userId, query)
GetProjectDocumentsAsync(projectId, userId, query)
GetSharedDocumentsAsync(userId, query)
GetRecentDocumentsAsync(userId, count = 5)
SearchDocumentsAsync(userId, query)
UploadAsync(userId, metadata, file)
UpdateMetadataAsync(userId, documentId, metadata)
ReplaceFileAsync(userId, documentId, file)
DeleteAsync(userId, documentId)
ShareAsync(userId, documentId, recipients)
GetReportsAsync(adminUserId, reportQuery)
```

Contract rules:

- Unauthorized documents are represented as not found or an equivalent non-disclosing result; callers must not infer their existence.
- Employee access includes owned documents and explicitly shared/project-authorized documents as defined by the role and project membership rules.
- Team Lead, Project Manager, and Administrator permissions are checked in the service, not only in Razor markup.
- Upload follows validate/authorize/scan -> generate path -> save bytes -> save metadata and activity; failed persistence removes the saved bytes.
- Mutations create audit activity after the business operation succeeds.
- Search combines text/filter predicates with the access predicate before database execution.

## Authorized download and preview endpoint

`DocumentDownload` receives a document ID and a preview/download mode.

1. Require an authenticated request.
2. Ask `DocumentService` for an authorized document stream.
3. Return the stored MIME type and a safe content-disposition header.
4. Return not found/forbidden without revealing whether an inaccessible ID exists.
5. Record Download or Preview activity only after authorization succeeds.

The endpoint must never accept a filesystem path from the browser and must never serve a file by direct static URL.
