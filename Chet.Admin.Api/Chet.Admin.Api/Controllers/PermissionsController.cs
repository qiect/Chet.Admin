using Chet.Admin.Contracts.Permission;
using Chet.Admin.DTOs.Permission;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供权限管理相关的API接口")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(IPermissionService permissionService, ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await _permissionService.GetAllPermissionsAsync();
        return Ok(ApiResponse.Ok(permissions, "Permissions retrieved successfully"));
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedPermissions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
        var result = await _permissionService.GetPagedPermissionsAsync(request);
        return Ok(PaginatedResponse<PermissionDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Permissions retrieved successfully"));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissionById(int id)
    {
        var permission = await _permissionService.GetPermissionByIdAsync(id);
        return Ok(ApiResponse.Ok(permission, "Permission retrieved successfully"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePermission(PermissionCreateDto dto)
    {
        var permission = await _permissionService.CreatePermissionAsync(dto);
        return CreatedAtAction(nameof(GetPermissionById), new { id = permission.Id }, ApiResponse.Ok(permission, "Permission created successfully", StatusCodes.Status201Created));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePermission(int id, PermissionUpdateDto dto)
    {
        await _permissionService.UpdatePermissionAsync(id, dto);
        return Ok(ApiResponse.NoContent("Permission updated successfully"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePermission(int id)
    {
        await _permissionService.DeletePermissionAsync(id);
        return Ok(ApiResponse.NoContent("Permission deleted successfully"));
    }
}
