namespace Chet.Admin.DTOs.Menu
{
    public class MenuTreeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Component { get; set; }
        public string? Redirect { get; set; }
        public string? Icon { get; set; }
        public int ParentId { get; set; }
        public string Type { get; set; } = string.Empty;
        public int Sort { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsExternal { get; set; }
        public bool IsCache { get; set; }
        public bool IsVisible { get; set; }
        public string? Permission { get; set; }
        public List<MenuTreeDto> Children { get; set; } = [];
    }
}
