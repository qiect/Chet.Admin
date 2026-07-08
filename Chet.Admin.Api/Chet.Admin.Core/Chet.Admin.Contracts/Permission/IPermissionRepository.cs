using Chet.Admin.Contracts;
using Chet.Admin.Domain.Permission;

namespace Chet.Admin.Contracts.Permission
{
    public interface IPermissionRepository : IRepository<PermissionEntity>
    {
        Task<PermissionEntity?> GetByCodeAsync(string code);
        Task<IEnumerable<PermissionEntity>> GetPermissionsByRoleIdAsync(int roleId);
        Task<IEnumerable<PermissionEntity>> GetPermissionsByUserIdAsync(int userId);
    }
}
