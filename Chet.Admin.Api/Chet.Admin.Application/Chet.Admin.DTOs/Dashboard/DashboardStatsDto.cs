namespace Chet.Admin.DTOs.Dashboard;

/// <summary>
/// 仪表盘统计DTO
/// </summary>
public class DashboardStatsDto
{
    /// <summary>
    /// 用户总数
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// 角色总数
    /// </summary>
    public int RoleCount { get; set; }

    /// <summary>
    /// 菜单总数
    /// </summary>
    public int MenuCount { get; set; }

    /// <summary>
    /// 部门总数
    /// </summary>
    public int DepartmentCount { get; set; }

    /// <summary>
    /// 今日登录数
    /// </summary>
    public int TodayLoginCount { get; set; }

    /// <summary>
    /// 活跃用户数
    /// </summary>
    public int ActiveUserCount { get; set; }
}

/// <summary>
/// 仪表盘趋势DTO
/// </summary>
public class DashboardTrendDto
{
    /// <summary>
    /// 趋势项列表
    /// </summary>
    public List<TrendItem> Items { get; set; } = new();
}

/// <summary>
/// 趋势项
/// </summary>
public class TrendItem
{
    /// <summary>
    /// 日期
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// 注册数
    /// </summary>
    public int RegisterCount { get; set; }

    /// <summary>
    /// 登录数
    /// </summary>
    public int LoginCount { get; set; }
}

/// <summary>
/// 最近日志项
/// </summary>
public class RecentLogItem
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 操作
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 模块
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperatedAt { get; set; }
}
