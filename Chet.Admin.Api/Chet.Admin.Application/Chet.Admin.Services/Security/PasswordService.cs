using Chet.Admin.Contracts.Security;

namespace Chet.Admin.Services.Security;

/// <summary>
/// 密码服务实现类（Password Service Implementation）
/// <para>
/// 使用BCrypt算法进行密码的哈希处理和验证。
/// BCrypt是一种专为密码存储设计的自适应哈希函数，
/// 内置盐值（salt）机制，能有效抵御彩虹表攻击和暴力破解。
 /// </para>
/// </summary>
/// <remarks>
/// <para>为什么选择BCrypt？</para>
/// <list type="table">
///   <listheader>
        ///     <term>特性</term>
///     <description>说明</description>
    ///   </listheader>
///   <item>
///     <term>自适应工作因子</term>
///     <description>可调整计算成本，随硬件性能提升而增加，保持安全性</description>
///   </item>
///   <item>
///     <term>内置盐值</term>
///     <description>每次哈希自动生成唯一盐值，无需单独管理</description>
///   </item>
///   <item>
///     <term>抗GPU破解</term>
///     <description>算法设计使其难以使用GPU进行大规模并行计算</description>
///   </item>
///   <item>
///     <term>行业标准</term>
///     <description>被广泛采用于OpenBSD、Linux、各类Web框架</description>
///   </item>
/// </list>
/// 
/// <para>工作因子（Work Factor）说明：</para>
/// <para>
/// 当前设置为12（2^12 = 4096轮迭代）。
/// 在现代CPU上约需250ms完成一次哈希。
/// 建议每年评估是否需要提高此值。
/// </para>
/// 
/// <para>安全最佳实践：</para>
/// <list type="bullet">
///   <item><description>永远不要明文存储或记录密码</description></item>
///   <item><description>哈希前不要对密码进行裁剪或截断</description></item>
///   <item><description>使用恒定时间比较来验证密码（BCrypt已内置）</description></item>
///   <item><description>考虑在BCrypt基础上添加HMAC二次加密</description></item>
/// </list>
/// 
/// <para>使用示例：</para>
/// <code>
/// // 注册时：哈希密码
/// string hash = _passwordService.Hash("MySecure@123");
/// // 存储 hash 到数据库
/// 
/// // 登录时：验证密码
/// bool isValid = _passwordService.Verify("MySecure@123", storedHash);
/// if (isValid) { /* 登录成功 */ }
/// </code>
/// </remarks>
public class PasswordService : IPasswordService
{
    /// <summary>
    /// BCrypt工作因子（Work Factor）
    /// <para>
    /// 控制哈希计算的迭代次数，值为2的幂次方。
    /// 值越大越安全但计算越慢：
    /// <list type="bullet">
    ///   <item><description>10: ~100ms（2010年推荐）</description></item>
    ///   <item><description>11: ~200ms</description></item>
    ///   <item><description>12: ~250ms（当前设置，2024年推荐）</description></item>
    ///   <item><description>13: ~500ms</description></item>
    ///   <item><description>14: ~1000ms</description></item>
    /// </list>
    /// </para>
    /// </summary>
    private const int WorkFactor = 12;

    /// <summary>
    /// 对明文密码进行BCrypt哈希处理
    /// <para>
    /// 生成包含盐值的密码哈希字符串，格式为 $2a$XX$...
    /// 每次调用都会生成不同的哈希值（因为随机盐值），
    /// 但相同的密码可以通过Verify方法验证通过。
    /// </para>
    /// </summary>
    /// <param name="password">要哈希的明文密码</param>
    /// <returns>
    /// BCrypt哈希字符串（60个字符），可直接存储到数据库
    /// </returns>
    /// <exception cref="ArgumentException">
    /// 当密码为空、空白或长度不足6个字符时抛出
    /// </exception>
    /// <remarks>
    /// <para>输出格式：</para>
    /// <code>$2a$12$abcdefghijklmnopqrstuvABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789</code>
    /// 
    /// <para>各部分含义：</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>部分</term>
    ///     <description>说明</description>
    ///   </listheader>
    ///   <item>
    ///     <term>$2a$</term>
    ///     <description>BCrypt版本标识符</description>
    ///   </item>
    ///   <item>
    ///     <term>12</term>
    ///     <description>工作因子（两位数字）</description>
    ///   </item>
    ///   <item>
    ///     <term>$...$...</term>
    ///     <description>Base64编码的盐值 + 哈希结果</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        if (password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters", nameof(password));

        return global::BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// 验证明文密码与BCrypt哈希是否匹配
    /// <para>
    /// 使用恒定时间比较算法防止时序攻击（Timing Attack）。
    /// 即使密码错误，验证耗时也基本相同。
    /// </para>
    /// </summary>
    /// <param name="password">用户输入的明文密码</param>
    /// <param name="hash">数据库中存储的BCrypt哈希值</param>
    /// <returns>
    /// 如果密码匹配返回true；否则返回false
    /// </returns>
    /// <remarks>
    /// <para>安全特性：</para>
    /// <list type="bullet">
    ///   <item><description>自动从hash中提取盐值</description></item>
    ///   <item><description>恒定时间比较，防时序攻击</description></item>
    ///   <item><description>异常不会泄露任何信息</description></item>
    /// </list>
    /// 
    /// <para>异常处理：</para>
    /// <para>
    /// 任何异常都会返回false而不是抛出，
    /// 防止攻击者通过异常行为推断信息。
    /// </para>
    /// </remarks>
    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return global::BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
