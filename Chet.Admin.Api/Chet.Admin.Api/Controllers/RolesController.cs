using Chet.Admin.Contracts.Role;
using Chet.Admin.DTOs.Role;
using Chet.Admin.DTOs.Permission;
using Chet.Admin.DTOs.Menu;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供角色管理相关的API接口")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return Ok(ApiResponse.Ok(roles, "Roles retrieved successfully"));
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedRoles([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
        var result = await _roleService.GetPagedRolesAsync(request);
        return Ok(PaginatedResponse<RoleDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Roles retrieved successfully"));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(int id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        return Ok(ApiResponse.Ok(role, "Role retrieved successfully"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole(RoleCreateDto dto)
    {
        var role = await _roleService.CreateRoleAsync(dto);
        return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, ApiResponse.Ok(role, "Role created successfully", StatusCodes.Status201Created));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(int id, RoleUpdateDto dto)
    {
        await _roleService.UpdateRoleAsync(id, dto);
        return Ok(ApiResponse.NoContent("Role updated successfully"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(int id)
    {
        await _roleService.DeleteRoleAsync(id);
        return Ok(ApiResponse.NoContent("Role deleted successfully"));
    }

    [HttpGet("{id}/permissions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolePermissions(int id)
    {
        var permissions = await _roleService.GetRolePermissionsAsync(id);
        return Ok(ApiResponse.Ok(permissions, "Role permissions retrieved successfully"));
    }

    [HttpPost("{id}/permissions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignPermissions(int id, [FromBody] List<int> permissionIds)
    {
        await _roleService.AssignPermissionsAsync(id, permissionIds);
        return Ok(ApiResponse.Ok(null, "Permissions assigned successfully"));
    }

    [HttpGet("{id}/menus")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoleMenus(int id)
    {
        var menus = await _roleService.GetRoleMenusAsync(id);
        return Ok(ApiResponse.Ok(menus, "Role menus retrieved successfully"));
    }

    [HttpPost("{id}/menus")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignMenus(int id, [FromBody] List<int> menuIds)
    {
        await _roleService.AssignMenusAsync(id, menuIds);
        return Ok(ApiResponse.Ok(null, "Menus assigned successfully"));
    }

    /// <summary>
    /// 更新角色数据权限
    /// </summary>
    [HttpPut("{id}/data-scope")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDataScope(int id, [FromBody] UpdateDataScopeDto dto)
    {
        await _roleService.UpdateDataScopeAsync(id, dto.DataScope, dto.CustomDeptIds);
        return Ok(ApiResponse.Ok(null, "Data scope updated successfully"));
    }
}
