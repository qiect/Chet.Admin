using Chet.Admin.Contracts.User;
using Chet.Admin.DTOs.User;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Chet.Admin.Services.User;

/// <summary>
/// 在线用户服务实现
/// </summary>
public class OnlineUserService : IOnlineUserService
{
    private readonly ILogger<OnlineUserService> _logger;
    private static readonly ConcurrentDictionary<int, OnlineUserDto> _onlineUsers = new();

    public OnlineUserService(ILogger<OnlineUserService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 标记用户上线
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="clientIp">客户端IP地址</param>
    public void UserOnline(int userId, string userName, string clientIp)
    {
        var info = new OnlineUserDto
        {
            UserId = userId,
            UserName = userName,
            ClientIp = clientIp,
            LoginTime = DateTime.UtcNow,
            LastActiveTime = DateTime.UtcNow,
        };
        _onlineUsers.AddOrUpdate(userId, info, (_, _) => info);
    }

    /// <summary>
    /// 标记用户下线
    /// </summary>
    /// <param name="userId">用户ID</param>
    public void UserOffline(int userId)
    {
        _onlineUsers.TryRemove(userId, out _);
    }

    /// <summary>
    /// 更新用户最后活动时间
    /// </summary>
    /// <param name="userId">用户ID</param>
    public void UpdateActivity(int userId)
    {
        if (_onlineUsers.TryGetValue(userId, out var info))
        {
            info.LastActiveTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 获取所有在线用户列表
    /// </summary>
    /// <returns>在线用户列表，自动清理超过30分钟未活动的用户</returns>
    public List<OnlineUserDto> GetOnlineUsers()
    {
        // Remove stale entries (no activity for 30 min)
        var threshold = DateTime.UtcNow.AddMinutes(-30);
        foreach (var kvp in _onlineUsers)
        {
            if (kvp.Value.LastActiveTime < threshold)
            {
                _onlineUsers.TryRemove(kvp.Key, out _);
            }
        }
        return _onlineUsers.Values.OrderByDescending(x => x.LastActiveTime).ToList();
    }

    /// <summary>
    /// 判断用户是否在线
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>在线返回true，否则返回false</returns>
    public bool IsOnline(int userId)
    {
        return _onlineUsers.ContainsKey(userId);
    }
}
