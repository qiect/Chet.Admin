using Chet.Admin.Contracts;
using Chet.Admin.Domain.Role;

namespace Chet.Admin.Contracts.Role
{
    /// <summary>
    /// 角色仓储接口
    /// </summary>
    public interface IRoleRepository : IRepository<RoleEntity>
    {
        /// <summary>
        /// 根据角色编码获取角色
        /// </summary>
        /// <param name="code">角色编码</param>
        /// <returns>角色实体，不存在则返回null</returns>
        Task<RoleEntity?> GetByCodeAsync(string code);

        /// <summary>
        /// 根据用户ID获取角色列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>角色实体集合</returns>
        Task<IEnumerable<RoleEntity>> GetRolesByUserIdAsync(int userId);
    }
}
