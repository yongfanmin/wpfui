// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Constant;

public enum TileType
{
    无平铺 = 0,
    基础平铺 = 1,
    /*
     正 反
     反 正
    四张图片拼合成一个块进行平铺
    */
    镜像平铺 = 2,
    // 先纵向平铺 然后再错位一半印花图高度进行横向平铺
    纵向错位平铺 = 3,
    // 先横向平铺 然后再错位一半印花图高度进行纵向平铺
    横向错位平铺 = 4,
}