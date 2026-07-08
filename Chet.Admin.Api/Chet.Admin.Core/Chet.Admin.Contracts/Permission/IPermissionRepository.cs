using Chet.Admin.Contracts;
using Chet.Admin.Domain.Permission;

namespace Chet.Admin.Contracts.Permission
{
    /// <summary>
    /// 权限仓储接口
    /// </summary>
    public interface IPermissionRepository : IRepository<PermissionEntity>
    {
        /// <summary>
        /// 根据权限编码获取权限
        /// </summary>
        /// <param name="code">权限编码</param>
        /// <returns>权限实体，不存在则返回null</returns>
        Task<PermissionEntity?> GetByCodeAsync(string code);

        /// <summary>
        /// 根据角色ID获取权限列表
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <returns>权限实体集合</returns>
        Task<IEnumerable<PermissionEntity>> GetPermissionsByRoleIdAsync(int roleId);

        /// <summary>
        /// 根据用户ID获取权限列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>权限实体集合</returns>
        Task<IEnumerable<PermissionEntity>> GetPermissionsByUserIdAsync(int userId);
    }
}
