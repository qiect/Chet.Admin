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
    /// 标记用户下线
    /// </summary>
    /// <param name="userId">用户ID</param>
    void UserOffline(int userId);

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
