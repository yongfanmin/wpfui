// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;
using Refit;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.FormatAdapter;

namespace Wpf.Ui.Gallery.Apis;

public class ProduceBatchDetailRequest
{
    //[JsonPropertyName("batch_no")]
    [JsonPropertyName("batchNo")]
    public long BatchNo { get; set; }
}

public interface IProduceBatchDetailApi
{
    [Post("/api/factoryInterface/getOrderProduceInfoByBatchNo")]
    Task<FactoryApiResponse<List<OrderPrintBatch>>> getProduceBatchDetail(
        [Body] ProduceBatchDetailRequest request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token,
        // TODO 非标准写法
        [Header("machineid")] string machineid
    );
    
    [Post("/api/factoryInterface/getOrderProduceInfoByBatchNo")]
    Task<FactoryApiResponse<object>> getProduceBatchDetailObjTest(
        [Body] ProduceBatchDetailRequest request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token,
        // TODO 非标准写法
        [Header("machineid")] string machineid
    );
}