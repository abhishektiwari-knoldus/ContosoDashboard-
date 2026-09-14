using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<List<Document>> GetMyDocumentsAsync(int userId, DocumentSearchRequest? request = null);
    Task<List<Document>> GetProjectDocumentsAsync(int projectId, int userId, DocumentSearchRequest? request = null);
    Task<List<Document>> GetSharedDocumentsAsync(int userId, DocumentSearchRequest? request = null);
    Task<List<Document>> GetTaskDocumentsAsync(int taskId, int userId);
    Task<List<Document>> GetRecentDocumentsAsync(int userId, int count = 5);
    Task<int> GetDocumentCountAsync(int userId);
    Task<DocumentUploadResult> UploadAsync(int userId, DocumentMetadata metadata, Stream content, string fileName, string contentType, long length);
    Task<bool> UpdateMetadataAsync(int userId, int documentId, DocumentMetadata metadata);
    Task<bool> ReplaceFileAsync(int userId, int documentId, Stream content, string fileName, string contentType, long length);
    Task<bool> DeleteAsync(int userId, int documentId);
    Task<bool> ShareAsync(int userId, int documentId, int? recipientUserId, string? teamId);
    Task<(Document Document, Stream Stream)?> OpenAsync(int userId, int documentId, bool preview);
    Task<DocumentReport?> GetReportAsync(int adminUserId, DateTime? fromDate = null, DateTime? toDate = null);
}

