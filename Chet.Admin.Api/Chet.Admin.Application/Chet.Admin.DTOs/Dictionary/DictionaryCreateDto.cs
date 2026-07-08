namespace Chet.Admin.DTOs.Dictionary
{
    /// <summary>
    /// 字典创建DTO
    /// </summary>
    public class DictionaryCreateDto
    {
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
