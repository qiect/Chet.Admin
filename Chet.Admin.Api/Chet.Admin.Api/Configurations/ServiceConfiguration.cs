using Chet.Admin.Contracts;
using Chet.Admin.Contracts.Auth;
using Chet.Admin.Contracts.Jwt;
using Chet.Admin.Contracts.Security;
using Chet.Admin.Contracts.User;
using Chet.Admin.Contracts.Role;
using Chet.Admin.Contracts.Menu;
using Chet.Admin.Contracts.Department;
using Chet.Admin.Contracts.Dictionary;
using Chet.Admin.Contracts.Audit;
using Chet.Admin.Contracts.Dashboard;
using Chet.Admin.Contracts.Notification;
using Chet.Admin.Contracts.File;
using Chet.Admin.Services.Auth;
using Chet.Admin.Services.Jwt;
using Chet.Admin.Services.Security;
using Chet.Admin.Services.User;
using Chet.Admin.Services.Role;
using Chet.Admin.Services.Menu;
using Chet.Admin.Services.Department;
using Chet.Admin.Services.Dictionary;
using Chet.Admin.Services.Audit;
using Chet.Admin.Services.Dashboard;
using Chet.Admin.Services.Notification;
using Chet.Admin.Services.File;

namespace Chet.Admin.Api.Configurations;

/// <summary>
/// 业务逻辑服务配置类
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// 配置业务逻辑服务
    /// </summary>
    /// <param name="services">IServiceCollection实例</param>
    public static void ConfigureServices(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, Data.UnitOfWork>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IDataScopeService, DataScopeService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IDictionaryService, DictionaryService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFileService, FileService>();
        services.AddSingleton<IOnlineUserService, OnlineUserService>();
        services.AddSingleton<CaptchaService>();
    }
}
