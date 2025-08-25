// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;
using Refit;
using Wpf.Ui.Gallery.Dto;

namespace Wpf.Ui.Gallery.Apis;

public class ProduceBatchRequest
{
    // TODO 当前参数由软件传参 后续直接改成无需传参 工厂后台配置好账号对应得参数配置(抓取哪些公版进行生产?)
    // num=1&designProductIds=5666,4800
    // Header machineid = 68,405
    [JsonPropertyName("num")]
    public int Num { get; set; }

    [JsonPropertyName("designProductIds")]
    public string? DesignProductIds { get; set; }

    //JsonPropertyName("machineid")]
    //public string? MachineId { get; set; }
}

public interface IProduceBatchApi
{
    [Post("/api/factoryInterface/getProduceList")]
    Task<FactoryApiResponse<List<ProduceBatchItem>>> getProduceBatchList(
        [Body] ProduceBatchRequest request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token,
        // TODO 非标准写法
        [Header("machineid")] string machineid
        );
}