namespace Chet.Admin.DTOs.Permission
{
    /// <summary>
    /// 权限创建DTO
    /// </summary>
    public class PermissionCreateDto
    {
        /// <summary>
        /// 权限编码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 权限名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 权限类型
        /// </summary>
        public string Type { get; set; } = "Button";

        /// <summary>
        /// 权限描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 关联菜单ID
        /// </summary>
        public int? MenuId { get; set; }
    }
}
