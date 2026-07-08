using Chet.Admin.Contracts.Role;
using Chet.Admin.Domain.Role;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data.Role;

public class RoleRepository : EfCoreRepository<RoleEntity>, IRoleRepository
{
    public RoleRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<RoleEntity?> GetByCodeAsync(string code)
    {
        return await _dbContext.Roles.FirstOrDefaultAsync(r => r.Code == code);
    }

    public async Task<IEnumerable<RoleEntity>> GetRolesByUserIdAsync(int userId)
    {
        return await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role)
            .ToListAsync();
    }
}
