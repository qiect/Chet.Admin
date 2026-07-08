namespace Chet.Admin.Domain.File;

public class FileEntity
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Description { get; set; }
    public int? UploaderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
