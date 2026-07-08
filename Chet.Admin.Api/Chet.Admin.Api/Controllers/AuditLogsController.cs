using Chet.Admin.Contracts.Audit;
using Chet.Admin.DTOs.Audit;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 操作日志控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("操作日志管理")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsController> _logger;

    /// <summary>
    /// 初始化操作日志控制器的新实例
    /// </summary>
    /// <param name="auditLogService">操作日志服务接口</param>
    /// <param name="logger">日志记录器</param>
    public AuditLogsController(IAuditLogService auditLogService, ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// 分页查询操作日志列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="userId">用户ID筛选</param>
    /// <param name="module">模块名称筛选</param>
    /// <param name="action">操作类型筛选</param>
    /// <param name="startTime">开始时间筛选</param>
    /// <param name="endTime">结束时间筛选</param>
    /// <returns>分页操作日志列表</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedAuditLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? module = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        var request = new AuditLogPagedRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Keyword = keyword,
            UserId = userId,
            Module = module,
            Action = action,
            StartTime = startTime,
            EndTime = endTime
        };
        var result = await _auditLogService.GetPagedAuditLogsAsync(request);
        return Ok(PaginatedResponse<AuditLogDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Audit logs retrieved successfully"));
    }

    /// <summary>
    /// 清理指定时间之前的操作日志
    /// </summary>
    /// <param name="before">清理截止时间</param>
    /// <returns>清理结果</returns>
    [HttpDelete("clear")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearAuditLogs([FromQuery] DateTime before)
    {
        await _auditLogService.ClearBeforeAsync(before);
        return Ok(ApiResponse.Ok(null, "Audit logs cleared successfully"));
    }
}
