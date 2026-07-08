using Chet.Admin.DTOs.File;
using Microsoft.AspNetCore.Http;

namespace Chet.Admin.Contracts.File;

public interface IFileService
{
    Task<(List<FileDto> items, int total)> GetListAsync(int pageNumber, int pageSize, string? keyword);
    Task<FileDto> UploadAsync(IFormFile file, int? uploaderId);
    Task<FileDto?> GetByIdAsync(int id);
    Task<(byte[] Data, string ContentType, string FileName)?> DownloadAsync(int id);
    Task DeleteAsync(int id);
}
