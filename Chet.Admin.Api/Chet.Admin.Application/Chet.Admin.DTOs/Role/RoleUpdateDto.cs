namespace Chet.Admin.DTOs.Role
{
    /// <summary>
    /// 角色更新DTO
    /// </summary>
    public class RoleUpdateDto
    {
        /// <summary>
        /// 角色编码
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 角色描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int? Sort { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// 数据权限范围
        /// </summary>
        public string? DataScope { get; set; }
    }
}
