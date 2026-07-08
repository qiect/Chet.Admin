using Chet.Admin.Contracts.Role;
using Chet.Admin.Data;
using Chet.Admin.Domain.Department;
using Chet.Admin.Domain.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Role;

/// <summary>
/// 数据权限服务实现
/// </summary>
public class DataScopeService : IDataScopeService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DataScopeService> _logger;

    public DataScopeService(AppDbContext dbContext, ILogger<DataScopeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户可访问的部门ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>可访问的部门ID列表</returns>
    public async Task<List<int>> GetAccessibleDeptIdsAsync(int userId)
    {
        // 获取用户角色
        var roleIds = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (roleIds.Count == 0) return new List<int>();

        // 获取角色及其数据权限范围
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => new { r.Id, r.DataScope })
            .ToListAsync();

        // 如果任一角色拥有 All 范围，返回所有部门
        if (roles.Any(r => r.DataScope == "All"))
            return await _dbContext.Departments.AsNoTracking().Select(d => d.Id).ToListAsync();

        var result = new HashSet<int>();

        // 获取用户所属部门
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return new List<int>();

        foreach (var role in roles)
        {
            switch (role.DataScope)
            {
                case "Dept":
                    if (user.DepartmentId.HasValue)
                        result.Add(user.DepartmentId.Value);
                    break;

                case "DeptAndChild":
                    if (user.DepartmentId.HasValue)
                    {
                        result.Add(user.DepartmentId.Value);
                        await AddChildDeptsAsync(user.DepartmentId.Value, result);
                    }
                    break;

                case "Self":
                    // Self 范围不添加任何部门
                    break;

                case "Custom":
                    var customDepts = await _dbContext.RoleDataScopeDepts
                        .AsNoTracking()
                        .Where(rd => rd.RoleId == role.Id)
                        .Select(rd => rd.DepartmentId)
                        .ToListAsync();
                    foreach (var d in customDepts) result.Add(d);
                    break;
            }
        }

        return result.ToList();
    }

    /// <summary>
    /// 获取用户的数据权限范围
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>数据权限范围字符串（All、DeptAndChild、Dept、Custom、Self）</returns>
    public async Task<string> GetDataScopeAsync(int userId)
    {
        var roleIds = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (roleIds.Count == 0) return "Self";

        var scopes = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.DataScope)
            .ToListAsync();

        // 返回最宽松的范围
        if (scopes.Contains("All")) return "All";
        if (scopes.Contains("DeptAndChild")) return "DeptAndChild";
        if (scopes.Contains("Dept")) return "Dept";
        if (scopes.Contains("Custom")) return "Custom";
        return "Self";
    }

    /// <summary>
    /// 递归获取指定部门的所有子部门ID
    /// </summary>
    /// <param name="parentId">父部门ID</param>
    /// <param name="result">用于收集子部门ID的集合</param>
    private async Task AddChildDeptsAsync(int parentId, HashSet<int> result)
    {
        var children = await _dbContext.Departments
            .AsNoTracking()
            .Where(d => d.ParentId == parentId)
            .Select(d => d.Id)
            .ToListAsync();

        foreach (var child in children)
        {
            if (result.Add(child))
            {
                await AddChildDeptsAsync(child, result);
            }
        }
    }
}
