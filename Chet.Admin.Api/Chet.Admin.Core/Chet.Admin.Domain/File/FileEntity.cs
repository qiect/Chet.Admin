namespace Chet.Admin.Domain.File;

/// <summary>
/// 文件实体
/// </summary>
public class FileEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 原始文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 存储文件名（重命名后的唯一文件名）
    /// </summary>
    public string StoredName { get; set; } = string.Empty;

    /// <summary>
    /// 文件存储路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件MIME类型
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 上传用户ID
    /// </summary>
    public int? UploaderId { get; set; }

    /// <summary>
    /// 上传时间（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
