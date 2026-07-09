using Chet.Admin.Contracts.Menu;
using Chet.Admin.DTOs.Menu;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 菜单管理控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供菜单管理相关的API接口")]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ILogger<MenusController> _logger;

    /// <summary>
    /// 初始化菜单控制器的新实例
    /// </summary>
    /// <param name="menuService">菜单服务接口</param>
    /// <param name="logger">日志记录器</param>
    public MenusController(IMenuService menuService, ILogger<MenusController> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有菜单
    /// </summary>
    /// <returns>菜单列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMenus()
    {
        var menus = await _menuService.GetAllMenusAsync();
        return Ok(ApiResponse.Ok(menus, "Menus retrieved successfully"));
    }

    /// <summary>
    /// 获取菜单树形结构
    /// </summary>
    /// <returns>菜单树</returns>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuTree()
    {
        var tree = await _menuService.GetMenuTreeAsync();
        return Ok(ApiResponse.Ok(tree, "Menu tree retrieved successfully"));
    }

    /// <summary>
    /// 获取当前登录用户的菜单树形结构（按角色过滤）
    /// </summary>
    /// <returns>当前用户的菜单树</returns>
    [HttpGet("my-tree")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyMenus()
    {
        var userId = GetUserId();
        var tree = await _menuService.GetMyMenuTreeAsync(userId);
        return Ok(ApiResponse.Ok(tree, "User menus retrieved successfully"));
    }

    /// <summary>
    /// 分页获取菜单列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>分页菜单列表</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<MenuDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedMenus([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
        var result = await _menuService.GetPagedMenusAsync(request);
        return Ok(PaginatedResponse<MenuDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Menus retrieved successfully"));
    }

    /// <summary>
    /// 根据ID获取菜单详情
    /// </summary>
    /// <param name="id">菜单ID</param>
    /// <returns>菜单详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMenuById(int id)
    {
        var menu = await _menuService.GetMenuByIdAsync(id);
        return Ok(ApiResponse.Ok(menu, "Menu retrieved successfully"));
    }

    /// <summary>
    /// 创建新菜单
    /// </summary>
    /// <param name="dto">菜单创建数据传输对象</param>
    /// <returns>创建的菜单信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMenu(MenuCreateDto dto)
    {
        var menu = await _menuService.CreateMenuAsync(dto);
        return CreatedAtAction(nameof(GetMenuById), new { id = menu.Id }, ApiResponse.Ok(menu, "Menu created successfully", StatusCodes.Status201Created));
    }

    /// <summary>
    /// 更新菜单信息
    /// </summary>
    /// <param name="id">菜单ID</param>
    /// <param name="dto">菜单更新数据传输对象</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateMenu(int id, MenuUpdateDto dto)
    {
        await _menuService.UpdateMenuAsync(id, dto);
        return Ok(ApiResponse.NoContent("Menu updated successfully"));
    }

    /// <summary>
    /// 删除菜单
    /// </summary>
    /// <param name="id">菜单ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMenu(int id)
    {
        await _menuService.DeleteMenuAsync(id);
        return Ok(ApiResponse.NoContent("Menu deleted successfully"));
    }

    /// <summary>
    /// 从JWT Claims中获取当前用户ID
    /// </summary>
    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (claim == null || !int.TryParse(claim.Value, out var userId))
        {
            throw new UnauthorizedAccessException();
        }
        return userId;
    }
}
