// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.PrintTask;

public class OrderTrackInfo
{
    public string OrderNo { get; set; }
        
    public string ProductImgPath { get; set; }
        
    public string ProductName { get; set; }
    
    public int OrderDetailId { get; set; }
    public long ProductId { get; set; }
    public long BuyIndex { get; set; }
    
    public long BuyNumber { get; set; }
    
    public string? SkuAlias { get; set; }
    
    public string? SkuInfo { get; set; }
}