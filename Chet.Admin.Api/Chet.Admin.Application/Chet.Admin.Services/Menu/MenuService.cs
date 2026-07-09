using AutoMapper;
using Chet.Admin.Contracts;
using Chet.Admin.Contracts.Menu;
using Chet.Admin.Data;
using Chet.Admin.Domain.Menu;
using Chet.Admin.DTOs.Menu;
using Chet.Admin.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Menu;

/// <summary>
/// 菜单服务实现
/// </summary>
public class MenuService : IMenuService
{
    private readonly IMenuRepository _menuRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<MenuService> _logger;

    public MenuService(
        IMenuRepository menuRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<MenuService> logger)
    {
        _menuRepository = menuRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 根据ID获取菜单信息
    /// </summary>
    /// <param name="id">菜单ID</param>
    /// <returns>菜单数据传输对象</returns>
    public async Task<MenuDto> GetMenuByIdAsync(int id)
    {
        _logger.LogInformation("Getting menu by id: {Id}", id);
        var menu = await _menuRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(MenuEntity), id);
        return _mapper.Map<MenuDto>(menu);
    }

    /// <summary>
    /// 获取所有菜单列表
    /// </summary>
    /// <returns>菜单数据传输对象集合</returns>
    public async Task<IEnumerable<MenuDto>> GetAllMenusAsync()
    {
        _logger.LogInformation("Getting all menus");
        var menus = await _menuRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<MenuDto>>(menus);
    }

    /// <summary>
    /// 获取菜单树形结构
    /// </summary>
    /// <returns>菜单树形结构集合</returns>
    public async Task<IEnumerable<MenuTreeDto>> GetMenuTreeAsync()
    {
        _logger.LogInformation("Getting menu tree");
        var menus = await _menuRepository.GetAllAsync();
        var menuDtos = _mapper.Map<List<MenuTreeDto>>(menus);
        return BuildMenuTree(menuDtos, 0);
    }

    /// <summary>
    /// 获取当前用户的菜单树形结构
    /// 仅包含用户被分配的菜单及其所有祖先菜单，确保树形层级完整
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>菜单树形结构集合</returns>
    public async Task<IEnumerable<MenuTreeDto>> GetMyMenuTreeAsync(int userId)
    {
        _logger.LogInformation("Getting menu tree for user: {UserId}", userId);

        var allMenus = (await _menuRepository.GetAllAsync()).ToList();
        var userMenus = (await _menuRepository.GetMenusByUserIdAsync(userId)).ToList();

        if (userMenus.Count == 0)
        {
            return [];
        }

        // 收集需要包含的菜单ID（被分配的菜单 + 其所有祖先菜单）
        var includedIds = new HashSet<int>();
        var menuById = allMenus.ToDictionary(m => m.Id);

        foreach (var menu in userMenus)
        {
            var current = menu;
            while (current != null && includedIds.Add(current.Id))
            {
                if (current.ParentId > 0
                    && menuById.TryGetValue(current.ParentId, out var parent))
                {
                    current = parent;
                }
                else
                {
                    break;
                }
            }
        }

        var filteredMenus = allMenus.Where(m => includedIds.Contains(m.Id)).ToList();
        var menuDtos = _mapper.Map<List<MenuTreeDto>>(filteredMenus);
        return BuildMenuTree(menuDtos, 0);
    }

    /// <summary>
    /// 分页查询菜单列表
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>分页菜单列表</returns>
    public async Task<PagedResult<MenuDto>> GetPagedMenusAsync(PagedRequest request)
    {
        _logger.LogInformation("Getting paged menus: Page {PageNumber}, Size {PageSize}", request.PageNumber, request.PageSize);
        request.Normalize();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var dbContext = (AppDbContext)_unitOfWork.DbContext;
            var keyword = request.Keyword.Trim();
            var query = dbContext.Menus.AsNoTracking()
                .Where(m => m.Name.Contains(keyword) || m.Path.Contains(keyword));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            var menuDtos = _mapper.Map<List<MenuDto>>(items);
            return new PagedResult<MenuDto>(menuDtos, request.PageNumber, request.PageSize, totalCount);
        }

        var pagedMenus = await _menuRepository.GetPagedAsync(request);
        var menuDtos2 = _mapper.Map<List<MenuDto>>(pagedMenus.Items);
        return new PagedResult<MenuDto>(menuDtos2, request.PageNumber, request.PageSize, pagedMenus.Metadata.TotalCount);
    }

    /// <summary>
    /// 创建菜单
    /// </summary>
    /// <param name="dto">菜单创建信息</param>
    /// <returns>创建后的菜单数据传输对象</returns>
    public async Task<MenuDto> CreateMenuAsync(MenuCreateDto dto)
    {
        _logger.LogInformation("Creating menu: {Name}", dto.Name);
        var menu = _mapper.Map<MenuEntity>(dto);
        await _menuRepository.AddAsync(menu);
        await _menuRepository.SaveChangesAsync();
        return _mapper.Map<MenuDto>(menu);
    }

    /// <summary>
    /// 更新菜单信息
    /// </summary>
    /// <param name="id">菜单ID</param>
    /// <param name="dto">菜单更新信息</param>
    public async Task UpdateMenuAsync(int id, MenuUpdateDto dto)
    {
        _logger.LogInformation("Updating menu: {Id}", id);
        var menu = await _menuRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(MenuEntity), id);
        _mapper.Map(dto, menu);
        _menuRepository.Update(menu);
        await _menuRepository.SaveChangesAsync();
    }

    /// <summary>
    /// 删除菜单
    /// </summary>
    /// <param name="id">菜单ID</param>
    public async Task DeleteMenuAsync(int id)
    {
        _logger.LogInformation("Deleting menu: {Id}", id);
        var menu = await _menuRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(MenuEntity), id);
        _menuRepository.Delete(menu);
        await _menuRepository.SaveChangesAsync();
    }

    /// <summary>
    /// 递归构建菜单树形结构
    /// </summary>
    /// <param name="allMenus">所有菜单列表</param>
    /// <param name="parentId">父节点ID</param>
    /// <returns>树形结构菜单集合</returns>
    private static IEnumerable<MenuTreeDto> BuildMenuTree(List<MenuTreeDto> allMenus, int parentId)
    {
        return allMenus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.Sort)
            .Select(m =>
            {
                m.Children = BuildMenuTree(allMenus, m.Id).ToList();
                return m;
            })
            .ToList();
    }
}
