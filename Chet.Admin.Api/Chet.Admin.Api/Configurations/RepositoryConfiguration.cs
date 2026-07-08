using Chet.Admin.Contracts;
using Chet.Admin.Contracts.User;
using Chet.Admin.Contracts.Role;
using Chet.Admin.Contracts.Permission;
using Chet.Admin.Contracts.Menu;
using Chet.Admin.Contracts.Department;
using Chet.Admin.Contracts.Dictionary;
using Chet.Admin.Data;
using Chet.Admin.Data.User;
using Chet.Admin.Data.Role;
using Chet.Admin.Data.Permission;
using Chet.Admin.Data.Menu;
using Chet.Admin.Data.Department;
using Chet.Admin.Data.Dictionary;

namespace Chet.Admin.Api.Configurations;

/// <summary>
/// 仓储服务配置类
/// </summary>
public static class RepositoryConfiguration
{
    /// <summary>
    /// 配置仓储服务
    /// </summary>
    /// <param name="services">IServiceCollection实例</param>
    public static void ConfigureRepositories(this IServiceCollection services)
    {
        // 注册仓储服务
        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDictionaryRepository, DictionaryRepository>();
    }
}
