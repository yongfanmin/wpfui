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

    public void SetNameByFormat(List<ProduceImgNameFormat> nameFormat,string size,string color,string productName,long batchNum)
    {
        IEnumerable<string> mappedItems = nameFormat.Select(format =>
        {
            return format switch
            {
                ProduceImgNameFormat.Size => $"{size}",
                ProduceImgNameFormat.Color => $"{color}",
                ProduceImgNameFormat.ProductName => $"{productName}",
                ProduceImgNameFormat.BatchNum => $"{batchNum}",
                _ => string.Empty,
            };
        });
        Name = string.Join("-", mappedItems);
    }
    public ImgSupportFormat ImgFormat { get; set; } = ImgSupportFormat.Jpeg;
    public string LocalPath { get; set; }
    
    public ProduceImgLayoutFolderClassify ProduceImgLayoutFolderClassify { get; set; }
    
    public ProduceImgNameFormat ProduceImgNameFormat { get; set; }
}