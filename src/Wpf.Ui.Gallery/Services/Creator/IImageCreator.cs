// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using NetVips;
using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Services.Creator;

public interface IImageCreator
{
    /*public Image CreateImageFromPhysicalSize(
        double widthMm,
        double heightMm,
        int dpi,
        ImgSupportFormat format,
        double[]? backgroundColor = null);*/

    public void SaveImageForProduction(Image image, string filePathWithoutExtension, ImgSupportFormat format);
}