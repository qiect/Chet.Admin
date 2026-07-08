namespace Chet.Admin.Contracts.Security
{
    /// <summary>
    /// 密码哈希和验证服务接口
    /// </summary>
    public interface IPasswordService
    {
        /// <summary>
        /// 对密码进行哈希处理
        /// </summary>
        /// <param name="password">原始密码</param>
        /// <returns>哈希后的密码字符串</returns>
        string Hash(string password);

        /// <summary>
        /// 验证密码是否匹配哈希值
        /// </summary>
        /// <param name="password">待验证的原始密码</param>
        /// <param name="hash">已存储的密码哈希值</param>
        /// <returns>如果密码匹配则返回true，否则返回false</returns>
        bool Verify(string password, string hash);
    }
}
