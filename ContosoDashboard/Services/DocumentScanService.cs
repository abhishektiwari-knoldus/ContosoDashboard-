using Microsoft.AspNetCore.Components.Forms;

namespace ContosoDashboard.Services;

public sealed record ScanResult(bool IsAccepted, string? Reason, string? DetectedType);

public interface IDocumentScanService
{
    Task<ScanResult> ScanAsync(Stream content, string fileName, string contentType, long length, CancellationToken cancellationToken = default);
}

public sealed class LocalDocumentScanService : IDocumentScanService
{
    private readonly long _maximumFileSize;
    private readonly HashSet<string> _allowedExtensions;
    private readonly Dictionary<string, string[]> _signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["%PDF"], [".jpg"] = ["\u00FF\u00D8\u00FF"], [".jpeg"] = ["\u00FF\u00D8\u00FF"], [".png"] = ["\u0089PNG"],
        [".doc"] = ["\u00D0\u00CF\u0011\u00E0"], [".xls"] = ["\u00D0\u00CF\u0011\u00E0"], [".ppt"] = ["\u00D0\u00CF\u0011\u00E0"],
        [".docx"] = ["PK"], [".xlsx"] = ["PK"], [".pptx"] = ["PK"]
    };

    public LocalDocumentScanService(IConfiguration configuration)
    {
        _maximumFileSize = configuration.GetValue("Documents:MaximumFileSizeBytes", 25 * 1024 * 1024);
        _allowedExtensions = configuration.GetSection("Documents:AllowedExtensions").Get<string[]>()?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png"];
    }

    public async Task<ScanResult> ScanAsync(Stream content, string fileName, string contentType, long length, CancellationToken cancellationToken = default)
    {
        if (length <= 0 || length > _maximumFileSize) return new(false, "Each file must be between 1 byte and 25 MB.", null);
        var extension = Path.GetExtension(fileName);
        if (!_allowedExtensions.Contains(extension)) return new(false, "This file type is not supported.", null);
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 255) return new(false, "The file type is invalid.", null);
        if (_signatures.TryGetValue(extension, out var signatures))
        {
            var buffer = new byte[Math.Min(16, length)];
            content.Position = 0;
            var read = await content.ReadAsync(buffer.AsMemory(), cancellationToken);
            content.Position = 0;
            var sample = System.Text.Encoding.Latin1.GetString(buffer, 0, read);
            if (!signatures.Any(sample.StartsWith)) return new(false, "The file content does not match its extension.", null);
        }
        return new(true, null, contentType);
    }
}