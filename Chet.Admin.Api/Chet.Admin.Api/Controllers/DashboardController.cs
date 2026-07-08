using Chet.Admin.Contracts.Dashboard;
using Chet.Admin.DTOs.Dashboard;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("仪表盘数据")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _dashboardService.GetStatsAsync();
        return Ok(ApiResponse.Ok(stats, "Dashboard stats retrieved successfully"));
    }

    [HttpGet("trend")]
    public async Task<IActionResult> GetTrend([FromQuery] int days = 7)
    {
        var trend = await _dashboardService.GetTrendAsync(days);
        return Ok(ApiResponse.Ok(trend, "Dashboard trend retrieved successfully"));
    }

    [HttpGet("recent-logs")]
    public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 10)
    {
        var logs = await _dashboardService.GetRecentLogsAsync(count);
        return Ok(ApiResponse.Ok(logs, "Recent logs retrieved successfully"));
    }
}
