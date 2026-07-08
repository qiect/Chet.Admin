namespace Chet.Admin.DTOs.Role
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Sort { get; set; }
        public bool IsEnabled { get; set; }
        public string DataScope { get; set; } = "All";
        public DateTime CreatedAt { get; set; }
    }
}
