using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using ContosoDashboard.Services;

namespace ContosoDashboard.Pages;

[Authorize]
public class DocumentDownloadModel : PageModel
{
    private readonly IDocumentService _documents;
    public DocumentDownloadModel(IDocumentService documents) => _documents = documents;

    public async Task<IActionResult> OnGetAsync(int id, bool preview = false)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId)) return NotFound();
        var result = await _documents.OpenAsync(userId, id, preview);
        if (result == null) return NotFound();
        var document = result.Value.Document;
        var disposition = preview && (document.FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || document.FileType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)) ? "inline" : "attachment";
        Response.Headers.ContentDisposition = $"{disposition}; filename*=UTF-8''{Uri.EscapeDataString(document.OriginalFileName)}";
        return File(result.Value.Stream, document.FileType);
    }
}