// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Converters;


public class OrderProduceStatusConvert : JsonConverter<OrderProduceStatus>
{
    public override OrderProduceStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 1. 确保 JSON 中的值是一个数字
        if (reader.TokenType != JsonTokenType.Number)
        {
            // 如果不是数字（例如是字符串 "5"），您可以选择增加兼容性或直接抛出异常
            // 这里我们选择抛出异常，因为输入 JSON 格式是固定的
            throw new JsonException($"期望一个数字来反序列化 OrderProduceStatus，但收到了 {reader.TokenType}.");
        }

        // 2. 读取整数值
        int intValue = reader.GetInt32();

        // 3. 将整数值强制转换为 OrderProduceStatus 枚举
        //    即使整数值在枚举中未定义（例如 99），这个转换也会成功。
        //    如果需要严格验证，可以使用 Enum.IsDefined()。
        return (OrderProduceStatus)intValue;
    }

    public override void Write(Utf8JsonWriter writer, OrderProduceStatus value, JsonSerializerOptions options)
    {
        // 1. 将 OrderProduceStatus 枚举转换为其底层的整数值
        int intValue = (int)value;
        
        // 2. 将这个整数值写入 JSON
        writer.WriteNumberValue(intValue);
    }
}