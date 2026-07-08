using AutoMapper;
using Chet.Admin.Contracts;
using Chet.Admin.Contracts.Role;
using Chet.Admin.Contracts.Menu;
using Chet.Admin.Domain.Role;
using Chet.Admin.DTOs.Menu;
using Chet.Admin.DTOs.Role;
using Chet.Admin.Shared;
using Chet.Admin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Role;

/// <summary>
/// 角色服务实现
/// </summary>
public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IRoleRepository roleRepository,
        IMenuRepository menuRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RoleService> logger)
    {
        _roleRepository = roleRepository;
        _menuRepository = menuRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 根据ID获取角色信息
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <returns>角色数据传输对象</returns>
    public async Task<RoleDto> GetRoleByIdAsync(int id)
    {
        _logger.LogInformation("Getting role by id: {Id}", id);
        var role = await _roleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(RoleEntity), id);
        return _mapper.Map<RoleDto>(role);
    }

    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    /// <returns>角色数据传输对象集合</returns>
    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        _logger.LogInformation("Getting all roles");
        var roles = await _roleRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<RoleDto>>(roles);
    }

    /// <summary>
    /// 分页查询角色列表
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>分页角色列表</returns>
    public async Task<PagedResult<RoleDto>> GetPagedRolesAsync(PagedRequest request)
    {
        _logger.LogInformation("Getting paged roles: Page {PageNumber}, Size {PageSize}", request.PageNumber, request.PageSize);
        request.Normalize();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var dbContext = (AppDbContext)_unitOfWork.DbContext;
            var keyword = request.Keyword.Trim();
            var query = dbContext.Roles.AsNoTracking()
                .Where(r => r.Code.Contains(keyword) || r.Name.Contains(keyword));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            var roleDtos = _mapper.Map<List<RoleDto>>(items);
            return new PagedResult<RoleDto>(roleDtos, request.PageNumber, request.PageSize, totalCount);
        }

        var pagedRoles = await _roleRepository.GetPagedAsync(request);
        var roleDtos2 = _mapper.Map<List<RoleDto>>(pagedRoles.Items);
        return new PagedResult<RoleDto>(roleDtos2, request.PageNumber, request.PageSize, pagedRoles.Metadata.TotalCount);
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="dto">角色创建信息</param>
    /// <returns>创建后的角色数据传输对象</returns>
    public async Task<RoleDto> CreateRoleAsync(RoleCreateDto dto)
    {
        _logger.LogInformation("Creating role: {Code}", dto.Code);
        var existingRole = await _roleRepository.GetByCodeAsync(dto.Code);
        if (existingRole != null)
            throw new BadRequestException($"Role code '{dto.Code}' already exists");

        var role = _mapper.Map<RoleEntity>(dto);
        await _roleRepository.AddAsync(role);
        await _roleRepository.SaveChangesAsync();
        return _mapper.Map<RoleDto>(role);
    }

    /// <summary>
    /// 更新角色信息
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="dto">角色更新信息</param>
    public async Task UpdateRoleAsync(int id, RoleUpdateDto dto)
    {
        _logger.LogInformation("Updating role: {Id}", id);
        var role = await _roleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(RoleEntity), id);
        _mapper.Map(dto, role);
        _roleRepository.Update(role);
        await _roleRepository.SaveChangesAsync();
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    /// <param name="id">角色ID</param>
    public async Task DeleteRoleAsync(int id)
    {
        _logger.LogInformation("Deleting role: {Id}", id);
        var role = await _roleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(RoleEntity), id);
        _roleRepository.Delete(role);
        await _roleRepository.SaveChangesAsync();
    }

    /// <summary>
    /// 为角色分配菜单
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="menuIds">菜单ID列表</param>
    public async Task AssignMenusAsync(int roleId, List<int> menuIds)
    {
        _logger.LogInformation("Assigning menus to role: {RoleId}", roleId);
        var role = await _roleRepository.GetByIdAsync(roleId)
            ?? throw new NotFoundException(nameof(RoleEntity), roleId);

        var dbContext = (AppDbContext)_unitOfWork.DbContext;

        // 删除旧的关联
        var existing = await dbContext.RoleMenus.Where(rm => rm.RoleId == roleId).ToListAsync();
        dbContext.RoleMenus.RemoveRange(existing);

        // 添加新的关联
        foreach (var menuId in menuIds)
        {
            await dbContext.RoleMenus.AddAsync(new RoleMenuEntity { RoleId = roleId, MenuId = menuId });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// 获取角色拥有的菜单列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>菜单数据传输对象集合</returns>
    public async Task<IEnumerable<MenuDto>> GetRoleMenusAsync(int roleId)
    {
        _logger.LogInformation("Getting menus for role: {RoleId}", roleId);
        var menus = await _menuRepository.GetMenusByRoleIdAsync(roleId);
        return _mapper.Map<IEnumerable<MenuDto>>(menus);
    }

    /// <summary>
    /// 更新角色数据权限范围
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="dataScope">数据权限范围（All、DeptAndChild、Dept、Self、Custom）</param>
    /// <param name="customDeptIds">自定义部门ID列表，仅当dataScope为Custom时生效</param>
    public async Task UpdateDataScopeAsync(int roleId, string dataScope, List<int>? customDeptIds)
    {
        _logger.LogInformation("Updating data scope for role: {RoleId}, Scope: {DataScope}", roleId, dataScope);
        var role = await _roleRepository.GetByIdAsync(roleId)
            ?? throw new NotFoundException(nameof(RoleEntity), roleId);

        role.DataScope = dataScope;
        _roleRepository.Update(role);

        var dbContext = (AppDbContext)_unitOfWork.DbContext;

        // 删除旧的自定义部门关联
        var existing = await dbContext.RoleDataScopeDepts.Where(rd => rd.RoleId == roleId).ToListAsync();
        dbContext.RoleDataScopeDepts.RemoveRange(existing);

        // 如果是 Custom 范围，添加新的部门关联
        if (dataScope == "Custom" && customDeptIds is { Count: > 0 })
        {
            foreach (var deptId in customDeptIds)
            {
                await dbContext.RoleDataScopeDepts.AddAsync(new RoleDataScopeDeptEntity { RoleId = roleId, DepartmentId = deptId });
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
