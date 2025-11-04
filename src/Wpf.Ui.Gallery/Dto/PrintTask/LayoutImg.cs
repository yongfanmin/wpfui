// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.PrintTask;

public class LayoutImg
{
    public uint WidthPx { get; set; }
    
    public uint HeightPx { get; set; }
    
    public int Id { get; set; }
    
    public uint PositionX { get; set; }
    public uint PositionY { get; set; }
    
    public string ImgPath { get; set; }
}