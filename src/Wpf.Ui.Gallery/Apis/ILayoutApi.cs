// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;
using Refit;
using Wpf.Ui.Gallery.Dto;

namespace Wpf.Ui.Gallery.Apis;

public class LayoutRequest
{
    [JsonPropertyName("design_template_id")]
    public long DesignProductId { get; set; }
}

public interface ILayoutApi
{
    [Get("/api/v2/factoryInterface/getPatternPieceLayout")]
    Task<FactoryApiResponse<Object>> GetLayoutInfo([Body] LayoutRequest request,
        // TODO 接口端使用非标准鉴权方式
        [Header("Token")] string token
        );
}