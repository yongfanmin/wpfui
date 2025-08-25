// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wpf.Ui.Gallery.Converters;

public class StringOrNumberToIntConverter: JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            // 如果是字符串，则尝试解析
            string? stringValue = reader.GetString();
            if (int.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            // 如果本身就是数字，直接读取
            return reader.GetInt32();
        }

        // 如果类型不匹配且无法转换，可以返回默认值0或抛出异常
        // 在此返回0通常更安全，能避免整个反序列化过程失败
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        // 写入时，总是写入标准的数字类型
        writer.WriteNumberValue(value);
    }
}