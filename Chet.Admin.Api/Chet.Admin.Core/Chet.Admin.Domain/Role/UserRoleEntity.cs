using Chet.Admin.Domain.User;

namespace Chet.Admin.Domain.Role
{
    /// <summary>
    /// 用户角色关联实体
    /// </summary>
    public class UserRoleEntity
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }

        /// <summary>
        /// 导航属性
        /// </summary>
        public UserEntity User { get; set; } = null!;
        public RoleEntity Role { get; set; } = null!;
    }
}
