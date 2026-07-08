namespace Chet.Admin.DTOs.File;

/// <summary>
/// 文件DTO
/// </summary>
public class FileDto
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件类型
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 上传者ID
    /// </summary>
    public int? UploaderId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
