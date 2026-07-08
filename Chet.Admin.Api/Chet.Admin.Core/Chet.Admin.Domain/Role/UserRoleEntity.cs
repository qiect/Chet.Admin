using Chet.Admin.Domain.User;

namespace Chet.Admin.Domain.Role
{
    /// <summary>
    /// 用户角色关联实体
    /// </summary>
    public class UserRoleEntity
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 角色ID
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// 导航属性-用户
        /// </summary>
        public UserEntity User { get; set; } = null!;

        /// <summary>
        /// 导航属性-角色
        /// </summary>
        public RoleEntity Role { get; set; } = null!;
    }
}
