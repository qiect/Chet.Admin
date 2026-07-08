namespace Chet.Admin.DTOs.Permission
{
    public class PermissionCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Button";
        public string? Description { get; set; }
        public int? MenuId { get; set; }
    }
}
