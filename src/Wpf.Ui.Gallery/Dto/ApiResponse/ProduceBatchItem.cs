// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto;

using System.Text.Json.Serialization;

public class ProduceBatchItem
{
    [JsonPropertyName("num_total")]
    public int NumTotal { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("produce_batch_number")]
    public string ProduceBatchNumber { get; set; }

    [JsonPropertyName("produce_batch_number_total")]
    public int ProduceBatchNumberTotal { get; set; }
}