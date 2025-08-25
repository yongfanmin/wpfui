// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Dto.Machine;

public class SaveLocalInfo
{
    public string Name { get; set; } = FileName.ProduceImgLayoutName;
    public ImgSupportFormat ImgFormat { get; set; } = ImgSupportFormat.Jpeg;
    public string LocalPath { get; set; }
}