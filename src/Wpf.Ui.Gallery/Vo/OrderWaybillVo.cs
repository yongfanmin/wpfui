// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;

namespace Wpf.Ui.Gallery.Vo;

public class OrderWaybillVo
{
    [JsonPropertyName("factory_id")] public int FactoryId { get; set; }
    [JsonPropertyName("express_pdf_url")] public string ExpressWaybillUrl { get; set; }
    [JsonPropertyName("order_id")] public string OrderId { get; set; }
    [JsonPropertyName("order_no")] public string OrderNo { get; set; }
    [JsonPropertyName("order_code")] public string OrderCode { get; set; }
    [JsonPropertyName("express_id")] public int ExpressId { get; set; }
    [JsonPropertyName("express_number")] public string ExpressNum { get; set; }
    [JsonPropertyName("express_company_id")] public int ExpressCompanyId { get; set; }
    [JsonPropertyName("express_name")] public string ExpreeName { get; set; }
}