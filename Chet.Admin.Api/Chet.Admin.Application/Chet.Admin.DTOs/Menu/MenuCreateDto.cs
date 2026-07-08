namespace Chet.Admin.DTOs.Menu
{
    public class MenuCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Component { get; set; }
        public string? Redirect { get; set; }
        public string? Icon { get; set; }
        public int ParentId { get; set; }
        public string Type { get; set; } = "Menu";
        public int Sort { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsExternal { get; set; }
        public bool IsCache { get; set; }
        public bool IsVisible { get; set; } = true;
        public string? Permission { get; set; }
    }
}
