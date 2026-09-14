namespace ContosoDashboard.Services;

public static class DocumentRules
{
    public const long MaximumFileSize = 25 * 1024 * 1024;
    public static readonly string[] Categories = ["Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other"];
}

public sealed record DocumentMetadata(string Title, string? Description, string Category, string? Tags, int? ProjectId, int? TaskId);
public sealed record DocumentSearchRequest(string? SearchText = null, string? Category = null, int? ProjectId = null, DateTime? FromDate = null, DateTime? ToDate = null, string SortBy = "date", bool Descending = true, int Page = 1, int PageSize = 50);
public sealed record DocumentUploadResult(bool Succeeded, string? Error, Models.Document? Document);
public sealed record DocumentReport(int TotalDocuments, IReadOnlyDictionary<string, int> ByFileType, IReadOnlyDictionary<string, int> ByUploader, IReadOnlyDictionary<string, int> ByAction);