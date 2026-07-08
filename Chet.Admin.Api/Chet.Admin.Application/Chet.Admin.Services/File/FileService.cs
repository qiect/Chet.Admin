using Chet.Admin.Contracts.File;
using Chet.Admin.Data;
using Chet.Admin.Domain.File;
using Chet.Admin.DTOs.File;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.File;

/// <summary>
/// 文件服务实现
/// </summary>
public class FileService : IFileService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<FileService> _logger;
    private readonly string _uploadDir;

    private static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".zip", ".rar" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public FileService(AppDbContext dbContext, ILogger<FileService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(_uploadDir))
        {
            Directory.CreateDirectory(_uploadDir);
        }
    }

    /// <summary>
    /// 分页查询文件列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>文件列表和总条数</returns>
    public async Task<(List<FileDto> items, int total)> GetListAsync(int pageNumber, int pageSize, string? keyword)
    {
        var query = _dbContext.Files.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(f => f.FileName.Contains(keyword));
        }

        var total = await query.CountAsync();

        var entities = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entities.Select(entity => new FileDto
        {
            Id = entity.Id,
            FileName = entity.FileName,
            FilePath = entity.FilePath,
            ContentType = entity.ContentType,
            FileSize = entity.FileSize,
            UploaderId = entity.UploaderId,
            CreatedAt = entity.CreatedAt,
        }).ToList();

        return (items, total);
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <param name="uploaderId">上传者ID，可为空</param>
    /// <returns>文件数据传输对象</returns>
    public async Task<FileDto> UploadAsync(IFormFile file, int? uploaderId)
    {
        if (file.Length > MaxFileSize)
            throw new BadRequestException("文件大小不能超过10MB");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new BadRequestException("不支持的文件类型");

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(_uploadDir, storedName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var entity = new FileEntity
        {
            FileName = file.FileName,
            StoredName = storedName,
            FilePath = $"uploads/{storedName}",
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploaderId = uploaderId,
        };

        _dbContext.Files.Add(entity);
        await _dbContext.SaveChangesAsync();

        return new FileDto
        {
            Id = entity.Id,
            FileName = entity.FileName,
            FilePath = entity.FilePath,
            ContentType = entity.ContentType,
            FileSize = entity.FileSize,
            UploaderId = entity.UploaderId,
            CreatedAt = entity.CreatedAt,
        };
    }

    /// <summary>
    /// 根据ID获取文件信息
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件数据传输对象，不存在时返回null</returns>
    public async Task<FileDto?> GetByIdAsync(int id)
    {
        var entity = await _dbContext.Files.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
        if (entity == null) return null;
        return new FileDto
        {
            Id = entity.Id,
            FileName = entity.FileName,
            FilePath = entity.FilePath,
            ContentType = entity.ContentType,
            FileSize = entity.FileSize,
            UploaderId = entity.UploaderId,
            CreatedAt = entity.CreatedAt,
        };
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>包含文件数据、内容类型和文件名的元组，不存在时返回null</returns>
    public async Task<(byte[] Data, string ContentType, string FileName)?> DownloadAsync(int id)
    {
        var entity = await _dbContext.Files.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
        if (entity == null) return null;

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), entity.FilePath);
        if (!System.IO.File.Exists(fullPath)) return null;

        var data = await System.IO.File.ReadAllBytesAsync(fullPath);
        return (data, entity.ContentType, entity.FileName);
    }

    /// <summary>
    /// 删除文件（同时删除物理文件和数据库记录）
    /// </summary>
    /// <param name="id">文件ID</param>
    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id);
        if (entity == null) throw new NotFoundException(nameof(FileEntity), id);

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), entity.FilePath);
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }

        _dbContext.Files.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }
}
