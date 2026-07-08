using AutoMapper;
using Chet.Admin.Contracts;
using Chet.Admin.Contracts.Permission;
using Chet.Admin.Data;
using Chet.Admin.Domain.Permission;
using Chet.Admin.DTOs.Permission;
using Chet.Admin.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Permission;

/// <summary>
/// 权限服务实现
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 根据ID获取权限信息
    /// </summary>
    /// <param name="id">权限ID</param>
    /// <returns>权限数据传输对象</returns>
    public async Task<PermissionDto> GetPermissionByIdAsync(int id)
    {
        _logger.LogInformation("Getting permission by id: {Id}", id);
        var permission = await _permissionRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(PermissionEntity), id);
        return _mapper.Map<PermissionDto>(permission);
    }

    /// <summary>
    /// 获取所有权限列表
    /// </summary>
    /// <returns>权限数据传输对象集合</returns>
    public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync()
    {
        _logger.LogInformation("Getting all permissions");
        var permissions = await _permissionRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<PermissionDto>>(permissions);
    }

    /// <summary>
    /// 分页查询权限列表
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>分页权限列表</returns>
    public async Task<PagedResult<PermissionDto>> GetPagedPermissionsAsync(PagedRequest request)
    {
        _logger.LogInformation("Getting paged permissions: Page {PageNumber}, Size {PageSize}", request.PageNumber, request.PageSize);
        request.Normalize();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var dbContext = (AppDbContext)_unitOfWork.DbContext;
            var keyword = request.Keyword.Trim();
            var query = dbContext.Permissions.AsNoTracking()
                .Where(p => p.Code.Contains(keyword) || p.Name.Contains(keyword));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            var permissionDtos = _mapper.Map<List<PermissionDto>>(items);
            return new PagedResult<PermissionDto>(permissionDtos, request.PageNumber, request.PageSize, totalCount);
        }

        var pagedPermissions = await _permissionRepository.GetPagedAsync(request);
        var permissionDtos2 = _mapper.Map<List<PermissionDto>>(pagedPermissions.Items);
        return new PagedResult<PermissionDto>(permissionDtos2, request.PageNumber, request.PageSize, pagedPermissions.Metadata.TotalCount);
    }

    /// <summary>
    /// 创建权限
    /// </summary>
    /// <param name="dto">权限创建信息</param>
    /// <returns>创建后的权限数据传输对象</returns>
    public async Task<PermissionDto> CreatePermissionAsync(PermissionCreateDto dto)
    {
        _logger.LogInformation("Creating permission: {Code}", dto.Code);
        var existing = await _permissionRepository.GetByCodeAsync(dto.Code);
        if (existing != null)
            throw new BadRequestException($"Permission code '{dto.Code}' already exists");

        var permission = _mapper.Map<PermissionEntity>(dto);
        await _permissionRepository.AddAsync(permission);
        await _permissionRepository.SaveChangesAsync();
        return _mapper.Map<PermissionDto>(permission);
    }

    /// <summary>
    /// 更新权限信息
    /// </summary>
    /// <param name="id">权限ID</param>
    /// <param name="dto">权限更新信息</param>
    public async Task UpdatePermissionAsync(int id, PermissionUpdateDto dto)
    {
        _logger.LogInformation("Updating permission: {Id}", id);
        var permission = await _permissionRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(PermissionEntity), id);
        _mapper.Map(dto, permission);
        _permissionRepository.Update(permission);
        await _permissionRepository.SaveChangesAsync();
    }

    /// <summary>
    /// 删除权限
    /// </summary>
    /// <param name="id">权限ID</param>
    public async Task DeletePermissionAsync(int id)
    {
        _logger.LogInformation("Deleting permission: {Id}", id);
        var permission = await _permissionRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(PermissionEntity), id);
        _permissionRepository.Delete(permission);
        await _permissionRepository.SaveChangesAsync();
    }
}
