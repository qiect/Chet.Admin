using AutoMapper;
using Chet.Admin.Contracts;
using Chet.Admin.Contracts.Cache;
using Chet.Admin.Contracts.Role;
using Chet.Admin.Contracts.Security;
using Chet.Admin.Contracts.User;
using Chet.Admin.Domain.User;
using Chet.Admin.DTOs.User;
using Chet.Admin.Services.User;
using Chet.Admin.Shared;
using Chet.Admin.Shared.Caching;
using Microsoft.Extensions.Logging;
using Moq;

namespace Chet.Admin.UnitTests
{
    /// <summary>
    /// 用户服务单元测试类，用于测试UserService的各种功能
    /// 该测试类使用模拟对象来隔离被测服务与其依赖项
    /// </summary>
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;       // 模拟用户存储库
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;              // 模拟工作单元
        private readonly Mock<ICacheService> _mockCacheService;          // 模拟缓存服务
        private readonly Mock<IPasswordService> _mockPasswordService;     // 模拟密码服务
        private readonly Mock<IMapper> _mockMapper;                     // 模拟对象映射器
        private readonly Mock<ILogger<UserService>> _mockLogger;        // 模拟日志服务
        private readonly UserService _userService;                      // 被测试的服务实例

        /// <summary>
        /// 构造函数，初始化测试所需的模拟对象和服务实例
        /// </summary>
        public UserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCacheService = new Mock<ICacheService>();
            _mockPasswordService = new Mock<IPasswordService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UserService>>();
            var mockDataScopeService = new Mock<IDataScopeService>();

