// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.CreateImg;

public class RollOfFabric
{
    // 布料卷需要与机器绑定
    // 机器需要设置能打印的图片格式 是否支持透明背景 打印DPI是多少 支持神什么打印颜色标准sRGB IEC61966-2.1 (通用标准) 或是专门用于印刷的 U.S. Web Coated (SWOP) v2 (CMYK)？
    // 机器支持压缩吗, 支持什么压缩格式 Lzw / Deflate (Zip) 还是 JPEG
    public int WidthMm { get; set; }
    public int CurrentMaxLengthMm { get; set; }
}