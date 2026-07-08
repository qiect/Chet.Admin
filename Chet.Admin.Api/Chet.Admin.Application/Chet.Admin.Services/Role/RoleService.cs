using AutoMapper;
using Chet.Admin.Contracts;
using Chet.Admin.Contracts.Role;
using Chet.Admin.Contracts.Menu;
using Chet.Admin.Contracts.Permission;
using Chet.Admin.Domain.Role;
using Chet.Admin.DTOs.Menu;
using Chet.Admin.DTOs.Permission;
using Chet.Admin.DTOs.Role;
using Chet.Admin.Shared;
using Chet.Admin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Role;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMenuRepository menuRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RoleService> logger)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RoleDto> GetRoleByIdAsync(int id)
    {
        _logger.LogInformation("Getting role by id: {Id}", id);
        var role = await _roleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(RoleEntity), id);
        return _mapper.Map<RoleDto>(role);
    }

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        _logger.LogInformation("Getting all roles");
        var roles = await _roleRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<RoleDto>>(roles);
    }

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

    public async Task UpdateRoleAsync(int id, RoleUpdateDto dto)
    {
        _logger.LogInformation("Updating role: {Id}", id);
        var role = await _roleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(RoleEntity), id);
        _mapper.Map(dto, role);
        _roleRepository.Update(role);
        await _roleRepository.SaveChangesAsync();
    }

    public async Task DeleteRoleAsync(int id)
    {
        _logger.LogInformation("Deleting role: {Id}", id);
        var role = await _roleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(RoleEntity), id);
        _roleRepository.Delete(role);
        await _roleRepository.SaveChangesAsync();
    }

    public async Task AssignPermissionsAsync(int roleId, List<int> permissionIds)
    {
        _logger.LogInformation("Assigning permissions to role: {RoleId}", roleId);
        var role = await _roleRepository.GetByIdAsync(roleId)
            ?? throw new NotFoundException(nameof(RoleEntity), roleId);

        var dbContext = (AppDbContext)_unitOfWork.DbContext;

        // 删除旧的关联
        var existing = await dbContext.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        dbContext.RolePermissions.RemoveRange(existing);

        // 添加新的关联
        foreach (var permissionId in permissionIds)
        {
            await dbContext.RolePermissions.AddAsync(new RolePermissionEntity { RoleId = roleId, PermissionId = permissionId });
        }

        await _unitOfWork.SaveChangesAsync();
    }

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

    public async Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(int roleId)
    {
        _logger.LogInformation("Getting permissions for role: {RoleId}", roleId);
        var permissions = await _permissionRepository.GetPermissionsByRoleIdAsync(roleId);
        return _mapper.Map<IEnumerable<PermissionDto>>(permissions);
    }

    public async Task<IEnumerable<MenuDto>> GetRoleMenusAsync(int roleId)
    {
        _logger.LogInformation("Getting menus for role: {RoleId}", roleId);
        var menus = await _menuRepository.GetMenusByRoleIdAsync(roleId);
        return _mapper.Map<IEnumerable<MenuDto>>(menus);
    }

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
