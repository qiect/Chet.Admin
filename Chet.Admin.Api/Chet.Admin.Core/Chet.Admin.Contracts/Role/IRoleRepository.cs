using Chet.Admin.Contracts;
using Chet.Admin.Domain.Role;

namespace Chet.Admin.Contracts.Role
{
    public interface IRoleRepository : IRepository<RoleEntity>
    {
        Task<RoleEntity?> GetByCodeAsync(string code);
        Task<IEnumerable<RoleEntity>> GetRolesByUserIdAsync(int userId);
    }
}
