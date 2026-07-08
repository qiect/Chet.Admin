using Chet.Admin.Contracts.Dictionary;
using Chet.Admin.Domain.Dictionary;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data.Dictionary;

/// <summary>
/// 字典仓储实现类
/// </summary>
public class DictionaryRepository : EfCoreRepository<DictionaryEntity>, IDictionaryRepository
{
    /// <summary>
    /// 初始化字典仓储实例
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public DictionaryRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 根据字典类型编码查询字典项列表
    /// </summary>
    /// <param name="dictType">字典类型编码</param>
    /// <returns>字典实体列表（按排序号排序）</returns>
    public async Task<IEnumerable<DictionaryEntity>> GetByDictTypeAsync(string dictType)
    {
        return await _dbContext.Dictionaries
            .Where(d => d.DictType == dictType)
            .OrderBy(d => d.Sort)
            .ToListAsync();
    }
}
