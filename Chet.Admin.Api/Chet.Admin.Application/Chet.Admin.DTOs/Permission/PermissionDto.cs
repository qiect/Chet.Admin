namespace Chet.Admin.DTOs.Permission
{
    public class PermissionDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? MenuId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
