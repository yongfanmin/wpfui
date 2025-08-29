// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Dto.FormatAdapter;

public class RenderTypeBuilder
{
    public static RenderType getRenderType(int is_3d)
    {
        // 渲染类型
        if (is_3d.Equals(1))
        {
            return RenderType.全印_叠加裁片;
        }
        else
        {
            return RenderType.局部印_矩形框;
        }
    }
}