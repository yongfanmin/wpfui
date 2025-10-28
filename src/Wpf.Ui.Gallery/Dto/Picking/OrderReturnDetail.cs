// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;

namespace Wpf.Ui.Gallery.Dto.Picking;

public class OrderReturnDetail
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; }
    
    [JsonPropertyName("order_no")]
    public string OrderNo { get; set; }
    
    [JsonPropertyName("order_code")]
    public string OrderCode { get; set; }
    
    [JsonPropertyName("buy_type_count")]
    public int buyTypeCount { get; set; }
    
    [JsonPropertyName("buy_number_count")]
    public int BuyNumberCount { get; set; }
    
    [JsonPropertyName("paytime")]
    public string PayTime { get; set; }
    
    [JsonPropertyName("express_id")]
    public int ExpressId { get; set; }
    
    [JsonPropertyName("express_number")]
    public string ExpressNumber { get; set; }
    
    [JsonPropertyName("remarks")]
    public string Remarks { get; set; }
    
    [JsonPropertyName("status")]
    public int Status { get; set; }
}