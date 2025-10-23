// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;
using Refit;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Vo;

namespace Wpf.Ui.Gallery.Apis;

public class ProduceBatchRequest
{
    // num=1&designProductIds=5666,4800
    // Header machineid = 68,405
    [JsonPropertyName("num")]
    public int Num { get; set; }

    // [JsonPropertyName("designProductIds")]
    // public string? DesignProductIds { get; set; }

    //JsonPropertyName("machineid")]
    //public string? MachineId { get; set; }
}


public class BatchNo2Produce
{
    [JsonPropertyName("batchNo")]
    public string BatchNo { get; set; }
}

public class ProduceBatchNum2Produce
{
    [JsonPropertyName("produceBatchNum")]
    public string ProduceBatchNum { get; set; }
}

public class BatchNo2ProduceComplete
{
    [JsonPropertyName("batchNo")]
    public string BatchNo { get; set; }
}

public class ProduceBatchNo2ProduceComplete
{
    [JsonPropertyName("produceBatchNum")]
    public string ProduceBatchNum { get; set; }
}


public interface IProduceBatchApi
{
    [Post("/api/v2/factoryInterface/getProduceList")]
    Task<FactoryApiResponse<List<ProduceBatchInfo>>> getProduceBatchList(
        [Body] ProduceBatchRequest request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token
        );
    
    // 后端接口出现歧义 实际是对 item_id 设置成已生产 而不是batch_no
    [Post("/api/v2/factoryInterface/setOrderProduceBatchNoCreating")]
    Task<FactoryApiResponse<Object>> setBatchNo2Produce(
        [Body] BatchNo2Produce request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token
    );
    
    [Post("/api/v2/factoryInterface/setOrderProduceProduceBatchNumCreating")]
    Task<FactoryApiResponse<Object>> setProduceBatchNum2Produce(
        [Body] ProduceBatchNum2Produce request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token
    );
    
    
    [Post("/api/factoryInterface/setOrderProduceCompleteByBatchNo")]
    Task<FactoryApiResponse<Object>> setBatchNo2ProduceComplete(
        [Body] BatchNo2ProduceComplete request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token
    );
    
    
    [Post("/api/v2/factoryInterface/setOrderProduceCompleteByProduceBatchNum")]
    Task<FactoryApiResponse<Object>> setProduceBatchNum2Complete(
        [Body] ProduceBatchNo2ProduceComplete request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token
    );
    
}