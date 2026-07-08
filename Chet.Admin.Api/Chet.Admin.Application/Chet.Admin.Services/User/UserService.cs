using AutoMapper;
using Chet.Admin.Contracts;
using Chet.Admin.Contracts.Cache;
using Chet.Admin.Contracts.Role;
using Chet.Admin.Contracts.Security;
using Chet.Admin.Contracts.User;
using Chet.Admin.Data;
using Chet.Admin.Domain.Role;
using Chet.Admin.Domain.User;
using Chet.Admin.DTOs.User;
using Chet.Admin.Shared;
using Chet.Admin.Shared.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.User;

/// <summary>
/// 用户服务实现类，实现了 IUserService 接口
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IPasswordService _passwordService;
    private readonly IDataScopeService _dataScopeService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IPasswordService passwordService,
        IDataScopeService dataScopeService,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _passwordService = passwordService;
        _dataScopeService = dataScopeService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 根据ID获取用户信息
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns>用户数据传输对象</returns>
    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        _logger.LogInformation("Getting user by id: {Id}", id);

        var cacheKey = CacheKeys.Users.ById(id);

        return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                throw new NotFoundException(nameof(UserEntity), id);
            }
            return _mapper.Map<UserDto>(user);
        }, CacheKeys.Expiry.Medium);
    }

    /// <summary>
    /// 获取所有用户列表
    /// </summary>
    /// <returns>用户数据传输对象集合</returns>
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        _logger.LogInformation("Getting all users");

        var cacheKey = CacheKeys.Users.All();

        return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }, CacheKeys.Expiry.Medium);
    }

    /// <summary>
    /// 分页查询用户列表
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>分页用户列表</returns>
    public async Task<PagedResult<UserDto>> GetPagedUsersAsync(PagedRequest request)
    {
        _logger.LogInformation("Getting paged users: Page {PageNumber}, Size {PageSize}", request.PageNumber, request.PageSize);

        request.Normalize();

        var dbContext = (AppDbContext)_unitOfWork.DbContext;

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            var query = dbContext.Users.AsNoTracking()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Where(u => u.Name.Contains(keyword) || u.Email.Contains(keyword));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            var userDtos = _mapper.Map<List<UserDto>>(items);
            return new PagedResult<UserDto>(userDtos, request.PageNumber, request.PageSize, totalCount);
        }

        // 无关键字分支：直接通过 DbContext 查询以包含 UserRoles 导航属性
        var baseQuery = dbContext.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role);
        var totalCount2 = await baseQuery.CountAsync();
        var items2 = await baseQuery
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        var userDtos2 = _mapper.Map<List<UserDto>>(items2);
        return new PagedResult<UserDto>(userDtos2, request.PageNumber, request.PageSize, totalCount2);
    }

    /// <summary>
    /// 根据当前用户数据权限分页查询用户列表
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <param name="currentUserId">当前登录用户ID，用于数据权限过滤</param>
    /// <returns>分页用户列表</returns>
    public async Task<PagedResult<UserDto>> GetPagedUsersAsync(PagedRequest request, int? currentUserId)
    {
        if (!currentUserId.HasValue)
            return await GetPagedUsersAsync(request);

        var dataScope = await _dataScopeService.GetDataScopeAsync(currentUserId.Value);

        // All 范围不做过滤
        if (dataScope == "All")
            return await GetPagedUsersAsync(request);

        request.Normalize();
        var dbContext = (AppDbContext)_unitOfWork.DbContext;
        IQueryable<UserEntity> query = dbContext.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

        // 关键字过滤
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(u => u.Name.Contains(keyword) || u.Email.Contains(keyword));
        }

        // 数据权限过滤
        switch (dataScope)
        {
            case "Self":
                query = query.Where(u => u.Id == currentUserId.Value);
                break;

            case "Dept":
                var currentUserDept = await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id == currentUserId.Value)
                    .Select(u => u.DepartmentId)
                    .FirstOrDefaultAsync();
                if (currentUserDept.HasValue)
                    query = query.Where(u => u.DepartmentId == currentUserDept.Value);
                else
                    query = query.Where(u => u.Id == currentUserId.Value);
                break;

            case "DeptAndChild":
            case "Custom":
                var accessibleDeptIds = await _dataScopeService.GetAccessibleDeptIdsAsync(currentUserId.Value);
                if (accessibleDeptIds.Count > 0)
                    query = query.Where(u => u.DepartmentId.HasValue && accessibleDeptIds.Contains(u.DepartmentId.Value));
                else
                    query = query.Where(u => u.Id == currentUserId.Value);
                break;
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        var userDtos = _mapper.Map<List<UserDto>>(items);
        return new PagedResult<UserDto>(userDtos, request.PageNumber, request.PageSize, totalCount);
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    /// <param name="userCreateDto">用户创建信息</param>
    /// <returns>创建后的用户数据传输对象</returns>
    public async Task<UserDto> CreateUserAsync(UserCreateDto userCreateDto)
    {
        _logger.LogInformation("Creating user: {Email}", userCreateDto.Email);

        var user = _mapper.Map<UserEntity>(userCreateDto);
        user.PasswordHash = _passwordService.Hash(userCreateDto.Password);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;

        // 设置部门
        if (userCreateDto.DepartmentId.HasValue)
        {
            user.DepartmentId = userCreateDto.DepartmentId;
        }

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // 如果指定了角色，创建用户角色关联
        if (userCreateDto.RoleIds is { Count: > 0 })
        {
            var dbContext = (AppDbContext)_unitOfWork.DbContext;
            foreach (var roleId in userCreateDto.RoleIds)
            {
                await dbContext.UserRoles.AddAsync(new UserRoleEntity { UserId = user.Id, RoleId = roleId });
            }
            await _unitOfWork.SaveChangesAsync();
        }

        await _cacheService.RemoveByPatternAsync(CacheKeys.Users.Pattern);

        return _mapper.Map<UserDto>(user);
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="userUpdateDto">用户更新信息</param>
    public async Task UpdateUserAsync(int id, UserUpdateDto userUpdateDto)
    {
        _logger.LogInformation("Updating user: {Id}", id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new NotFoundException(nameof(UserEntity), id);
        }

        // 更新基本信息（仅更新提供了值的字段）
        if (!string.IsNullOrWhiteSpace(userUpdateDto.Name))
        {
            user.Name = userUpdateDto.Name;
        }
        if (!string.IsNullOrWhiteSpace(userUpdateDto.Email))
        {
            user.Email = userUpdateDto.Email;
        }

        // 如果提供了密码，则更新密码
        if (!string.IsNullOrWhiteSpace(userUpdateDto.Password))
        {
            user.PasswordHash = _passwordService.Hash(userUpdateDto.Password);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.MustChangePassword = false;
        }

        // 更新部门
        if (userUpdateDto.DepartmentId.HasValue)
        {
            user.DepartmentId = userUpdateDto.DepartmentId;
        }

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        // 如果提供了角色列表，则重新分配角色
        if (userUpdateDto.RoleIds != null)
        {
            var dbContext = (AppDbContext)_unitOfWork.DbContext;

            // 删除旧的关联
            var existing = await dbContext.UserRoles.Where(ur => ur.UserId == id).ToListAsync();
            dbContext.UserRoles.RemoveRange(existing);

            // 添加新的关联
            foreach (var roleId in userUpdateDto.RoleIds)
            {
                await dbContext.UserRoles.AddAsync(new UserRoleEntity { UserId = id, RoleId = roleId });
            }

            await _unitOfWork.SaveChangesAsync();
        }

        await _cacheService.RemoveAsync(CacheKeys.Users.ById(id));
        await _cacheService.RemoveByPatternAsync(CacheKeys.Users.Pattern);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id">用户ID</param>
    public async Task DeleteUserAsync(int id)
    {
        _logger.LogInformation("Deleting user: {Id}", id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new NotFoundException(nameof(UserEntity), id);
        }

        _userRepository.Delete(user);
        await _userRepository.SaveChangesAsync();

        await _cacheService.RemoveAsync(CacheKeys.Users.ById(id));
        await _cacheService.RemoveByPatternAsync(CacheKeys.Users.Pattern);
    }

    /// <summary>
    /// 为用户分配角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleIds">角色ID列表</param>
    public async Task AssignRolesAsync(int userId, List<int> roleIds)
    {
        _logger.LogInformation("Assigning roles to user: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(UserEntity), userId);

        var dbContext = (AppDbContext)_unitOfWork.DbContext;

        // 删除旧的关联
        var existing = await dbContext.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        dbContext.UserRoles.RemoveRange(existing);

        // 添加新的关联
        foreach (var roleId in roleIds)
        {
            await dbContext.UserRoles.AddAsync(new UserRoleEntity { UserId = userId, RoleId = roleId });
        }

        await _unitOfWork.SaveChangesAsync();

        await _cacheService.RemoveAsync(CacheKeys.Users.ById(userId));
        await _cacheService.RemoveByPatternAsync(CacheKeys.Users.Pattern);
    }

    /// <summary>
    /// 创建用户并分配角色
    /// </summary>
    /// <param name="dto">用户创建信息</param>
    /// <param name="roleIds">角色ID列表</param>
    /// <returns>创建后的用户数据传输对象</returns>
    public async Task<UserDto> CreateUserWithRolesAsync(UserCreateDto dto, List<int> roleIds)
    {
        _logger.LogInformation("Creating user with roles: {Email}", dto.Email);

        var user = _mapper.Map<UserEntity>(dto);
        user.PasswordHash = _passwordService.Hash(dto.Password);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // 创建用户角色关联
        if (roleIds is { Count: > 0 })
        {
            var dbContext = (AppDbContext)_unitOfWork.DbContext;
            foreach (var roleId in roleIds)
            {
                await dbContext.UserRoles.AddAsync(new UserRoleEntity { UserId = user.Id, RoleId = roleId });
            }
            await _unitOfWork.SaveChangesAsync();
        }

        await _cacheService.RemoveByPatternAsync(CacheKeys.Users.Pattern);

        return _mapper.Map<UserDto>(user);
    }

    /// <summary>
    /// 修改用户密码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="oldPassword">旧密码</param>
    /// <param name="newPassword">新密码</param>
    public async Task ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        _logger.LogInformation("Changing password for user: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException(nameof(UserEntity), userId);
        }

        // 验证旧密码
        if (!_passwordService.Verify(oldPassword, user.PasswordHash))
        {
            throw new BadRequestException("旧密码不正确");
        }

        user.PasswordHash = _passwordService.Hash(newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        await _cacheService.RemoveAsync(CacheKeys.Users.ById(userId));
        await _cacheService.RemoveByPatternAsync(CacheKeys.Users.Pattern);
    }

    /// <summary>
    /// 更新用户个人资料
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="name">用户名称，为空则不更新</param>
    /// <param name="avatar">头像地址，为空则不更新</param>
    public async Task UpdateProfileAsync(int userId, string? name, string? avatar)
    {
        _logger.LogInformation("Updating profile for user: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException(nameof(UserEntity), userId);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            user.Name = name;
        }
        if (avatar != null)
        {
            user.Avatar = avatar;
        }

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        await _cacheService.RemoveAsync(CacheKeys.Users.ById(userId));
        await _cacheService.RemoveByPatternAsync(CacheKeys.Users.Pattern);
    }
}