            _userService = new UserService(
                _mockUserRepository.Object,
                _mockUnitOfWork.Object,
                _mockCacheService.Object,
                _mockPasswordService.Object,
                mockDataScopeService.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        /// <summary>
        /// 测试根据有效ID获取用户的方法
        /// 验证当提供有效用户ID时，服务应正确返回对应的用户DTO
        /// </summary>
        [Fact]
        public async Task GetUserByIdAsync_WithValidId_ReturnsUserDto()
        {
            var userId = 1;
            var userEntity = new UserEntity { Id = userId, Name = "Test User", Email = "test@example.com" };
            var expectedUserDto = new UserDto { Id = userId, Name = "Test User", Email = "test@example.com", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };

            _mockCacheService.Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<UserDto>>>(), It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<UserDto>>, TimeSpan>((key, factory, expiry) => factory());
            _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(userEntity);
            _mockMapper.Setup(x => x.Map<UserDto>(userEntity)).Returns(expectedUserDto);

            var result = await _userService.GetUserByIdAsync(userId);

            Assert.Equal(expectedUserDto.Id, result.Id);
            Assert.Equal(expectedUserDto.Name, result.Name);
            Assert.Equal(expectedUserDto.Email, result.Email);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockMapper.Verify(x => x.Map<UserDto>(userEntity), Times.Once);
        }

        /// <summary>
        /// 测试根据无效ID获取用户的方法
        /// 验证当提供不存在的用户ID时，服务应抛出NotFoundException
        /// </summary>
        [Fact]
        public async Task GetUserByIdAsync_WithInvalidId_ThrowsNotFoundException()
        {
            var userId = 999;

            _mockCacheService.Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<UserDto>>>(), It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<UserDto>>, TimeSpan>((key, factory, expiry) => factory());
            _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((UserEntity)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetUserByIdAsync(userId));
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        /// <summary>
        /// 测试获取所有用户的方法
        /// 验证服务能够正确返回所有用户列表
        /// </summary>
        [Fact]
        public async Task GetAllUsersAsync_ReturnsAllUsers()
        {
            var users = new List<UserEntity>
            {
                new UserEntity { Id = 1, Name = "User 1", Email = "user1@example.com" },
                new UserEntity { Id = 2, Name = "User 2", Email = "user2@example.com" }
            }.AsEnumerable();

            var expectedUserDtos = new List<UserDto>
            {
                new UserDto { Id = 1, Name = "User 1", Email = "user1@example.com", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new UserDto { Id = 2, Name = "User 2", Email = "user2@example.com", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
            }.AsEnumerable();

            _mockCacheService.Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<IEnumerable<UserDto>>>>(), It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<IEnumerable<UserDto>>>, TimeSpan>((key, factory, expiry) => factory());
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
            _mockMapper.Setup(x => x.Map<IEnumerable<UserDto>>(users)).Returns(expectedUserDtos);

            var result = await _userService.GetAllUsersAsync();

            Assert.Equal(2, result.Count());
            _mockUserRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockMapper.Verify(x => x.Map<IEnumerable<UserDto>>(users), Times.Once);
        }

        /// <summary>
        /// 测试创建用户的方法
        /// 验证当提供有效数据时，服务应正确创建新用户并返回创建的用户DTO
        /// </summary>
        [Fact]
        public async Task CreateUserAsync_WithValidData_CreatesAndReturnsUser()
        {
            var userCreateDto = new UserCreateDto
            {
                Name = "New User",
                Email = "newuser@example.com",
                Password = "password123"
            };

            var userEntity = new UserEntity
            {
                Id = 1,
                Name = "New User",
                Email = "newuser@example.com"
            };

            var expectedUserDto = new UserDto
            {
                Id = 1,
                Name = "New User",
                Email = "newuser@example.com",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _mockPasswordService.Setup(x => x.Hash(userCreateDto.Password)).Returns("hashed_password");
            _mockMapper.Setup(x => x.Map<UserEntity>(userCreateDto)).Returns(userEntity);
            _mockUserRepository.Setup(x => x.AddAsync(userEntity)).Returns(Task.CompletedTask);
            _mockUserRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(CacheKeys.Users.Pattern)).Returns(Task.CompletedTask);
            _mockMapper.Setup(x => x.Map<UserDto>(userEntity)).Returns(expectedUserDto);

            var result = await _userService.CreateUserAsync(userCreateDto);

            Assert.Equal(expectedUserDto.Id, result.Id);
            Assert.Equal(expectedUserDto.Name, result.Name);
            Assert.Equal(expectedUserDto.Email, result.Email);
            _mockMapper.Verify(x => x.Map<UserEntity>(userCreateDto), Times.Once);
            _mockUserRepository.Verify(x => x.AddAsync(userEntity), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(CacheKeys.Users.Pattern), Times.Once);
            _mockMapper.Verify(x => x.Map<UserDto>(userEntity), Times.Once);
        }

        /// <summary>
        /// 测试更新用户的方法
        /// 验证当提供有效数据时，服务应正确更新用户信息并清除相关缓存
        /// </summary>
        [Fact]
        public async Task UpdateUserAsync_WithValidData_UpdatesUser()
        {
            var userId = 1;
            var userUpdateDto = new UserUpdateDto
            {
                Name = "Updated User",
                Email = "updated@example.com"
            };

            var existingUser = new UserEntity
            {
                Id = userId,
                Name = "Old Name",
                Email = "old@example.com"
            };

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockMapper.Setup(x => x.Map(userUpdateDto, existingUser)).Callback(() =>
            {
                existingUser.Name = userUpdateDto.Name;
                existingUser.Email = userUpdateDto.Email;
            });
            _mockUserRepository.Setup(x => x.Update(existingUser));
            _mockUserRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
            _mockCacheService.Setup(x => x.RemoveAsync(CacheKeys.Users.ById(userId))).Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(CacheKeys.Users.Pattern)).Returns(Task.CompletedTask);

            await _userService.UpdateUserAsync(userId, userUpdateDto);

            Assert.Equal(userUpdateDto.Name, existingUser.Name);
            Assert.Equal(userUpdateDto.Email, existingUser.Email);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockMapper.Verify(x => x.Map(userUpdateDto, existingUser), Times.Once);
            _mockUserRepository.Verify(x => x.Update(existingUser), Times.Once);
            _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.Users.ById(userId)), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(CacheKeys.Users.Pattern), Times.Once);
        }

        /// <summary>
        /// 测试使用无效ID更新用户的方法
        /// 验证当尝试更新不存在的用户时，服务应抛出NotFoundException
        /// </summary>
        [Fact]
        public async Task UpdateUserAsync_WithInvalidId_ThrowsNotFoundException()
        {
            var userId = 999;
            var userUpdateDto = new UserUpdateDto
            {
                Name = "Updated User",
                Email = "updated@example.com"
            };

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((UserEntity)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _userService.UpdateUserAsync(userId, userUpdateDto));
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        /// <summary>
        /// 测试删除用户的方法
        /// 验证当提供有效ID时，服务应正确删除用户并清除相关缓存
        /// </summary>
        [Fact]
        public async Task DeleteUserAsync_WithValidId_DeletesUser()
        {
            var userId = 1;
            var existingUser = new UserEntity
            {
                Id = userId,
                Name = "User to Delete",
                Email = "delete@example.com"
            };

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserRepository.Setup(x => x.Delete(existingUser));
            _mockUserRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
            _mockCacheService.Setup(x => x.RemoveAsync(CacheKeys.Users.ById(userId))).Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(CacheKeys.Users.Pattern)).Returns(Task.CompletedTask);

            await _userService.DeleteUserAsync(userId);

            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockUserRepository.Verify(x => x.Delete(existingUser), Times.Once);
            _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.Users.ById(userId)), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(CacheKeys.Users.Pattern), Times.Once);
        }

        /// <summary>
        /// 测试使用无效ID删除用户的方法
        /// 验证当尝试删除不存在的用户时，服务应抛出NotFoundException
        /// </summary>
        [Fact]
        public async Task DeleteUserAsync_WithInvalidId_ThrowsNotFoundException()
        {
            var userId = 999;

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((UserEntity)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _userService.DeleteUserAsync(userId));
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }
    }
}
