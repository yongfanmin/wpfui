// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.CreateImg;

public class ImgInfo
{
    public string ImgUrl { get; set; }

    public string ImgLocalPath { get; set; }

    public ImageLayer ImageLayer { get; set; }
}

public class ImageLayer
{
    public int Width { get; set; }

    public int Height { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public decimal Scale { get; set; }

    public decimal Rotate { get; set; }
}