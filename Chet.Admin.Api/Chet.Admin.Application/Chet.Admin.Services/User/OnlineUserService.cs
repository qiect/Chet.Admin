using Chet.Admin.Contracts.User;
using Chet.Admin.DTOs.User;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Chet.Admin.Services.User;

public class OnlineUserService : IOnlineUserService
{
    private readonly ILogger<OnlineUserService> _logger;
    private static readonly ConcurrentDictionary<int, OnlineUserDto> _onlineUsers = new();

    public OnlineUserService(ILogger<OnlineUserService> logger)
    {
        _logger = logger;
    }

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

    public void UserOffline(int userId)
    {
        _onlineUsers.TryRemove(userId, out _);
    }

    public void UpdateActivity(int userId)
    {
        if (_onlineUsers.TryGetValue(userId, out var info))
        {
            info.LastActiveTime = DateTime.UtcNow;
        }
    }

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

    public bool IsOnline(int userId)
    {
        return _onlineUsers.ContainsKey(userId);
    }
}
