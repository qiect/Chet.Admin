using Chet.Admin.Contracts;
using Chet.Admin.Domain.Dictionary;

namespace Chet.Admin.Contracts.Dictionary
{
    public interface IDictionaryRepository : IRepository<DictionaryEntity>
    {
        Task<IEnumerable<DictionaryEntity>> GetByDictTypeAsync(string dictType);
    }
}
