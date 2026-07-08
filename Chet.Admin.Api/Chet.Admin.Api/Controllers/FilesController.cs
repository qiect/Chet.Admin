using System.Security.Claims;
using Chet.Admin.Contracts.File;
using Chet.Admin.DTOs.File;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 文件管理控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("文件管理")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;

    /// <summary>
    /// 初始化文件控制器的新实例
    /// </summary>
    /// <param name="fileService">文件服务接口</param>
    /// <param name="logger">日志记录器</param>
    public FilesController(IFileService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// 分页获取文件列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>分页文件列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var (items, total) = await _fileService.GetListAsync(pageNumber, pageSize, keyword);
        return Ok(ApiResponse.Ok(new { items, total }, "Files retrieved successfully"));
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <returns>文件上传结果信息</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse.Error("请选择文件"));

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int? userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;

        var result = await _fileService.UploadAsync(file, userId);
        return Ok(ApiResponse.Ok(result, "文件上传成功"));
    }

    /// <summary>
    /// 根据ID获取文件详情
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件详情</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var file = await _fileService.GetByIdAsync(id);
        if (file == null) return NotFound(ApiResponse.Error("文件不存在"));
        return Ok(ApiResponse.Ok(file, "File retrieved successfully"));
    }

    /// <summary>
    /// 下载指定文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件流</returns>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var result = await _fileService.DownloadAsync(id);
        if (result == null) return NotFound(ApiResponse.Error("文件不存在"));

        var (data, contentType, fileName) = result.Value;
        return File(data, contentType, fileName);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _fileService.DeleteAsync(id);
        return Ok(ApiResponse.Ok(null, "文件删除成功"));
    }
}
