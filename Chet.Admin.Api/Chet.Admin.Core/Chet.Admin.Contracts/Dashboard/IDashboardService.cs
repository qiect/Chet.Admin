using Chet.Admin.DTOs.Dashboard;

namespace Chet.Admin.Contracts.Dashboard;

/// <summary>
/// 仪表盘服务接口
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// 获取仪表盘统计数据
    /// </summary>
    /// <returns>仪表盘统计DTO</returns>
    Task<DashboardStatsDto> GetStatsAsync();

    /// <summary>
    /// 获取仪表盘趋势数据
    /// </summary>
    /// <param name="days">统计天数，默认7天</param>
    /// <returns>仪表盘趋势DTO</returns>
    Task<DashboardTrendDto> GetTrendAsync(int days = 7);

    /// <summary>
    /// 获取最近的操作日志
    /// </summary>
    /// <param name="count">返回记录数，默认10条</param>
    /// <returns>最近日志项列表</returns>
    Task<List<RecentLogItem>> GetRecentLogsAsync(int count = 10);
}
