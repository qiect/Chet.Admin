namespace Chet.Admin.DTOs.User;

public class OnlineUserDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public DateTime LastActiveTime { get; set; }
}
