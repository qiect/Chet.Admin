namespace Chet.Admin.DTOs.Role
{
    /// <summary>
    /// 角色创建DTO
    /// </summary>
    public class RoleCreateDto
    {
        /// <summary>
        /// 角色编码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 角色描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}
