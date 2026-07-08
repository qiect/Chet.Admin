using Chet.Admin.DTOs.User;

namespace Chet.Admin.Contracts.User;

/// <summary>
/// 在线用户服务接口
/// </summary>
public interface IOnlineUserService
{
    /// <summary>
    /// 标记用户上线
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="clientIp">客户端IP地址</param>
    void UserOnline(int userId, string userName, string clientIp);

    /// <summary>
    /// 标记用户下线（仅移除在线记录，不使令牌失效）
    /// </summary>
    /// <param name="userId">用户ID</param>
    void UserOffline(int userId);

    /// <summary>
    /// 强制用户下线（移除在线记录并加入令牌黑名单，使已签发的JWT失效）
    /// </summary>
    /// <param name="userId">用户ID</param>
    void ForceOffline(int userId);

    /// <summary>
    /// 检查指定用户的令牌是否已被吊销（在黑名单中且令牌签发时间早于吊销时间）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="tokenIssuedAt">令牌签发时间（UTC）</param>
    /// <returns>已吊销返回true，否则返回false</returns>
    bool IsTokenRevoked(int userId, DateTime tokenIssuedAt);

    /// <summary>
    /// 从黑名单中移除用户（用户重新登录时调用，清除历史吊销记录）
    /// </summary>
    /// <param name="userId">用户ID</param>
    void RemoveFromBlacklist(int userId);

    /// <summary>
    /// 更新用户活动状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    void UpdateActivity(int userId);

    /// <summary>
    /// 获取所有在线用户列表
    /// </summary>
    /// <returns>在线用户DTO列表</returns>
    List<OnlineUserDto> GetOnlineUsers();

    /// <summary>
    /// 检查用户是否在线
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>在线返回true，否则返回false</returns>
    bool IsOnline(int userId);
}
