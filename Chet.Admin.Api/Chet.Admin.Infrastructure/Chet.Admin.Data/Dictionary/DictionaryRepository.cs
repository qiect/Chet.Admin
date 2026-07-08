using Chet.Admin.Contracts.Dictionary;
using Chet.Admin.Domain.Dictionary;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data.Dictionary;

public class DictionaryRepository : EfCoreRepository<DictionaryEntity>, IDictionaryRepository
{
    public DictionaryRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<DictionaryEntity>> GetByDictTypeAsync(string dictType)
    {
        return await _dbContext.Dictionaries
            .Where(d => d.DictType == dictType)
            .OrderBy(d => d.Sort)
            .ToListAsync();
    }
}
