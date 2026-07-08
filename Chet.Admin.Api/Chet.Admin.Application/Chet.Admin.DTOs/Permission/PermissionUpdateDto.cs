namespace Chet.Admin.DTOs.Permission
{
    public class PermissionUpdateDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public int? MenuId { get; set; }
    }
}
