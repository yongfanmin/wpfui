// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Converters;

public class ImgFormat2Extend
{
    public static string GetExtend(ImgSupportFormat imgSupportFormat)
    {
        switch (imgSupportFormat)
        {
            case ImgSupportFormat.Jpeg:
                return ".jpg";
            case ImgSupportFormat.Png:
                return ".png";
                case ImgSupportFormat.Tiff:
                return ".tif";
                case ImgSupportFormat.Webp:
                return ".webp";
        }

        return null;
    }
}