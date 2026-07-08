using Chet.Admin.Contracts;
using Chet.Admin.Domain.User;

namespace Chet.Admin.Contracts.User;

/// <summary>
/// 用户仓储接口（User Repository Interface）
/// <para>
/// 继承通用仓储接口，专门针对UserEntity实体提供用户特定的数据访问方法。
/// 封装了所有与用户相关的数据库查询操作。
/// </para>
/// </summary>
/// <remarks>
/// <para>设计原则：</para>
/// <para>
/// 遵循接口隔离原则（ISP），将特定于用户的查询方法
/// 从通用仓储中分离出来，保持接口的职责单一。
/// </para>
/// 
/// <para>扩展方法：</para>
/// <list type="table">
///   <listheader>
        ///     <term>方法</term>
///     <description>说明</description>
    ///   </listheader>
///   <item>
///     <term>GetByEmailAsync</term>
///     <description>根据邮箱地址查询用户（登录认证时使用）</description>
///   </item>
///   <item>
///     <term>(继承自IRepository)</term>
///     <description>通用的CRUD操作：GetById、GetAll、Add、Update、Delete等</description>
///   </item>
/// </list>
/// 
/// <para>使用示例：</para>
/// <code>
/// public class AuthService : IAuthService
/// {
///     private readonly IUserRepository _userRepository;
///     
///     public async Task&lt;LoginResponse&gt; LoginAsync(LoginDto dto)
///     {
///         // 使用用户特定的查询方法
///         var user = await _userRepository.GetByEmailAsync(dto.Email);
///         
///         if (user == null || !_passwordService.Verify(dto.Password, user.PasswordHash))
///             throw new UnauthorizedException("Invalid credentials");
///             
///         return GenerateToken(user);
///     }
/// }
/// </code>
/// </remarks>
public interface IUserRepository : IRepository<UserEntity>
{
    /// <summary>
    /// 根据邮箱地址获取用户实体
    /// <para>
    /// 主要用于用户登录认证场景。
    /// 邮箱字段通常配置了唯一索引以保证查询效率。
    /// </para>
    /// </summary>
    /// <param name="email">用户的邮箱地址（不区分大小写）</param>
    /// <returns>
    /// 匹配的用户实体；如果不存在则返回null
    /// </returns>
    /// <remarks>
    /// <para>性能优化：</para>
    /// <para>
    /// 建议在Email列上创建唯一索引：
    /// <code>
    /// CREATE UNIQUE INDEX IX_Users_Email ON Users(Email);
    /// </code>
    /// </para>
    /// 
    /// <para>安全提示：</para>
    /// <para>
    /// 返回的实体包含密码哈希值，请确保不要在API响应中直接返回此对象，
    /// 应通过AutoMapper映射到DTO后再返回给客户端。
    /// </para>
    /// </remarks>
    Task<UserEntity> GetByEmailAsync(string email);
}
