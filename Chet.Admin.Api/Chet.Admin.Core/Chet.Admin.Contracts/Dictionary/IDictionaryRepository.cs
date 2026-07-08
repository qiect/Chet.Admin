using Chet.Admin.Contracts;
using Chet.Admin.Domain.Dictionary;

namespace Chet.Admin.Contracts.Dictionary
{
    /// <summary>
    /// 字典仓储接口
    /// </summary>
    public interface IDictionaryRepository : IRepository<DictionaryEntity>
    {
        /// <summary>
        /// 根据字典类型获取字典项列表
        /// </summary>
        /// <param name="dictType">字典类型</param>
        /// <returns>字典实体集合</returns>
        Task<IEnumerable<DictionaryEntity>> GetByDictTypeAsync(string dictType);
    }
}
