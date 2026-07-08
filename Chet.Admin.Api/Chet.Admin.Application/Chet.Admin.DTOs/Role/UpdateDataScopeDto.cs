namespace Chet.Admin.DTOs.Role;

public class UpdateDataScopeDto
{
    public required string DataScope { get; set; }
    public List<int>? CustomDeptIds { get; set; }
}
