namespace Chet.Admin.Domain.Dictionary
{
    /// <summary>
    /// 字典实体
    /// </summary>
    public class DictionaryEntity : BaseEntity
    {
        /// <summary>
        /// 字典类型编码
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
        /// 排序号
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 父级ID
        /// </summary>
        public int ParentId { get; set; }
    }
}
