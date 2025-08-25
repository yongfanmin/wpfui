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

    public decimal Rotation { get; set; }

    public decimal TranslateX { get; set; }

    public decimal TranslateY { get; set; }
}