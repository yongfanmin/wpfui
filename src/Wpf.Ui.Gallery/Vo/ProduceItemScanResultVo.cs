// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Converters;

namespace Wpf.Ui.Gallery.Vo;

public class ProduceItemScanResultVo
{
    [JsonPropertyName("produceBatchNum")]
    public string ProduceBatchNum { get; set; }
    
    [JsonPropertyName("batchNo")]
    public long BatchNo { get; set; }
    
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; }

    [JsonPropertyName("orderProduceStatus")]
    [JsonConverter(typeof(OrderProduceStatusConvert))]
    public OrderProduceStatus OrderProduceStatus { get; set; }
}