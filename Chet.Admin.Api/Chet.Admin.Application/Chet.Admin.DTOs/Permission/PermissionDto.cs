namespace Chet.Admin.DTOs.Permission
{
    /// <summary>
    /// 权限DTO
    /// </summary>
    public class PermissionDto
    {
        /// <summary>
        /// 权限ID
        /// </summary>
        public int Id { get; set; }

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
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 权限描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 关联菜单ID
        /// </summary>
        public int? MenuId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
