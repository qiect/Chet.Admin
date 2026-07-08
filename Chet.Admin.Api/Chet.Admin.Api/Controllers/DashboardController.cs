using Chet.Admin.Contracts.Dashboard;
using Chet.Admin.DTOs.Dashboard;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 仪表盘控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("仪表盘数据")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    /// <summary>
    /// 初始化仪表盘控制器的新实例
    /// </summary>
    /// <param name="dashboardService">仪表盘服务接口</param>
    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// 获取仪表盘统计数据
    /// </summary>
    /// <returns>统计数据</returns>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _dashboardService.GetStatsAsync();
        return Ok(ApiResponse.Ok(stats, "Dashboard stats retrieved successfully"));
    }

    /// <summary>
    /// 获取仪表盘趋势数据
    /// </summary>
    /// <param name="days">统计天数</param>
    /// <returns>趋势数据</returns>
    [HttpGet("trend")]
    public async Task<IActionResult> GetTrend([FromQuery] int days = 7)
    {
        var trend = await _dashboardService.GetTrendAsync(days);
        return Ok(ApiResponse.Ok(trend, "Dashboard trend retrieved successfully"));
    }

    /// <summary>
    /// 获取最近操作日志
    /// </summary>
    /// <param name="count">返回条数</param>
    /// <returns>最近操作日志列表</returns>
    [HttpGet("recent-logs")]
    public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 10)
    {
        var logs = await _dashboardService.GetRecentLogsAsync(count);
        return Ok(ApiResponse.Ok(logs, "Recent logs retrieved successfully"));
    }
}
