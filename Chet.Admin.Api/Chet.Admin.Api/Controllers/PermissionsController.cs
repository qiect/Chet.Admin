using Chet.Admin.Contracts.Permission;
using Chet.Admin.DTOs.Permission;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 权限管理控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供权限管理相关的API接口")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionsController> _logger;

    /// <summary>
    /// 初始化权限控制器的新实例
    /// </summary>
    /// <param name="permissionService">权限服务接口</param>
    /// <param name="logger">日志记录器</param>
    public PermissionsController(IPermissionService permissionService, ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有权限
    /// </summary>
    /// <returns>权限列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await _permissionService.GetAllPermissionsAsync();
        return Ok(ApiResponse.Ok(permissions, "Permissions retrieved successfully"));
    }

    /// <summary>
    /// 分页获取权限列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>分页权限列表</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedPermissions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
        var result = await _permissionService.GetPagedPermissionsAsync(request);
        return Ok(PaginatedResponse<PermissionDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Permissions retrieved successfully"));
    }

    /// <summary>
    /// 根据ID获取权限详情
    /// </summary>
    /// <param name="id">权限ID</param>
    /// <returns>权限详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissionById(int id)
    {
        var permission = await _permissionService.GetPermissionByIdAsync(id);
        return Ok(ApiResponse.Ok(permission, "Permission retrieved successfully"));
    }

    /// <summary>
    /// 创建新权限
    /// </summary>
    /// <param name="dto">权限创建数据传输对象</param>
    /// <returns>创建的权限信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePermission(PermissionCreateDto dto)
    {
        var permission = await _permissionService.CreatePermissionAsync(dto);
        return CreatedAtAction(nameof(GetPermissionById), new { id = permission.Id }, ApiResponse.Ok(permission, "Permission created successfully", StatusCodes.Status201Created));
    }

    /// <summary>
    /// 更新权限信息
    /// </summary>
    /// <param name="id">权限ID</param>
    /// <param name="dto">权限更新数据传输对象</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePermission(int id, PermissionUpdateDto dto)
    {
        await _permissionService.UpdatePermissionAsync(id, dto);
        return Ok(ApiResponse.NoContent("Permission updated successfully"));
    }

    /// <summary>
    /// 删除权限
    /// </summary>
    /// <param name="id">权限ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePermission(int id)
    {
        await _permissionService.DeletePermissionAsync(id);
        return Ok(ApiResponse.NoContent("Permission deleted successfully"));
    }
}
