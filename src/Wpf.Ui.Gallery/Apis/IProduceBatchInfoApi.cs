// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;
using Refit;
using Wpf.Ui.Gallery.Dto;

namespace Wpf.Ui.Gallery.Apis;

public class ProduceBatchInfoRequest
{
    // TODO 当前参数由软件传参 后续改成只需要传批次号 接口根据批次号获取所有数据
    // num=1&designProductIds={0}&produceBatchNumber={1}
    // num=1&designProductIds=5666,4800
    // Header machineid = 68,405
    [JsonPropertyName("num")]
    public int Num { get; set; }

    [JsonPropertyName("designProductIds")]
    public string? DesignProductIds { get; set; }

    [JsonPropertyName("produceBatchNumber")]
    public string? ProduceBatchNumber { get; set; }
}

public interface IProduceBatchInfoApi
{
    [Post("/api/factoryInterface/getWaitOrderProduceListSmt")]
    Task<FactoryApiResponse<List<ProductBatchInfo>>> getProduceBatchInfo(
        [Body] ProduceBatchInfoRequest request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token,
        // TODO 非标准写法
        [Header("machineid")] string machineid
    );
    
    [Post("/api/factoryInterface/getWaitOrderProduceListSmt")]
    Task<Object> getProduceBatchInfoObj(
        [Body] ProduceBatchInfoRequest request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token,
        // TODO 非标准写法
        [Header("machineid")] string machineid
    );
}