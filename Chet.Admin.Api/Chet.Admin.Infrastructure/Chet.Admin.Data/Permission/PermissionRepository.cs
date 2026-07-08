using Chet.Admin.Contracts.Permission;
using Chet.Admin.Domain.Permission;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data.Permission;

/// <summary>
/// 权限仓储实现类
/// </summary>
public class PermissionRepository : EfCoreRepository<PermissionEntity>, IPermissionRepository
{
    /// <summary>
    /// 初始化权限仓储实例
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public PermissionRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 根据权限编码查询权限
    /// </summary>
    /// <param name="code">权限编码</param>
    /// <returns>权限实体，不存在返回null</returns>
    public async Task<PermissionEntity?> GetByCodeAsync(string code)
    {
        return await _dbContext.Permissions.FirstOrDefaultAsync(p => p.Code == code);
    }

    /// <summary>
    /// 根据角色ID查询其所有权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>权限实体列表</returns>
    public async Task<IEnumerable<PermissionEntity>> GetPermissionsByRoleIdAsync(int roleId)
    {
        return await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission)
            .ToListAsync();
    }

    /// <summary>
    /// 根据用户ID查询其所有权限（通过角色关联）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>权限实体列表（已去重）</returns>
    public async Task<IEnumerable<PermissionEntity>> GetPermissionsByUserIdAsync(int userId)
    {
        return await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_dbContext.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission)
            .Distinct()
            .ToListAsync();
    }
}
