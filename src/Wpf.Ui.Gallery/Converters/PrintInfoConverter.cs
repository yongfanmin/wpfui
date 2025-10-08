// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Wpf.Ui.Gallery.Dto.FormatAdapter;

namespace Wpf.Ui.Gallery.Converters;

public class PrintInfoConverter : JsonConverter<Dictionary<string, PrintInfo?>>
{
    public override Dictionary<string, PrintInfo?>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        // 确保我们正在处理一个对象
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return null;
        }

        // --- [核心的智能判断逻辑在这里] ---

        // 1. 我们不能直接反序列化，因为不知道是哪种结构。
        //    所以，我们先将整个JSON对象，读入一个灵活的 JsonNode 中。
        var jsonNode = JsonNode.Parse(ref reader);
        if (jsonNode is not JsonObject jsonObject)
        {
            return null; // 不是有效的JSON对象
        }

        // 2. “试探性”地检查是否存在 'dpi' 或 'view_id' 这样的、
        //    只属于PrintInfo对象自身的字段。
        if (jsonObject.ContainsKey("dpi") || jsonObject.ContainsKey("view_id"))
        {
            // --- 情况 A: 这是特殊结构 (单个PrintInfo对象) ---

            // a. 尝试将整个节点，直接反序列化为一个 PrintInfo 对象
            try
            {
                var printInfo = jsonObject.Deserialize<PrintInfo>(options);
                if (printInfo != null)
                {
                    // b. 手动构建我们期望的字典结构
                    //    使用 printInfo 内部的 view_id 作为键
                    // 这个键值似乎跟view_id无关 接口乱写 然后随便返回一个view_id? 局部印这个位置view_id只有1  但是打印位置的view_id 有多个 对不上
                    // string key = printInfo.ViewId > 0 ? printInfo.ViewId.ToString() : "0";
                    return new Dictionary<string, PrintInfo?> { { "0" , printInfo } };
                }
            }
            catch (JsonException)
            {
                /* 反序列化失败，按错误处理 */
            }
        }
        else
        {
            // --- 情况 B: 这是标准结构 (一个包裹着PrintInfo对象的字典) ---

            // a. 既然它不是单个PrintInfo，那它就应该是我们期望的字典结构。
            //    现在我们可以安全地、直接地将其反序列化为字典。
            try
            {
                return jsonObject.Deserialize<Dictionary<string, PrintInfo?>>(options);
            }
            catch (JsonException)
            {
                /* 反序列化失败，按错误处理 */
            }
        }

        // 如果所有逻辑都失败了，返回一个空的字典，保证程序的健壮性
        return new Dictionary<string, PrintInfo?>();
    }

    public override void Write(
        Utf8JsonWriter writer,
        Dictionary<string, PrintInfo?> value,
        JsonSerializerOptions options)
    {
        // 写入时，总是写入标准的字典格式
        JsonSerializer.Serialize(writer, value, options);
    }
}