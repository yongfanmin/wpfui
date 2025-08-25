// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Dto.FormatAdapter;

namespace Wpf.Ui.Gallery.Dto.Machine;

public class ProduceImgInfo
{
    
    // 机器信息 必要
    public MachineConfig MachineConfig { get; set; }
    
    // 面料卷信息 非必要
    public RollOfFabric RollOfFabric { get; set; }
    
    //排版方式
    public int Layout { get; set; }
    
    //排版布料信息 (手动排版 需要固定一个布料宽高  自动排版需要固定一个布料宽度 长度自动)
    public LayoutClothInfo LayoutClothInfo { get; set; }
    
    //裁片排版信息
    public List<PatternPieceLayout> PatternPieceLayoutList { get; set; }
    
    // 保存本地所需的信息 保存目录 名称 ...
    public SaveLocalInfo SaveLocalInfo { get; set; }
}