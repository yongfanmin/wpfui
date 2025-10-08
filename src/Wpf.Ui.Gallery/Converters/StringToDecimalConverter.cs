// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wpf.Ui.Gallery.Converters;


public class StringToDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 检查当前JSON token的类型
        if (reader.TokenType == JsonTokenType.String)
        {
            // 如果是字符串，则尝试解析
            string stringValue = reader.GetString();
            if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
            {
                return value;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            // 如果本身就是数字，直接读取
            return reader.GetDecimal();
        }

        // 如果都不是，或解析失败，抛出异常
        throw new JsonException($"Unable to convert token type {reader.TokenType} to a Decimal.");
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        // 写入时，总是写入标准的数字类型
        writer.WriteNumberValue(value);
    }
}