// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Dto.CreateImg;

public class TileTool
{
    public TileType TileType { get; set; } = TileType.无平铺;
    /// <summary>
    /// 平铺时的水平间隙 (单位：毫米)。
    /// </summary>
    public decimal TileSpacingXMm { get; set; } = 0;

    /// <summary>
    /// 平铺时的垂直间隙 (单位：毫米)。
    /// </summary>
    public decimal TileSpacingYMm { get; set; } = 0;
}