using Chet.Admin.Caching;
using Chet.Admin.Configuration;
using Chet.Admin.Contracts.Cache;
using StackExchange.Redis;

namespace Chet.Admin.Api.Configurations;

/// <summary>
/// Redis缓存配置类
/// </summary>
public static class RedisConfiguration
{
    /// <summary>
    /// 配置Redis缓存
    /// </summary>
    /// <param name="services">IServiceCollection实例</param>
    /// <param name="appSettings">应用程序配置实例</param>
    public static void ConfigureRedis(this IServiceCollection services, AppSettings appSettings)
    {
        if (appSettings?.Redis != null && appSettings.Redis.Enabled)
        {
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddScoped<ICacheService, NoOpCacheService>();
        }

        // 注册Redis连接服务，根据配置决定是否启用
        if (appSettings?.Redis != null && appSettings.Redis.Enabled)
        {
            var redisConnectionString = appSettings.Redis.ConnectionString ?? "localhost:6379";
            var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
            configurationOptions.AbortOnConnectFail = false;
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configurationOptions));
        }
    }
}
