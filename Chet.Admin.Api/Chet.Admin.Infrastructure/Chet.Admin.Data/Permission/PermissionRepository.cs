using Chet.Admin.Contracts.Permission;
using Chet.Admin.Domain.Permission;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data.Permission;

public class PermissionRepository : EfCoreRepository<PermissionEntity>, IPermissionRepository
{
    public PermissionRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<PermissionEntity?> GetByCodeAsync(string code)
    {
        return await _dbContext.Permissions.FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<IEnumerable<PermissionEntity>> GetPermissionsByRoleIdAsync(int roleId)
    {
        return await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission)
            .ToListAsync();
    }

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
