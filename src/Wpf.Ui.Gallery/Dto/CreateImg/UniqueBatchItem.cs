// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.CreateImg;

public class UniqueBatchItem
{
    public long DesignProductId { get; set; }
    public string ItemId { get; set; }
    public long BatchNum { get; set; }
    public string ProduceBatchNum { get; set; }
    public string Size { get; set; }
    
    public int SizeId { get; set; }
    public string Color { get; set; }
    public string ProductName { get; set; }
    public string OrderNo { get; set; }
    public string OrderCode { get; set; }
    public int OrderDetailId { get; set; }
    public int TargetDpi { get; set; }
    public List<ProductionTask> ProductionTasks { get; set; }
}