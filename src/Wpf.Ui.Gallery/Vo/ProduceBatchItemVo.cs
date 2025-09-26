// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Vo;

public partial class ProduceBatchItemVo : ObservableObject
{
    // [ObservableProperty] private bool _isSelected; // <-- 新增：选中状态

    // 生产批次号
    public string ProduceBatchNum { get; set; }
    
    public long BatchNum { get; set; }
    
    // 单号
    public string OrderNo { get; set; }

    // 纸样/样板/公版/版
    // public string? PatternName { get; set; }

    // sku别名 (颜色-尺码 等集合的名称)
    public string? SkuAlias { get; set; }

    // 订单支付时间
    //public string? PayTime { get; set; }
    
    public string ProduceImgLocalPath { get; set; }
    
    public string ProduceImgName { get; set; }

    [ObservableProperty] private ProduceBatchItemProcess _produceBatchItemProcess = ProduceBatchItemProcess.等待数据;
}