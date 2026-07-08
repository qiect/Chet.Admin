using Chet.Admin.Contracts.Menu;
using Chet.Admin.DTOs.Menu;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供菜单管理相关的API接口")]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ILogger<MenusController> _logger;

    public MenusController(IMenuService menuService, ILogger<MenusController> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMenus()
    {
        var menus = await _menuService.GetAllMenusAsync();
        return Ok(ApiResponse.Ok(menus, "Menus retrieved successfully"));
    }

    [HttpGet("tree")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuTree()
    {
        var tree = await _menuService.GetMenuTreeAsync();
        return Ok(ApiResponse.Ok(tree, "Menu tree retrieved successfully"));
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<MenuDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedMenus([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
        var result = await _menuService.GetPagedMenusAsync(request);
        return Ok(PaginatedResponse<MenuDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Menus retrieved successfully"));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMenuById(int id)
    {
        var menu = await _menuService.GetMenuByIdAsync(id);
        return Ok(ApiResponse.Ok(menu, "Menu retrieved successfully"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMenu(MenuCreateDto dto)
    {
        var menu = await _menuService.CreateMenuAsync(dto);
        return CreatedAtAction(nameof(GetMenuById), new { id = menu.Id }, ApiResponse.Ok(menu, "Menu created successfully", StatusCodes.Status201Created));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateMenu(int id, MenuUpdateDto dto)
    {
        await _menuService.UpdateMenuAsync(id, dto);
        return Ok(ApiResponse.NoContent("Menu updated successfully"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMenu(int id)
    {
        await _menuService.DeleteMenuAsync(id);
        return Ok(ApiResponse.NoContent("Menu deleted successfully"));
    }
}
