// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.PrintTask;

public class LayoutResult
{
    public uint LayoutWidthPx { get; set; }
    public uint LayoutHeightPx { get; set; }
    public List<LayoutImg> LayoutImgList { get; set; }
}