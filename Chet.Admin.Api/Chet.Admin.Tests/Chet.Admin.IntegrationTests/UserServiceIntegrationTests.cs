using Chet.Admin.Contracts.Cache;
using Chet.Admin.Contracts.Security;
using Chet.Admin.Contracts.User;
using Chet.Admin.Data;
using Chet.Admin.Data.User;
using Chet.Admin.Domain.User;
using Chet.Admin.DTOs.User;
using Chet.Admin.Mapping.User;
using Chet.Admin.Services.Security;
using Chet.Admin.Services.User;
using Chet.Admin.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.IntegrationTests
{
    /// <summary>
    /// 用户服务集成测试类，用于测试UserService与其他组件（如数据库、映射器等）的集成
    /// 该测试类使用内存数据库，以避免对外部依赖的需求
    /// </summary>
    public class UserServiceIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;      // 服务容器
        private readonly AppDbContext _dbContext;              // 数据库上下文
        private readonly IUserService _userService;            // 用户服务实例
        private readonly IUserRepository _userRepository;      // 用户存储库实例

        /// <summary>
        /// 构造函数，初始化测试所需的服务和依赖项
        /// 配置内存数据库和各种服务注册
        /// </summary>
        public UserServiceIntegrationTests()
        {
            // 设置内存数据库用于测试
            var services = new ServiceCollection();

            // 注册DbContext使用内存数据库，每个测试使用唯一的数据库名称
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            // 注册其他服务
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();
            
            // 注册密码服务
            services.AddScoped<IPasswordService, PasswordService>();

            // 注册AutoMapper，自动映射配置
            services.AddAutoMapper(typeof(MappingProfile));

            // 注册日志服务
            services.AddLogging(builder => builder.AddConsole());

            // 注册缓存服务（使用NoOpCacheService进行测试，避免实际缓存影响）
            services.AddSingleton<ICacheService, NoOpCacheService>();

            _serviceProvider = services.BuildServiceProvider();

            // 获取服务实例
            _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
            _userService = _serviceProvider.GetRequiredService<IUserService>();
            _userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
        }

        /// <summary>
        /// 测试根据存在的用户ID获取用户的方法
        /// 验证当数据库中存在指定ID的用户时，服务应正确返回对应的用户信息
        /// </summary>
        [Fact]
        public async Task GetUserByIdAsync_WithExistingUser_ReturnsUser()
        {
            // Arrange - 准备测试数据：创建并保存一个用户到内存数据库
            var user = new UserEntity
            {
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act - 执行被测方法
            var result = await _userService.GetUserByIdAsync(user.Id);

            // Assert - 验证结果是否符合预期
            Assert.NotNull(result);
            Assert.Equal(user.Name, result.Name);
            Assert.Equal(user.Email, result.Email);
        }

        /// <summary>
        /// 测试根据不存在的用户ID获取用户的方法
        /// 验证当数据库中不存在指定ID的用户时，服务应抛出NotFoundException
        /// </summary>
        [Fact]
        public async Task GetUserByIdAsync_WithNonExistingUser_ThrowsNotFoundException()
        {
            // Act & Assert - 验证调用方法会抛出NotFoundException
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetUserByIdAsync(999));
        }

        /// <summary>
        /// 测试获取所有用户的方法
        /// 验证服务能够正确返回数据库中的所有用户
        /// </summary>
        [Fact]
        public async Task GetAllUsersAsync_ReturnsAllUsers()
        {
            // Arrange - 准备测试数据：创建并保存多个用户到内存数据库
            var users = new List<UserEntity>
            {
                new UserEntity { Name = "User 1", Email = "user1@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") },
                new UserEntity { Name = "User 2", Email = "user2@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") }
            };

            await _dbContext.Users.AddRangeAsync(users);
            await _dbContext.SaveChangesAsync();

            // Act - 执行被测方法
            var result = await _userService.GetAllUsersAsync();

            // Assert - 验证结果是否符合预期
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        /// <summary>
        /// 测试创建用户功能 - 当提供有效数据时，应成功创建用户
        /// 此测试验证CreateUserAsync方法能够正确接收用户创建请求，
        /// 创建新用户实体，并将其持久化到数据库中
        /// </summary>
        [Fact]
        public async Task CreateUserAsync_WithValidData_CreatesUser()
        {
            // Arrange - 准备测试数据：创建用户输入DTO
            var userCreateDto = new UserCreateDto
            {
                Name = "New User",           // 设置用户名
                Email = "newuser@example.com", // 设置邮箱
                Password = "password123"     // 设置密码
            };

            // Act - 执行操作：调用服务创建用户
            var result = await _userService.CreateUserAsync(userCreateDto);

            // Assert - 验证结果：
            // 1. 验证返回结果不为空
            Assert.NotNull(result);
            // 2. 验证返回的用户对象包含正确的姓名
            Assert.Equal(userCreateDto.Name, result.Name);
            // 3. 验证返回的用户对象包含正确的邮箱
            Assert.Equal(userCreateDto.Email, result.Email);

            // 验证用户已保存到数据库 - 从数据库中检索刚创建的用户
            var savedUser = await _userRepository.GetByIdAsync(result.Id);
            // 验证数据库中的用户存在且数据正确
            Assert.NotNull(savedUser);
            Assert.Equal(userCreateDto.Name, savedUser.Name);
            Assert.Equal(userCreateDto.Email, savedUser.Email);
        }

        /// <summary>
        /// 测试更新用户功能 - 当提供有效数据时，应成功更新现有用户
        /// 此测试验证UpdateUserAsync方法能够正确接收用户更新请求，
        /// 将指定ID的用户信息更新为新的值，并持久化到数据库中
        /// </summary>
        [Fact]
        public async Task UpdateUserAsync_WithValidData_UpdatesUser()
        {
            // Arrange - 准备测试数据：创建并保存一个初始用户到数据库
            var user = new UserEntity
            {
                Name = "Original Name",              // 初始用户名
                Email = "original@example.com",      // 初始邮箱
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") // 初始密码哈希
            };

            // 将用户添加到数据库上下文中并保存
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // 准备更新数据：创建用户更新DTO
            var userUpdateDto = new UserUpdateDto
            {
                Name = "Updated Name",               // 新用户名
                Email = "updated@example.com"        // 新邮箱
            };

            // Act - 执行操作：调用服务更新用户信息
            await _userService.UpdateUserAsync(user.Id, userUpdateDto);

            // Assert - 验证结果：
            // 1. 从数据库获取更新后的用户
            var updatedUser = await _userRepository.GetByIdAsync(user.Id);
            // 2. 验证用户存在
            Assert.NotNull(updatedUser);
            // 3. 验证用户名已更新
            Assert.Equal(userUpdateDto.Name, updatedUser.Name);
            // 4. 验证邮箱已更新
            Assert.Equal(userUpdateDto.Email, updatedUser.Email);
        }

        /// <summary>
        /// 测试更新不存在的用户 - 当尝试更新一个不存在的用户时，应抛出NotFoundException
        /// 此测试验证UpdateUserAsync方法在找不到指定ID的用户时，
        /// 能够正确抛出NotFoundException异常，确保业务逻辑的完整性
        /// </summary>
        [Fact]
        public async Task UpdateUserAsync_WithNonExistingUser_ThrowsNotFoundException()
        {
            // Arrange - 准备测试数据：创建用户更新DTO，但不创建对应的用户
            var userUpdateDto = new UserUpdateDto
            {
                Name = "Updated Name",           // 更新后的用户名
                Email = "updated@example.com"    // 更新后的邮箱
            };

            // Act & Assert - 执行操作并验证异常：
            // 调用更新方法时传入一个不存在的用户ID（999），应抛出NotFoundException
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.UpdateUserAsync(999, userUpdateDto));
        }

        /// <summary>
        /// 测试删除用户功能 - 当提供存在的用户ID时，应成功删除用户
        /// 此测试验证DeleteUserAsync方法能够正确接收用户删除请求，
        /// 从数据库中移除指定ID的用户记录
        /// </summary>
        [Fact]
        public async Task DeleteUserAsync_WithExistingUser_DeletesUser()
        {
            // Arrange - 准备测试数据：创建并保存一个待删除的用户到数据库
            var user = new UserEntity
            {
                Name = "User to Delete",                   // 待删除的用户名
                Email = "delete@example.com",              // 待删除的邮箱
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") // 密码哈希
            };

            // 将用户添加到数据库上下文中并保存
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act - 执行操作：调用服务删除用户
            await _userService.DeleteUserAsync(user.Id);

            // Assert - 验证结果：
            // 1. 从数据库中获取已删除的用户
            var deletedUser = await _userRepository.GetByIdAsync(user.Id);
            // 2. 验证用户已被成功删除（返回null）
            Assert.Null(deletedUser);
        }

        /// <summary>
        /// 测试删除不存在的用户 - 当尝试删除一个不存在的用户时，应抛出NotFoundException
        /// 此测试验证DeleteUserAsync方法在找不到指定ID的用户时，
        /// 能够正确抛出NotFoundException异常，确保业务逻辑的完整性
        /// </summary>
        [Fact]
        public async Task DeleteUserAsync_WithNonExistingUser_ThrowsNotFoundException()
        {
            // Act & Assert - 执行操作并验证异常：
            // 调用删除方法时传入一个不存在的用户ID（999），应抛出NotFoundException
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.DeleteUserAsync(999));
        }

        /// <summary>
        /// 实现IDisposable接口，用于清理测试期间创建的资源
        /// 释放数据库上下文和服务容器
        /// </summary>
        public void Dispose()
        {
            _dbContext?.Dispose();
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// 为测试创建的空操作缓存服务实现
    /// 此实现不执行任何实际缓存操作，仅用于测试目的
    /// 避免在集成测试中使用真实的缓存服务
    /// </summary>
    public class NoOpCacheService : ICacheService
    {
        public Task<T> GetAsync<T>(string key)
        {
            return Task.FromResult(default(T)!);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            return Task.CompletedTask;
        }

        public Task RemoveByPatternAsync(string pattern)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            return Task.FromResult(false);
        }

        public Task<string[]> GetKeysByPatternAsync(string pattern)
        {
            return Task.FromResult(Array.Empty<string>());
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            return await factory();
        }

        public Task<bool> PingAsync()
        {
            return Task.FromResult(true);
        }
    }
}