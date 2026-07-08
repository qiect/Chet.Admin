using Chet.Admin.DTOs.File;
using Microsoft.AspNetCore.Http;

namespace Chet.Admin.Contracts.File;

/// <summary>
/// 文件服务接口
/// </summary>
public interface IFileService
{
    /// <summary>
    /// 分页获取文件列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="keyword">搜索关键字</param>
    /// <returns>文件列表及总记录数</returns>
    Task<(List<FileDto> items, int total)> GetListAsync(int pageNumber, int pageSize, string? keyword);

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <param name="uploaderId">上传者ID</param>
    /// <returns>文件DTO</returns>
    Task<FileDto> UploadAsync(IFormFile file, int? uploaderId);

    /// <summary>
    /// 根据ID获取文件信息
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件DTO，不存在则返回null</returns>
    Task<FileDto?> GetByIdAsync(int id);

    /// <summary>
    /// 根据ID下载文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>包含文件数据、内容类型和文件名的元组，不存在则返回null</returns>
    Task<(byte[] Data, string ContentType, string FileName)?> DownloadAsync(int id);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="id">文件ID</param>
    Task DeleteAsync(int id);
}
