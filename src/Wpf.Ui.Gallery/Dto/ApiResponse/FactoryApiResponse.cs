// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;

namespace Wpf.Ui.Gallery.Dto;

public class FactoryApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    // 直接使用泛型 因为无法预测后端到底会返回什么结构 使用Object接收又无法解析里面的字段变量
    [JsonPropertyName("data")]
    public T Data { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    public bool IsSuccess => Code == 1;
}