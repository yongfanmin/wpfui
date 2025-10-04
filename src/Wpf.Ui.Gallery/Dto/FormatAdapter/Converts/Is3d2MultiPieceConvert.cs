// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wpf.Ui.Gallery.Dto.FormatAdapter.Converts;


public class Is3d2MultiPieceConvert: JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 根据 JSON 值的类型进行判断
        switch (reader.TokenType)
        {
            // Case 1: 如果 JSON 值是一个数字
            case JsonTokenType.Number:
                // 尝试读取为整数，只有当这个整数完全等于 1 时才返回 true
                // 如果是 1.0, 2, 0 等其他数字，结果都将是 false
                return reader.TryGetInt32(out int intValue) && intValue == 1;

            // Case 2: 如果 JSON 值是一个字符串
            case JsonTokenType.String:
                // 只有当这个字符串完全等于 "1" 时才返回 true
                // " 1", "true", "1.0" 等其他字符串，结果都将是 false
                return reader.GetString() == "1";

            // Default Case: 对于所有其他 JSON 类型 (true, false, null, object, array 等)
            // 都严格地返回 false，以满足“其他值为false”的要求。
            default:
                return false;
        }
    }

    /// <summary>
    /// 将 C# 的 bool 值写入 JSON。
    /// </summary>
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        // 为了与输入源的格式保持一致，我们将 bool 值序列化为数字 1 或 0
        writer.WriteNumberValue(value ? 1 : 0);
    }
}