// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Dto.Machine;

public class MachineConfig
{
    //机器名称
    public string Name { get; set; }
    //打印工艺 如 热转印 数码印花
    public string ManufacturerProcess { get; set; }
    // 布料打印宽度 一般宽度固定 长度根据面料卷长度而定
    public int PrintWidthMm { get; set; }
    // 打印分辨率
    public int Dpi { get; set; }
    // 打印机支持的图片格式
    public List<ImgSupportFormat> ImgSupportFormatList { get; set; }
}