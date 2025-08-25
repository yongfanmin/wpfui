// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.CreateImg;

public class PatternPieceLayout
{
    public int ViewId { get; set; }

    // 裁片生产图本地存放地址
    public string PatternPieceProduceLocalImgUrl { get; set; }

    // 以下字段 如果是自动排版 则值由程序生成； 如是手动排版, 需要由工厂后端设计单个排版 然后保存数据 由存储的数据还原到此字段
    public decimal Rotation { get; set; }

    // 左上角基准点
    public decimal TranslateX { get; set; }

    public decimal TranslateY { get; set; }

    // 水平拉伸

    // 垂直拉伸

    // 水平镜像

    // 垂直镜像
}