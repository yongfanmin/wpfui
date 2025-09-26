// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Dto.Machine;

public class MachineConfig
{
    //机器名称 人工输入,最长限制64字符
    public string Name { get; set; }
    // 机器唯一码 隐藏字段 不显示, 由机器唯一码工具类自动生成
    public string MachineUniqueId { get; set; }
    //生产工艺 如 热转印 数码印花... 多选框
    /*public enum ManufacturerProcess
    {
        热升华 = 1,
        热转印 = 2,
        数码喷绘 = 3,
        丝网转印 = 4,
    }*/
    public List<ManufacturerProcess> ManufacturerProcessList { get; set; }
    // 布料打印宽度 单位毫米 一般宽度固定 长度根据面料卷长度而定
    public decimal PrintWidthMm { get; set; }
    // 打印分辨率
    public int Dpi { get; set; }
    // 打印机支持的图片格式 多选框
    /*public enum ImgSupportFormat
    {
        Jpeg = 1,
        Png = 2,
        Tiff = 3,
        Webp = 4
    }*/
    public List<ImgSupportFormat> ImgSupportFormatList { get; set; }
}