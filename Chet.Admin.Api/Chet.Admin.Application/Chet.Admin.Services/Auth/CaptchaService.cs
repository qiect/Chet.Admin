using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Auth;

public class CaptchaService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CaptchaService> _logger;
    private static readonly Random _random = new();
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";

    public CaptchaService(IMemoryCache cache, ILogger<CaptchaService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// 生成验证码
    /// </summary>
    public (string Id, string Code) Generate()
    {
        var id = Guid.NewGuid().ToString("N");
        var code = new string(Enumerable.Range(0, 4).Select(_ => Chars[_random.Next(Chars.Length)]).ToArray());
        _cache.Set($"captcha:{id}", code, TimeSpan.FromMinutes(5));
        return (id, code);
    }

    /// <summary>
    /// 验证验证码
    /// </summary>
    public bool Validate(string id, string code)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(code)) return false;

        var cacheKey = $"captcha:{id}";
        if (_cache.TryGetValue(cacheKey, out string? cachedCode))
        {
            _cache.Remove(cacheKey); // 验证码使用后即失效
            return string.Equals(cachedCode, code, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
