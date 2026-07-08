using Chet.Admin.DTOs.Dashboard;

namespace Chet.Admin.Contracts.Dashboard;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
    Task<DashboardTrendDto> GetTrendAsync(int days = 7);
    Task<List<RecentLogItem>> GetRecentLogsAsync(int count = 10);
}
