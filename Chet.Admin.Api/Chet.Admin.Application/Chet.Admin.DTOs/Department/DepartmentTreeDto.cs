namespace Chet.Admin.DTOs.Department
{
    /// <summary>
    /// 部门树形DTO
    /// </summary>
    public class DepartmentTreeDto
    {
        /// <summary>
        /// 部门ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 部门名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 部门编码
        /// </summary>
        public string Code { get; set; } = string.Empty;

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
        public int ParentId { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 子部门列表
        /// </summary>
        public List<DepartmentTreeDto> Children { get; set; } = [];
    }
}
