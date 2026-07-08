namespace Chet.Admin.Domain.Department
{
    /// <summary>
    /// 部门实体
    /// </summary>
    public class DepartmentEntity : BaseEntity
    {
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
        /// 父部门ID（0表示顶级部门）
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 子部门
        /// </summary>
        public List<DepartmentEntity> Children { get; set; } = [];
    }
}
