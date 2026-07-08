using Chet.Admin.DTOs.User;

namespace Chet.Admin.Contracts.User;

public interface IOnlineUserService
{
    void UserOnline(int userId, string userName, string clientIp);
    void UserOffline(int userId);
    void UpdateActivity(int userId);
    List<OnlineUserDto> GetOnlineUsers();
    bool IsOnline(int userId);
}
