namespace Chet.Admin.Shared
{
    /// <summary>
    /// 自定义异常类，用于表示HTTP 404资源未找到错误
    /// </summary>
    public class NotFoundException : Exception
    {
        /// <summary>
        /// 使用实体名称和ID初始化 <see cref="NotFoundException"/> 类的新实例
        /// </summary>
        /// <param name="entityName">实体类型名称</param>
        /// <param name="id">实体ID</param>
        public NotFoundException(string entityName, int id)
            : base($"{entityName} with ID {id} not found.")
        {
            EntityName = entityName;
            Id = id;
        }

        /// <summary>
        /// 获取或设置实体类型名称
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// 获取或设置未找到的实体ID
        /// </summary>
        public int Id { get; set; }
    }
}
