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

    public async Task<MenuDto> GetMenuByIdAsync(int id)
    {
        _logger.LogInformation("Getting menu by id: {Id}", id);
        var menu = await _menuRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(MenuEntity), id);
        return _mapper.Map<MenuDto>(menu);
    }

    public async Task<IEnumerable<MenuDto>> GetAllMenusAsync()
    {
        _logger.LogInformation("Getting all menus");
        var menus = await _menuRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<MenuDto>>(menus);
    }

    public async Task<IEnumerable<MenuTreeDto>> GetMenuTreeAsync()
    {
        _logger.LogInformation("Getting menu tree");
        var menus = await _menuRepository.GetAllAsync();
        var menuDtos = _mapper.Map<List<MenuTreeDto>>(menus);
        return BuildMenuTree(menuDtos, 0);
    }

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

    public async Task<MenuDto> CreateMenuAsync(MenuCreateDto dto)
    {
        _logger.LogInformation("Creating menu: {Name}", dto.Name);
        var menu = _mapper.Map<MenuEntity>(dto);
        await _menuRepository.AddAsync(menu);
        await _menuRepository.SaveChangesAsync();
        return _mapper.Map<MenuDto>(menu);
    }

    public async Task UpdateMenuAsync(int id, MenuUpdateDto dto)
    {
        _logger.LogInformation("Updating menu: {Id}", id);
        var menu = await _menuRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(MenuEntity), id);
        _mapper.Map(dto, menu);
        _menuRepository.Update(menu);
        await _menuRepository.SaveChangesAsync();
    }

    public async Task DeleteMenuAsync(int id)
    {
        _logger.LogInformation("Deleting menu: {Id}", id);
        var menu = await _menuRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(MenuEntity), id);
        _menuRepository.Delete(menu);
        await _menuRepository.SaveChangesAsync();
    }

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
