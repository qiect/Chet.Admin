using Chet.Admin.DTOs.Dictionary;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Dictionary
{
    public interface IDictionaryService
    {
        Task<DictionaryDto> GetDictionaryByIdAsync(int id);
        Task<IEnumerable<DictionaryDto>> GetAllDictionariesAsync();
        Task<PagedResult<DictionaryDto>> GetPagedDictionariesAsync(PagedRequest request);
        Task<IEnumerable<DictionaryDto>> GetByDictTypeAsync(string dictType);
        Task<DictionaryDto> CreateDictionaryAsync(DictionaryCreateDto dto);
        Task UpdateDictionaryAsync(int id, DictionaryUpdateDto dto);
        Task DeleteDictionaryAsync(int id);
        Task<List<DictionaryItemDto>> GetItemsByCodeAsync(string code);
    }
}
