// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Dto.FormatAdapter;

namespace Wpf.Ui.Gallery.Dto.CreateImg;

/// <summary>
/// 代表一个需要叠加在裁片上的印花图层及其所有变换信息
/// </summary>
public class PrintLayerInfo
{
    
    // 设计图 印花图对应的图库id
    public long GalleryId { get; set; }
    /// <summary>
    /// 印花图的URL
    /// </summary>
    public string DesignImageUrl { get; set; }
    
    // 印花图片下载到本地地址
    public LocalImgInfo DesignImageLocalImg { get; set; }
    
    /// <summary>
    /// 印花图在设计器画布上的物理尺寸（毫米）
    /// </summary>
    public RealSize DesignImageSizeMm { get; set; }

    // --- 变换信息 ---
    
    /// <summary>
    /// X轴方向的缩放比例
    /// </summary>
    public decimal ScaleX { get; set; }

    /// <summary>
    /// Y轴方向的缩放比例
    /// </summary>
    public decimal ScaleY { get; set; }
    
    /// <summary>
    /// 在设计器画布上的水平位移（像素或其他坐标单位，取决于transform的上下文）
    /// </summary>
    public decimal TranslateX { get; set; }

    /// <summary>
    /// 在设计器画布上的垂直位移（像素或其他坐标单位）
    /// </summary>
    public decimal TranslateY { get; set; }
    
    /// <summary>
    /// 旋转角度（度）
    /// </summary>
    public decimal Rotation { get; set; }
    
    // 打印层级 目前是跟裁片序号一致 2025-8-19
    public int ZIndex { get; set; }
}