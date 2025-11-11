// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.CreateImg;

public class UniqueBatchItem
{
    public long DesignProductId { get; set; }
    public bool IsMultiPiece { get; set; }
    public string ItemId { get; set; }
    public long BatchNum { get; set; }
    public string ProduceBatchNum { get; set; }
    
    public long SkuId { get; set; }
    
    // 成品id
    public long ProductId { get; set; }

    // 购物车的第几件  [订单号->多子订单->多件]
    public long BuyIndex { get; set; }
    
    public int ViewId { get; set; }

    public string Size { get; set; }
    
    public int SizeId { get; set; }
    public string Color { get; set; }
    
    public int ColorId { get; set; }
    public string ProductName { get; set; }
    public string OrderNo { get; set; }
    public string OrderCode { get; set; }
    public int OrderDetailId { get; set; }
    public int TargetDpi { get; set; }
    public List<ProductionTask> ProductionTasks { get; set; }
}