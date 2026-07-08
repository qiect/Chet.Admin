namespace Chet.Admin.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int UserCount { get; set; }
    public int RoleCount { get; set; }
    public int MenuCount { get; set; }
    public int DepartmentCount { get; set; }
    public int TodayLoginCount { get; set; }
    public int ActiveUserCount { get; set; }
}

public class DashboardTrendDto
{
    public List<TrendItem> Items { get; set; } = new();
}

public class TrendItem
{
    public string Date { get; set; } = string.Empty;
    public int RegisterCount { get; set; }
    public int LoginCount { get; set; }
}

public class RecentLogItem
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OperatedAt { get; set; }
}
