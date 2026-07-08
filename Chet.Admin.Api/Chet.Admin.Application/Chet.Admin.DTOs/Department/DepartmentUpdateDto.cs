namespace Chet.Admin.DTOs.Department
{
    public class DepartmentUpdateDto
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Leader { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int? ParentId { get; set; }
        public int? Sort { get; set; }
        public bool? IsEnabled { get; set; }
    }
}
