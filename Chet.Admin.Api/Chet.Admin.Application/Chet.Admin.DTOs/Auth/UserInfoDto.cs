namespace Chet.Admin.DTOs.Auth
{
    /// <summary>
    /// 用户信息DTO（登录后返回）
    /// </summary>
    public class UserInfoDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 部门ID
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 角色列表
        /// </summary>
        public List<string> Roles { get; set; } = [];

        /// <summary>
        /// 权限列表
        /// </summary>
        public List<string> Permissions { get; set; } = [];
    }
}
