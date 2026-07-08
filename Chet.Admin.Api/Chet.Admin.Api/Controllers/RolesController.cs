using Chet.Admin.Contracts.Role;
using Chet.Admin.DTOs.Role;
using Chet.Admin.DTOs.Permission;
using Chet.Admin.DTOs.Menu;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 角色管理控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供角色管理相关的API接口")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    /// <summary>
    /// 初始化角色控制器的新实例
    /// </summary>
    /// <param name="roleService">角色服务接口</param>
    /// <param name="logger">日志记录器</param>
    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有角色
    /// </summary>
    /// <returns>角色列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return Ok(ApiResponse.Ok(roles, "Roles retrieved successfully"));
    }

    /// <summary>
    /// 分页获取角色列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>分页角色列表</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedRoles([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
        var result = await _roleService.GetPagedRolesAsync(request);
        return Ok(PaginatedResponse<RoleDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Roles retrieved successfully"));
    }

    /// <summary>
    /// 根据ID获取角色详情
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <returns>角色详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(int id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        return Ok(ApiResponse.Ok(role, "Role retrieved successfully"));
    }

    /// <summary>
    /// 创建新角色
    /// </summary>
    /// <param name="dto">角色创建数据传输对象</param>
    /// <returns>创建的角色信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole(RoleCreateDto dto)
    {
        var role = await _roleService.CreateRoleAsync(dto);
        return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, ApiResponse.Ok(role, "Role created successfully", StatusCodes.Status201Created));
    }

    /// <summary>
    /// 更新角色信息
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="dto">角色更新数据传输对象</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(int id, RoleUpdateDto dto)
    {
        await _roleService.UpdateRoleAsync(id, dto);
        return Ok(ApiResponse.NoContent("Role updated successfully"));
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(int id)
    {
        await _roleService.DeleteRoleAsync(id);
        return Ok(ApiResponse.NoContent("Role deleted successfully"));
    }

    /// <summary>
    /// 获取角色所拥有的权限列表
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <returns>权限列表</returns>
    [HttpGet("{id}/permissions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolePermissions(int id)
    {
        var permissions = await _roleService.GetRolePermissionsAsync(id);
        return Ok(ApiResponse.Ok(permissions, "Role permissions retrieved successfully"));
    }

    /// <summary>
    /// 为角色分配权限
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="permissionIds">权限ID列表</param>
    /// <returns>分配结果</returns>
    [HttpPost("{id}/permissions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignPermissions(int id, [FromBody] List<int> permissionIds)
    {
        await _roleService.AssignPermissionsAsync(id, permissionIds);
        return Ok(ApiResponse.Ok(null, "Permissions assigned successfully"));
    }

    /// <summary>
    /// 获取角色所拥有的菜单列表
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <returns>菜单列表</returns>
    [HttpGet("{id}/menus")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoleMenus(int id)
    {
        var menus = await _roleService.GetRoleMenusAsync(id);
        return Ok(ApiResponse.Ok(menus, "Role menus retrieved successfully"));
    }

    /// <summary>
    /// 为角色分配菜单
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="menuIds">菜单ID列表</param>
    /// <returns>分配结果</returns>
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
    /// <param name="id">角色ID</param>
    /// <param name="dto">数据权限更新数据传输对象</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}/data-scope")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDataScope(int id, [FromBody] UpdateDataScopeDto dto)
    {
        await _roleService.UpdateDataScopeAsync(id, dto.DataScope, dto.CustomDeptIds);
        return Ok(ApiResponse.Ok(null, "Data scope updated successfully"));
    }
}
