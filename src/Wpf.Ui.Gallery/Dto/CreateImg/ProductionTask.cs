// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.CreateImg;

// 生产任务 (一个裁片+多印花图 = 一个生产任务)
public class ProductionTask
{
    public string PatternPieceTitle { get; set; }
    public int FactoryId { get; set; }
    /// <summary>
    /// 任务的唯一标识，例如 "S2508...-后片"
    /// </summary>
    public string TaskId { get; set; }
    
    //公版id
    public long DesignProductId { get; set; }
    
    //单号
    public string OrderNo { get; set; }
    
    //裁片序号
    public int ViewId { get; set; }

    /// <summary>
    /// 裁片模板图的URL
    /// </summary>
    public string PatternPieceImageUrl { get; set; }
    
    // 裁片图片下载到本地地址
    public LocalImgInfo PatternPieceImageLocalImg { get; set; }
    
    // 裁片生产图本地存放地址
    public string PatternPieceProduceLocalImgUrl { get; set; }

    /// <summary>
    /// 裁片最终需要的物理宽度（毫米）
    /// </summary>
    public decimal PatternPieceTargetWidthMm { get; set; }
    
    /// <summary>
    /// 裁片最终需要的物理高度（毫米）
    /// </summary>
    public decimal PatternPieceTargetHeightMm { get; set; }
    
    /// <summary>
    /// 最终输出图像所需的DPI
    /// </summary>
    public int TargetDpi { get; set; }

    /// <summary>
    /// 此裁片上所有的印花图层
    /// </summary>
    public List<PrintLayerInfo> PrintLayers { get; set; }
}