using Chet.Admin.Contracts.Dashboard;
using Chet.Admin.Data;
using Chet.Admin.Domain.Audit;
using Chet.Admin.Domain.Menu;
using Chet.Admin.Domain.Role;
using Chet.Admin.Domain.User;
using Chet.Admin.Domain.Department;
using Chet.Admin.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Dashboard;

/// <summary>
/// 仪表盘服务实现
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(AppDbContext dbContext, ILogger<DashboardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 获取仪表盘统计数据
    /// </summary>
    /// <returns>包含用户、角色、菜单、部门数量及今日登录、活跃用户数的统计对象</returns>
    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var today = DateTime.UtcNow.Date;

        var userCount = await _dbContext.Users.CountAsync();
        var roleCount = await _dbContext.Roles.CountAsync();
        var menuCount = await _dbContext.Menus.CountAsync();
        var deptCount = await _dbContext.Departments.CountAsync();

        var todayLoginCount = await _dbContext.AuditLogs
            .Where(x => x.Action == "Login" && x.OperatedAt >= today)
            .CountAsync();

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var activeUserCount = await _dbContext.AuditLogs
            .Where(x => x.OperatedAt >= sevenDaysAgo)
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync();

        return new DashboardStatsDto
        {
            UserCount = userCount,
            RoleCount = roleCount,
            MenuCount = menuCount,
            DepartmentCount = deptCount,
            TodayLoginCount = todayLoginCount,
            ActiveUserCount = activeUserCount,
        };
    }

    /// <summary>
    /// 获取登录趋势数据
    /// </summary>
    /// <param name="days">统计天数，默认7天</param>
    /// <returns>登录趋势数据</returns>
    public async Task<DashboardTrendDto> GetTrendAsync(int days = 7)
    {
        var items = new List<TrendItem>();
        for (int i = days - 1; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            var nextDate = date.AddDays(1);

            var loginCount = await _dbContext.AuditLogs
                .Where(x => x.Action == "Login" && x.OperatedAt >= date && x.OperatedAt < nextDate)
                .CountAsync();

            items.Add(new TrendItem
            {
                Date = date.ToString("MM-dd"),
                RegisterCount = 0,
                LoginCount = loginCount,
            });
        }

        return new DashboardTrendDto { Items = items };
    }

    /// <summary>
    /// 获取最近的审计日志列表
    /// </summary>
    /// <param name="count">获取的记录数，默认10条</param>
    /// <returns>最近审计日志列表</returns>
    public async Task<List<RecentLogItem>> GetRecentLogsAsync(int count = 10)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.OperatedAt)
            .Take(count)
            .Select(x => new RecentLogItem
            {
                Id = x.Id,
                UserName = x.UserName,
                Action = x.Action,
                Module = x.Module,
                Description = x.Description,
                OperatedAt = x.OperatedAt,
            })
            .ToListAsync();
    }
}
