// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;

namespace Wpf.Ui.Gallery.Vo;

public class OrderDetailVo
{
    [JsonPropertyName("order_id")] public string OrderId { get; set; }

    [JsonPropertyName("order_no")] public string OrderNo { get; set; }

    [JsonPropertyName("order_code")] public string OrderCode { get; set; }

    [JsonPropertyName("buy_type_count")] public int TypeCount { get; set; }

    [JsonPropertyName("but_number_count")] public int ItemCount { get; set; }

    [JsonPropertyName("paytime")] public string PayTime { get; set; }

    [JsonPropertyName("express_id")] public int ExpressId { get; set; }

    [JsonPropertyName("express_number")] public string ExpressNum { get; set; }

    [JsonPropertyName("remarks")] public string Remarks { get; set; }

    [JsonPropertyName("status")] public int Status { get; set; }
}