// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.PrintTask;

public class PrintTaskConfig
{
    public bool ToCymk { get; set; }
    
    public bool ToWhiteInkSpot { get; set; }

    public bool IsConvertToCmyk()
    {
        return ToCymk || ToWhiteInkSpot;
    }

    public bool IsNeedProcess()
    {
        // 任意一个操作 都需要进入批处理流程 没勾选额外操作 则直接复制生产图到目标文件夹
        return ToWhiteInkSpot || ToCymk;
    }
}