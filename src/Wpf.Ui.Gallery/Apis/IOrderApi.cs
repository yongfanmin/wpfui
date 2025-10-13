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
}

public interface IOrderApi
{
    [Post("/api/v2/factoryInterface/getOrderDetailByOrderCode")]
    Task<FactoryApiResponse<Object>> getOrderDetailByOrderCode(
        [Body] OrderCodeRequest request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token
    );
}