namespace Chet.Admin.DTOs.Auth
{
    /// <summary>
    /// 用户信息DTO（登录后返回）
    /// </summary>
    public class UserInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? Avatar { get; set; }
        public List<string> Roles { get; set; } = [];
        public List<string> Permissions { get; set; } = [];
    }
}
