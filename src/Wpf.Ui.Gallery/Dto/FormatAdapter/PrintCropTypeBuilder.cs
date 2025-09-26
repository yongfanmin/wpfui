// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Dto.FormatAdapter;

public class PrintCropTypeBuilder
{
    // 这个来源字符串由前端定义的 或则旧系统定义的 , 在这里转换成新的枚举定义
    /*1.等比缩放【默认】 局部印 ?
    2. 原图裁剪 不变裁剪
    3.满幅缩放 (带印花区域? 桌布? 地垫) = 打印区居中裁剪
    4. 设计区域等比缩放              全印 单幅全印*/
    public static PrintCropType BuildPrintCropTypeFromOriginString(int PrintCropTypeOriginString)
    {
        switch (PrintCropTypeOriginString)
        {
            case 1:
            {
                return PrintCropType.裁片指定印花区域裁切;
            }
            /*case 2:
            {
                return PrintCropType.基础平铺;
            }*/
            case 3:
            {
                return PrintCropType.裁片满幅裁切;
            }
            case 4:
            {
                return PrintCropType.裁片底图全印裁切;
            }
        }

        throw new Exception("不支持的印花裁切类型:" + PrintCropTypeOriginString);
    }
}