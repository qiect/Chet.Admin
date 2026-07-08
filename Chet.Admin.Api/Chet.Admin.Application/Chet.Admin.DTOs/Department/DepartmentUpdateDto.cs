namespace Chet.Admin.DTOs.Department
{
    /// <summary>
    /// 部门更新DTO
    /// </summary>
    public class DepartmentUpdateDto
    {
        /// <summary>
        /// 部门名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 部门编码
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 负责人
        /// </summary>
        public string? Leader { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 父级ID
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int? Sort { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool? IsEnabled { get; set; }
    }
}
