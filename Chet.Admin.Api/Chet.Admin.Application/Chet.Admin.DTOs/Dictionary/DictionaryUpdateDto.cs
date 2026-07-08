namespace Chet.Admin.DTOs.Dictionary
{
    public class DictionaryUpdateDto
    {
        public string? DictType { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string? Label { get; set; }
        public int? Sort { get; set; }
        public bool? IsEnabled { get; set; }
        public string? Remark { get; set; }
        public int? ParentId { get; set; }
    }
}
