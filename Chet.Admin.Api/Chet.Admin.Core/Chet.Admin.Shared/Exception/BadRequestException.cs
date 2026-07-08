namespace Chet.Admin.Shared
{
    /// <summary>
    /// 自定义异常类，用于表示HTTP 400错误请求
    /// </summary>
    public class BadRequestException : Exception
    {
        /// <summary>
        /// 使用指定的错误消息初始化 <see cref="BadRequestException"/> 类的新实例
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        public BadRequestException(string message)
            : base(message)
        {
        }
    }
}
