namespace Chet.Admin.DTOs.Permission
{
    /// <summary>
    /// 权限更新DTO
    /// </summary>
    public class PermissionUpdateDto
    {
        /// <summary>
        /// 权限编码
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 权限名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 权限类型
        /// </summary>
        public string? Type { get; set; }

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
