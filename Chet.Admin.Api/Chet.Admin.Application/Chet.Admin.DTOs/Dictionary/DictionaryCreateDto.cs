namespace Chet.Admin.DTOs.Dictionary
{
    public class DictionaryCreateDto
    {
        public string DictType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Sort { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? Remark { get; set; }
        public int ParentId { get; set; }
    }
}
