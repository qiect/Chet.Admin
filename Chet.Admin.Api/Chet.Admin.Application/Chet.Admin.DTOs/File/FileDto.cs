namespace Chet.Admin.DTOs.File;

public class FileDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? UploaderId { get; set; }
    public DateTime CreatedAt { get; set; }
}
