namespace Chet.Admin.DTOs.Role
{
    public class RoleCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Sort { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
