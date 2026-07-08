namespace Chet.Admin.DTOs.Dictionary;

/// <summary>
/// 字典项DTO
/// </summary>
public class DictionaryItemDto
{
    /// <summary>
    /// 字典值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 字典标签
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
