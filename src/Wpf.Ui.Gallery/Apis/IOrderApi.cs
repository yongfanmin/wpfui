// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;
using Refit;
using Wpf.Ui.Gallery.Dto;

namespace Wpf.Ui.Gallery.Apis;

public class OrderCodeRequest
{
    //[JsonPropertyName("batch_no")]
    [JsonPropertyName("orderCode")]
    public string OrderCode { get; set; }

    [JsonPropertyName("force")] public bool Force { get; set; } = false;
}

public class BatchNoRequest
{
    //[JsonPropertyName("batch_no")]
    [JsonPropertyName("batchNo")]
    public string BatchNo { get; set; }
}

public class ItemIdRequest
{
    //[JsonPropertyName("batch_no")]
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; }
}

public interface IOrderApi
{
    // 获取订单编码
    [Post("/api/v2/factoryInterface/getOrderDetailByOrderCode")]
    Task<FactoryApiResponse<Object>> getOrderDetailByOrderCode(
        [Body] OrderCodeRequest request,
        [Header("Token")] string token
    );
    
    
    [Post("/api/v2/factoryInterface/getOrderDetailByBatchNo")]
    Task<FactoryApiResponse<Object>> getOrderDetailByBatchNo(
        [Body] BatchNoRequest request,
        [Header("Token")] string token
    );
    
    
    [Post("/api/v2/factoryInterface/getOrderDetailByItemId")]
    Task<FactoryApiResponse<Object>> getOrderDetailByItemId(
        [Body] ItemIdRequest request,
        [Header("Token")] string token
    );
    
    // 获取物流面单信息
    [Post("/api/factoryInterface/getOrderExpressInfoByOrderCode")]
    Task<FactoryApiResponse<Object>> getOrderExpressInfoByOrderCode(
        [Body] OrderCodeRequest request,
        [Header("Token")] string token
    );
    
    
    // 设置发货
    [Post("/api/factoryInterface/setOrderCompleteByOrderCode")]
    Task<FactoryApiResponse<Object>> setOrderCompleteByOrderCode(
        [Body] OrderCodeRequest request,
        [Header("Token")] string token
    );
    
    // 强制设置发货
    [Post("/api/factoryInterface/setOrderCompleteByOrderCode")]
    Task<FactoryApiResponse<Object>> setOrderCompleteByOrderCodeForce(
        [Body] OrderCodeRequest request,
        [Header("Token")] string token
    );
}