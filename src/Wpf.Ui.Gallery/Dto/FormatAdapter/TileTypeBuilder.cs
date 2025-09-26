// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Dto.FormatAdapter;

public class TileTypeBuilder
{
    // 这个来源字符串由前端定义的 或则旧系统定义的 , 在这里转换成新的枚举定义
    public static TileType BuildTileTypeFromOriginString(string tileTypeOriginString)
    {
        if (string.IsNullOrEmpty(tileTypeOriginString))
        {
            return TileType.无平铺;
        }

        switch (tileTypeOriginString)
        {
            case "basicsTile":
            {
                return TileType.基础平铺;
            }
            case "Mirror":
            {
                return TileType.镜像平铺;
            }
            case "XSpacedTile":
            {
                return TileType.横向错位平铺;
            }
            case "YSpacedTile":
            {
                return TileType.纵向错位平铺;
            }
        }

        throw new Exception("不支持的平铺类型:" + tileTypeOriginString);
    }
}