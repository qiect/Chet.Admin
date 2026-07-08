namespace Chet.Admin.DTOs.Department
{
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Leader { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int ParentId { get; set; }
        public int Sort { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
