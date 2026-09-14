using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentActivity
{
    [Key] public int DocumentActivityId { get; set; }
    public int DocumentId { get; set; }
    public int UserId { get; set; }
    [Required, MaxLength(40)] public string Action { get; set; } = string.Empty;
    public DateTime OccurredDate { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string? Details { get; set; }
    [ForeignKey(nameof(DocumentId))] public virtual Document Document { get; set; } = null!;
    [ForeignKey(nameof(UserId))] public virtual User User { get; set; } = null!;
}

public static class DocumentActivityAction
{
    public const string Upload = "Upload";
    public const string Download = "Download";
    public const string Preview = "Preview";
    public const string MetadataEdit = "MetadataEdit";
    public const string Replace = "Replace";
    public const string Delete = "Delete";
    public const string Share = "Share";
}