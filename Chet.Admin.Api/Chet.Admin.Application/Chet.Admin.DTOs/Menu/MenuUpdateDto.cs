namespace Chet.Admin.DTOs.Menu
{
    public class MenuUpdateDto
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Component { get; set; }
        public string? Redirect { get; set; }
        public string? Icon { get; set; }
        public int? ParentId { get; set; }
        public string? Type { get; set; }
        public int? Sort { get; set; }
        public bool? IsEnabled { get; set; }
        public bool? IsExternal { get; set; }
        public bool? IsCache { get; set; }
        public bool? IsVisible { get; set; }
        public string? Permission { get; set; }
    }
}
