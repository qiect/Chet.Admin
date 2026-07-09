using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chet.Admin.Shared.Api;

/// <summary>
/// DateTime JSON 转换器：统一把 DateTime 当作 UTC 输出，确保序列化结果带 "Z" 后缀。
/// <para>
/// 背景：SQLite 存储的 DateTime 读回时 Kind=Unspecified，System.Text.Json 默认序列化时不带时区标识，
/// 前端 new Date(str) 会按本地时区解析导致时间显示错误。本转换器把所有 DateTime 视为 UTC，
/// 输出 ISO 8601 带 Z 的格式（如 "2026-07-09T15:03:47Z"），前端可正确转换为本地时区显示。
/// </para>
/// </summary>
public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        // 输入带 Z 或时区偏移的按 UTC 解析；裸时间字符串视为 UTC
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // 统一输出为 UTC 带 Z 后缀
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        writer.WriteStringValue(utc);
    }
}
