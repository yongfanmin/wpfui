// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Constant;

// 印花裁切类型
public enum PrintCropType
{
    /*1.等比缩放【默认】 局部印 ?
    2. 原图裁剪 不变裁剪
    3.满幅缩放 (带印花区域? 桌布? 地垫) = 打印区居中裁剪
    4. 设计区域等比缩放              全印 单幅全印*/
    
    // 全印 如 全印缝纫衣服
    裁片底图全印裁切 = 0,
    // 满幅印 打印固定视图大小的图片 如地毯 (2025.9.9 当前可印花区域裁切 只支持可印花区域相对裁片底图是 上下左右都居中的位置)
    裁片满幅裁切 = 1,
    // 局部印 只打印需要打印的图片 如胸前印花
    裁片指定印花区域裁切 = 2,
}