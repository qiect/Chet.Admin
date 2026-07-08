using Chet.Admin.Contracts.Audit;
using Chet.Admin.DTOs.Audit;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("操作日志管理")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(IAuditLogService auditLogService, ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

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

    [HttpDelete("clear")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearAuditLogs([FromQuery] DateTime before)
    {
        await _auditLogService.ClearBeforeAsync(before);
        return Ok(ApiResponse.Ok(null, "Audit logs cleared successfully"));
    }
}