public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IDocumentScanService _scanner;
    private readonly INotificationService _notifications;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(ApplicationDbContext context, IFileStorageService storage, IDocumentScanService scanner, INotificationService notifications, ILogger<DocumentService> logger)
    {
        _context = context; _storage = storage; _scanner = scanner; _notifications = notifications; _logger = logger;
    }

    public Task<List<Document>> GetMyDocumentsAsync(int userId, DocumentSearchRequest? request = null) => QueryAccessible(userId, request, ownedOnly: true).ToListAsync();

    public async Task<List<Document>> GetProjectDocumentsAsync(int projectId, int userId, DocumentSearchRequest? request = null)
    {
        if (!await CanAccessProjectAsync(projectId, userId)) return [];
        return await ApplyQuery(_context.Documents.Where(d => !d.IsDeleted && d.ProjectId == projectId), request).ToListAsync();
    }

    public Task<List<Document>> GetSharedDocumentsAsync(int userId, DocumentSearchRequest? request = null) =>
        ApplyQuery(_context.Documents.Where(d => !d.IsDeleted && d.Shares.Any(s => s.IsActive && (s.UserId == userId || s.TeamId == _context.Users.Where(u => u.UserId == userId).Select(u => u.Department).FirstOrDefault()))), request).ToListAsync();

    public async Task<List<Document>> GetTaskDocumentsAsync(int taskId, int userId)
    {
        var task = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.TaskId == taskId);
        if (task == null || !await CanUploadToContextAsync(userId, task.ProjectId, taskId)) return [];
        return await QueryAccessible(userId, null).Where(d => d.TaskId == taskId).ToListAsync();
    }

    public Task<List<Document>> GetRecentDocumentsAsync(int userId, int count = 5) =>
        _context.Documents.Where(d => !d.IsDeleted && d.UploadedByUserId == userId).Include(d => d.Project).OrderByDescending(d => d.UploadedDate).Take(Math.Clamp(count, 1, 25)).ToListAsync();

    public Task<int> GetDocumentCountAsync(int userId) => QueryAccessible(userId, null).CountAsync();

    public async Task<DocumentUploadResult> UploadAsync(int userId, DocumentMetadata metadata, Stream content, string fileName, string contentType, long length)
    {
        var validation = ValidateMetadata(metadata);
        if (validation != null) return new(false, validation, null);
        if (!await CanUploadToContextAsync(userId, metadata.ProjectId, metadata.TaskId)) return new(false, "You are not authorized to upload to this project or task.", null);
        var scan = await _scanner.ScanAsync(content, fileName, contentType, length);
        if (!scan.IsAccepted) return new(false, scan.Reason, null);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var relativePath = $"{userId}/{metadata.ProjectId?.ToString() ?? "personal"}/{Guid.NewGuid():N}{extension}";
        try
        {
            await _storage.UploadAsync(content, relativePath, contentType);
            var document = new Document
            {
                Title = metadata.Title.Trim(), Description = metadata.Description?.Trim(), Category = metadata.Category,
                Tags = NormalizeTags(metadata.Tags), OriginalFileName = Path.GetFileName(fileName), FilePath = relativePath,
                FileType = contentType, FileSize = length, UploadedByUserId = userId, ProjectId = metadata.ProjectId,
                TaskId = metadata.TaskId, UploadedDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow
            };
            _context.Documents.Add(document);
            _context.DocumentActivities.Add(new DocumentActivity { Document = document, UserId = userId, Action = DocumentActivityAction.Upload, Details = document.OriginalFileName });
            await _context.SaveChangesAsync();
            await NotifyProjectMembersAsync(document, userId);
            return new(true, null, document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document upload failed for user {UserId}", userId);
            await _storage.DeleteAsync(relativePath);
            return new(false, "The document could not be saved. No partial document was retained.", null);
        }
    }

    public async Task<bool> UpdateMetadataAsync(int userId, int documentId, DocumentMetadata metadata)
    {
        var error = ValidateMetadata(metadata);
        var document = await FindAuthorizedAsync(documentId, userId, mutation: true);
        if (error != null || document == null) return false;
        document.Title = metadata.Title.Trim(); document.Description = metadata.Description?.Trim(); document.Category = metadata.Category; document.Tags = NormalizeTags(metadata.Tags); document.UpdatedDate = DateTime.UtcNow;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = DocumentActivityAction.MetadataEdit });
        await _context.SaveChangesAsync(); return true;
    }

    public async Task<bool> ReplaceFileAsync(int userId, int documentId, Stream content, string fileName, string contentType, long length)
    {
        var document = await FindAuthorizedAsync(documentId, userId, mutation: true);
        if (document == null) return false;
        var scan = await _scanner.ScanAsync(content, fileName, contentType, length);
        if (!scan.IsAccepted) return false;
        var newPath = $"{document.UploadedByUserId}/{document.ProjectId?.ToString() ?? "personal"}/{Guid.NewGuid():N}{Path.GetExtension(fileName).ToLowerInvariant()}";
        await _storage.UploadAsync(content, newPath, contentType);
        var oldPath = document.FilePath;
        document.FilePath = newPath; document.FileType = contentType; document.FileSize = length; document.OriginalFileName = Path.GetFileName(fileName); document.UpdatedDate = DateTime.UtcNow;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = DocumentActivityAction.Replace });
        try { await _context.SaveChangesAsync(); await _storage.DeleteAsync(oldPath); return true; }
        catch { await _storage.DeleteAsync(newPath); return false; }
    }

    public async Task<bool> DeleteAsync(int userId, int documentId)
    {
        var document = await FindAuthorizedAsync(documentId, userId, mutation: true);
        if (document == null) return false;
        document.IsDeleted = true; document.UpdatedDate = DateTime.UtcNow;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = DocumentActivityAction.Delete });
        await _context.SaveChangesAsync();
        await _storage.DeleteAsync(document.FilePath);
        return true;
    }

    public async Task<bool> ShareAsync(int userId, int documentId, int? recipientUserId, string? teamId)
    {
        if ((recipientUserId.HasValue ? 1 : 0) + (!string.IsNullOrWhiteSpace(teamId) ? 1 : 0) != 1) return false;
        var document = await FindAuthorizedAsync(documentId, userId, mutation: true);
        if (document == null) return false;
        if (recipientUserId == userId) return false;
        if (recipientUserId.HasValue && !await _context.Users.AnyAsync(u => u.UserId == recipientUserId)) return false;
        var duplicate = await _context.DocumentShares.AnyAsync(s => s.DocumentId == documentId && s.IsActive && s.UserId == recipientUserId && s.TeamId == teamId);
        if (duplicate) return false;
        _context.DocumentShares.Add(new DocumentShare { DocumentId = documentId, UserId = recipientUserId, TeamId = teamId, SharedByUserId = userId });
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = DocumentActivityAction.Share, Details = recipientUserId?.ToString() ?? teamId });
        await _context.SaveChangesAsync();
        if (recipientUserId.HasValue) await _notifications.CreateNotificationAsync(new Notification { UserId = recipientUserId.Value, Title = "Document shared with you", Message = $"{document.Title} was shared with you.", Type = NotificationType.DocumentShared, Priority = NotificationPriority.Informational });
        return true;
    }

    public async Task<(Document Document, Stream Stream)?> OpenAsync(int userId, int documentId, bool preview)
    {
        var document = await FindAuthorizedAsync(documentId, userId);
        if (document == null) return null;
        var stream = await _storage.DownloadAsync(document.FilePath);
        if (stream == null) return null;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = preview ? DocumentActivityAction.Preview : DocumentActivityAction.Download });
        await _context.SaveChangesAsync();
        return (document, stream);
    }

    public async Task<DocumentReport?> GetReportAsync(int adminUserId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        if (!await IsAdministratorAsync(adminUserId)) return null;
        var activities = _context.DocumentActivities.AsQueryable();
        if (fromDate.HasValue) activities = activities.Where(a => a.OccurredDate >= fromDate.Value);
        if (toDate.HasValue) activities = activities.Where(a => a.OccurredDate <= toDate.Value);
        var rows = await activities.Include(a => a.Document).Include(a => a.User).ToListAsync();
        return new DocumentReport(rows.Select(a => a.DocumentId).Distinct().Count(), rows.GroupBy(a => a.Document.FileType).ToDictionary(g => g.Key, g => g.Count()), rows.GroupBy(a => a.User.DisplayName).ToDictionary(g => g.Key, g => g.Count()), rows.GroupBy(a => a.Action).ToDictionary(g => g.Key, g => g.Count()));
    }

    private IQueryable<Document> QueryAccessible(int userId, DocumentSearchRequest? request, bool ownedOnly = false)
    {
        var query = _context.Documents.Where(d => !d.IsDeleted);
        if (ownedOnly) query = query.Where(d => d.UploadedByUserId == userId);
        else query = query.Where(d => d.UploadedByUserId == userId || d.ProjectId.HasValue && (d.Project!.ProjectManagerId == userId || d.Project.ProjectMembers.Any(pm => pm.UserId == userId)) || d.Shares.Any(s => s.IsActive && (s.UserId == userId || s.TeamId == _context.Users.Where(u => u.UserId == userId).Select(u => u.Department).FirstOrDefault())) || _context.Users.Any(u => u.UserId == userId && u.Role == UserRole.Administrator));
        return ApplyQuery(query.Include(d => d.Project).Include(d => d.UploadedByUser), request);
    }

    private static IQueryable<Document> ApplyQuery(IQueryable<Document> query, DocumentSearchRequest? request)
    {
        if (request == null) return query.OrderByDescending(d => d.UploadedDate).Take(100);
        if (!string.IsNullOrWhiteSpace(request.SearchText)) { var text = request.SearchText.Trim(); query = query.Where(d => d.Title.Contains(text) || (d.Description != null && d.Description.Contains(text)) || (d.Tags != null && d.Tags.Contains(text)) || d.UploadedByUser.DisplayName.Contains(text) || (d.Project != null && d.Project.Name.Contains(text))); }
        if (!string.IsNullOrWhiteSpace(request.Category)) query = query.Where(d => d.Category == request.Category);
        if (request.ProjectId.HasValue) query = query.Where(d => d.ProjectId == request.ProjectId);
        if (request.FromDate.HasValue) query = query.Where(d => d.UploadedDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(d => d.UploadedDate <= request.ToDate.Value.AddDays(1));
        query = request.SortBy.ToLowerInvariant() switch { "title" => request.Descending ? query.OrderByDescending(d => d.Title) : query.OrderBy(d => d.Title), "category" => request.Descending ? query.OrderByDescending(d => d.Category) : query.OrderBy(d => d.Category), "size" => request.Descending ? query.OrderByDescending(d => d.FileSize) : query.OrderBy(d => d.FileSize), _ => request.Descending ? query.OrderByDescending(d => d.UploadedDate) : query.OrderBy(d => d.UploadedDate) };
        return query.Skip(Math.Max(0, request.Page - 1) * Math.Clamp(request.PageSize, 1, 100)).Take(Math.Clamp(request.PageSize, 1, 100));
    }

    private async Task<Document?> FindAuthorizedAsync(int documentId, int userId, bool mutation = false)
    {
        var document = await QueryAccessible(userId, null).FirstOrDefaultAsync(d => d.DocumentId == documentId);
        if (document == null) return null;
        if (!mutation) return document;
        return await CanManageAsync(document, userId) ? document : null;
    }

    private async Task<bool> CanManageAsync(Document document, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.Role == UserRole.Administrator || document.UploadedByUserId == userId) return true;
        return document.ProjectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId);
    }

    private async Task<bool> CanAccessProjectAsync(int projectId, int userId) => await _context.Projects.AnyAsync(p => p.ProjectId == projectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId) || _context.Users.Any(u => u.UserId == userId && u.Role == UserRole.Administrator)));
    private async Task<bool> CanUploadToContextAsync(int userId, int? projectId, int? taskId)
    {
        if (taskId.HasValue) { var task = await _context.Tasks.FindAsync(taskId); return task != null && task.ProjectId == projectId && await CanAccessProjectAsync(projectId ?? 0, userId); }
        return !projectId.HasValue || await CanAccessProjectAsync(projectId.Value, userId);
    }
    private async Task<bool> IsAdministratorAsync(int userId) => await _context.Users.AnyAsync(u => u.UserId == userId && u.Role == UserRole.Administrator);
    private async Task NotifyProjectMembersAsync(Document document, int uploaderId)
    {
        if (!document.ProjectId.HasValue) return;
        var recipients = await _context.ProjectMembers.Where(pm => pm.ProjectId == document.ProjectId && pm.UserId != uploaderId).Select(pm => pm.UserId).ToListAsync();
        foreach (var recipient in recipients) await _notifications.CreateNotificationAsync(new Notification { UserId = recipient, Title = "New project document", Message = $"A new document was added to {document.Project?.Name ?? "your project"}.", Type = NotificationType.ProjectDocumentAdded, Priority = NotificationPriority.Informational });
    }
    private static string? ValidateMetadata(DocumentMetadata metadata) => string.IsNullOrWhiteSpace(metadata.Title) || metadata.Title.Length > 255 ? "A document title is required and must be 255 characters or fewer." : !DocumentRules.Categories.Contains(metadata.Category) ? "Select a valid document category." : metadata.Description?.Length > 2000 ? "The description is too long." : metadata.Tags?.Length > 1000 ? "The tags are too long." : null;
    private static string? NormalizeTags(string? tags) => string.IsNullOrWhiteSpace(tags) ? null : string.Join(", ", tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase));
}