// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wpf.Ui.Gallery.Converters;
/// <summary>
/// 一个用于 System.Text.Json 的自定义转换器，
/// 它可以将 JSON 字符串安全地转换为可空的 DateTime (DateTime?)。
/// 如果字符串格式无效、为空或为null，则返回 null，而不是抛出异常。
/// </summary>
///



// !!!未验证方法 似乎不可用
public class StringToDateTimeConverter : JsonConverter<DateTime?>
{
    /// <summary>
    /// 从 JSON 读取并转换字符串到 DateTime?。
    /// </summary>
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 检查当前 JSON token 是否为 null
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // 检查当前 JSON token 是否为字符串
        if (reader.TokenType == JsonTokenType.String)
        {
            string? dateString = reader.GetString();

            // 如果字符串是空的或仅包含空白，也视为 null
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            // 尝试使用灵活的 DateTime.TryParse 进行解析。
            // 它能识别多种格式，包括 "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ", "MM/dd/yyyy" 等。
            // 使用 InvariantCulture 确保解析不受本地化设置的影响。
            // DateTimeStyles.AdjustToUniversal 会将带时区信息的时间转换为UTC时间，非常适合API场景。
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime dateValue))
            {
                return dateValue;
            }
            
            // 如果上述解析失败（例如对于非标准格式），则安全地返回null
            return null;
        }

        // 如果 token 类型不是字符串或 null（例如是数字、布尔值等），
        // 我们也认为它无法转换为 DateTime，返回 null。
        return null;
    }

    /// <summary>
    /// 将 DateTime? 写入为 JSON 字符串。
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            // 使用 "O" (Round-trip) 格式化字符串，这是标准的、包含时区信息的ISO 8601格式。
            // 例如: "2025-11-13T15:30:00.1234567+08:00"
            writer.WriteStringValue(value.Value.ToString("O", CultureInfo.InvariantCulture));
        }
        else
        {
            // 如果 DateTime? 本身就是 null，则写入 JSON null
            writer.WriteNullValue();
        }
    }
}