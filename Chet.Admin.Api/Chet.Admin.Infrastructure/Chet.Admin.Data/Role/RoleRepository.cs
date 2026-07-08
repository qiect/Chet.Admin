using Chet.Admin.Contracts.Role;
using Chet.Admin.Domain.Role;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data.Role;

/// <summary>
/// 角色仓储实现类
/// </summary>
public class RoleRepository : EfCoreRepository<RoleEntity>, IRoleRepository
{
    /// <summary>
    /// 初始化角色仓储实例
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public RoleRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 根据角色编码查询角色
    /// </summary>
    /// <param name="code">角色编码</param>
    /// <returns>角色实体，不存在返回null</returns>
    public async Task<RoleEntity?> GetByCodeAsync(string code)
    {
        return await _dbContext.Roles.FirstOrDefaultAsync(r => r.Code == code);
    }

    /// <summary>
    /// 根据用户ID查询其所有角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>角色实体列表</returns>
    public async Task<IEnumerable<RoleEntity>> GetRolesByUserIdAsync(int userId)
    {
        return await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role)
            .ToListAsync();
    }
}
