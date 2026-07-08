using Chet.Admin.Contracts.User;
using Chet.Admin.DTOs.User;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Chet.Admin.Services.User;

/// <summary>
/// 在线用户服务实现
/// </summary>
/// <remarks>
/// 维护两份内存数据：
/// 1. <see cref="_onlineUsers"/>：当前在线用户列表（用于在线用户管理页面展示）
/// 2. <see cref="_revokedUsers"/>：令牌黑名单（用于强制下线功能，记录被吊销的用户ID及吊销时间）
/// 黑名单机制：当用户被强制下线时，将其加入黑名单。JWT验证时检查令牌签发时间是否早于吊销时间，
/// 若是则拒绝请求；用户重新登录后获得的新令牌签发时间晚于吊销时间，可正常使用。
/// </remarks>
public class OnlineUserService : IOnlineUserService
{
    private readonly ILogger<OnlineUserService> _logger;
    private static readonly ConcurrentDictionary<int, OnlineUserDto> _onlineUsers = new();

    /// <summary>
    /// 令牌黑名单：Key=用户ID，Value=吊销时间（UTC）
    /// </summary>
    private static readonly ConcurrentDictionary<int, DateTime> _revokedUsers = new();

    /// <summary>
    /// 黑名单条目保留时长（超过此时间的条目将被清理，因为对应令牌早已过期）
    /// </summary>
    private static readonly TimeSpan _blacklistRetention = TimeSpan.FromHours(2);

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

        // 用户重新上线时清除可能存在的历史黑名单记录
        RemoveFromBlacklist(userId);
    }

    /// <summary>
    /// 标记用户下线（仅移除在线记录，不使令牌失效）
    /// </summary>
    /// <param name="userId">用户ID</param>
    public void UserOffline(int userId)
    {
        _onlineUsers.TryRemove(userId, out _);
    }

    /// <summary>
    /// 强制用户下线（移除在线记录并加入令牌黑名单，使已签发的JWT失效）
    /// </summary>
    /// <param name="userId">用户ID</param>
    public void ForceOffline(int userId)
    {
        // 移除在线记录
        _onlineUsers.TryRemove(userId, out _);

        // 加入令牌黑名单，记录吊销时间
        _revokedUsers.AddOrUpdate(userId, DateTime.UtcNow, (_, _) => DateTime.UtcNow);

        _logger.LogInformation("User {UserId} has been forced offline and added to token blacklist", userId);
    }

    /// <summary>
    /// 检查指定用户的令牌是否已被吊销
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="tokenIssuedAt">令牌签发时间（UTC）</param>
    /// <returns>已吊销返回true，否则返回false</returns>
    public bool IsTokenRevoked(int userId, DateTime tokenIssuedAt)
    {
        CleanupExpiredBlacklist();

        if (!_revokedUsers.TryGetValue(userId, out var revokedAt))
        {
            return false;
        }

        // 令牌签发时间早于吊销时间 → 令牌已被吊销
        // 比较前统一转换为UTC，避免本地时间/UTC时间混用导致判断错误
        var issuedUtc = tokenIssuedAt.Kind == DateTimeKind.Utc
            ? tokenIssuedAt
            : tokenIssuedAt.ToUniversalTime();

        return issuedUtc < revokedAt;
    }

    /// <summary>
    /// 从黑名单中移除用户（用户重新登录时调用，清除历史吊销记录）
    /// </summary>
    /// <param name="userId">用户ID</param>
    public void RemoveFromBlacklist(int userId)
    {
        _revokedUsers.TryRemove(userId, out _);
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

    /// <summary>
    /// 清理过期的黑名单条目（吊销时间超过保留时长的条目）
    /// </summary>
    private void CleanupExpiredBlacklist()
    {
        var cutoff = DateTime.UtcNow.Subtract(_blacklistRetention);
        foreach (var kvp in _revokedUsers)
        {
            if (kvp.Value < cutoff)
            {
                _revokedUsers.TryRemove(kvp.Key, out _);
            }
        }
    }
}
