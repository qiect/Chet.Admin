namespace Chet.Admin.DTOs.Role
{
    public class RoleUpdateDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Sort { get; set; }
        public bool? IsEnabled { get; set; }
        public string? DataScope { get; set; }
    }
}
