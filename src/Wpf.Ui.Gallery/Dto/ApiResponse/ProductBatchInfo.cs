// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;
using Wpf.Ui.Gallery.Converters;

namespace Wpf.Ui.Gallery.Dto;

public class ProductBatchInfo
{
    /*{
        "id": 12259,
        "produce_batch_number": "S2508190802",
        "batch_no": 2581942540001,
        "status": 2,
        "design_product_id": 5491,
        "device_id": 68,
        "is_virtual": -1
    }*/
    [JsonPropertyName("id")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long Id { get; set; }

    [JsonPropertyName("produce_batch_number")]
    public string ProduceBatchNumber { get; set; }

    [JsonPropertyName("batch_no")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long BatchNum { get; set; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(StringOrNumberToIntConverter))]
    public int Status { get; set; }
    
    [JsonPropertyName("design_product_id")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long DesignProductId { get; set; }
    
    [JsonPropertyName("device_id")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long DeviceId { get; set; }
    
    [JsonPropertyName("is_virtual")]
    [JsonConverter(typeof(StringOrNumberToIntConverter))]
    public int IsVirtual { get; set; }
}