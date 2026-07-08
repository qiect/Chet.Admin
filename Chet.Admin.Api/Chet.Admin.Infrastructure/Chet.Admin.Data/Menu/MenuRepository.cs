using Chet.Admin.Contracts.Menu;
using Chet.Admin.Domain.Menu;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data.Menu;

public class MenuRepository : EfCoreRepository<MenuEntity>, IMenuRepository
{
    public MenuRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<MenuEntity>> GetMenusByRoleIdAsync(int roleId)
    {
        return await _dbContext.RoleMenus
            .Where(rm => rm.RoleId == roleId)
            .Include(rm => rm.Menu)
            .Select(rm => rm.Menu)
            .ToListAsync();
    }

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

    public async Task<IEnumerable<MenuEntity>> GetMenusByParentIdAsync(int parentId)
    {
        return await _dbContext.Menus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.Sort)
            .ToListAsync();
    }
}
