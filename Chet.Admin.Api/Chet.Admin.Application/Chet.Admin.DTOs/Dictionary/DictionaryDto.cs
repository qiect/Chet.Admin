namespace Chet.Admin.DTOs.Dictionary
{
    /// <summary>
    /// 字典DTO
    /// </summary>
    public class DictionaryDto
    {
        /// <summary>
        /// 字典ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 字典类型
        /// </summary>
        public string DictType { get; set; } = string.Empty;

        /// <summary>
        /// 字典名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 字典值
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 字典标签
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 父级ID
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
