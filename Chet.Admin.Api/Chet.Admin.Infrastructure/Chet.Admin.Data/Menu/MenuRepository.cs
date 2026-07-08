using Chet.Admin.Contracts.Menu;
using Chet.Admin.Domain.Menu;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data.Menu;

/// <summary>
/// 菜单仓储实现类
/// </summary>
public class MenuRepository : EfCoreRepository<MenuEntity>, IMenuRepository
{
    /// <summary>
    /// 初始化菜单仓储实例
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public MenuRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 根据角色ID查询其所有菜单
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>菜单实体列表</returns>
    public async Task<IEnumerable<MenuEntity>> GetMenusByRoleIdAsync(int roleId)
    {
        return await _dbContext.RoleMenus
            .Where(rm => rm.RoleId == roleId)
            .Include(rm => rm.Menu)
            .Select(rm => rm.Menu)
            .ToListAsync();
    }

    /// <summary>
    /// 根据用户ID查询其所有菜单（通过角色关联）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>菜单实体列表（已去重）</returns>
    public async Task<IEnumerable<MenuEntity>> GetMenusByUserIdAsync(int userId)
    {
        return await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_dbContext.RoleMenus, ur => ur.RoleId, rm => rm.RoleId, (ur, rm) => rm)
            .Include(rm => rm.Menu)
            .Select(rm => rm.Menu)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// 根据父菜单ID查询子菜单列表
    /// </summary>
    /// <param name="parentId">父菜单ID</param>
    /// <returns>子菜单实体列表（按排序号排序）</returns>
    public async Task<IEnumerable<MenuEntity>> GetMenusByParentIdAsync(int parentId)
    {
        return await _dbContext.Menus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.Sort)
            .ToListAsync();
    }
}
