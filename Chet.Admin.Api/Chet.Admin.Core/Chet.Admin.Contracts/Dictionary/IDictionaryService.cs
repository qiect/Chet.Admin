using Chet.Admin.DTOs.Dictionary;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Dictionary
{
    /// <summary>
    /// 字典服务接口
    /// </summary>
    public interface IDictionaryService
    {
        /// <summary>
        /// 根据ID获取字典信息
        /// </summary>
        /// <param name="id">字典ID</param>
        /// <returns>字典DTO</returns>
        Task<DictionaryDto> GetDictionaryByIdAsync(int id);

        /// <summary>
        /// 获取所有字典
        /// </summary>
        /// <returns>字典DTO集合</returns>
        Task<IEnumerable<DictionaryDto>> GetAllDictionariesAsync();

        /// <summary>
        /// 分页获取字典列表
        /// </summary>
        /// <param name="request">分页请求参数</param>
        /// <returns>字典分页结果</returns>
        Task<PagedResult<DictionaryDto>> GetPagedDictionariesAsync(PagedRequest request);

        /// <summary>
        /// 根据字典类型获取字典列表
        /// </summary>
        /// <param name="dictType">字典类型</param>
        /// <returns>字典DTO集合</returns>
        Task<IEnumerable<DictionaryDto>> GetByDictTypeAsync(string dictType);

        /// <summary>
        /// 创建字典
        /// </summary>
        /// <param name="dto">字典创建DTO</param>
        /// <returns>创建的字典DTO</returns>
        Task<DictionaryDto> CreateDictionaryAsync(DictionaryCreateDto dto);

        /// <summary>
        /// 更新字典信息
        /// </summary>
        /// <param name="id">字典ID</param>
        /// <param name="dto">字典更新DTO</param>
        Task UpdateDictionaryAsync(int id, DictionaryUpdateDto dto);

        /// <summary>
        /// 删除字典
        /// </summary>
        /// <param name="id">字典ID</param>
        Task DeleteDictionaryAsync(int id);

        /// <summary>
        /// 根据字典编码获取字典项列表
        /// </summary>
        /// <param name="code">字典编码</param>
        /// <returns>字典项DTO列表</returns>
        Task<List<DictionaryItemDto>> GetItemsByCodeAsync(string code);
    }
}
