// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Vo;

public partial class ProduceBatchVo : ObservableObject
{
    [ObservableProperty] private bool? _isSelected = false; // <-- 新增：支持三态(null)的选中状态

    // 批次号
    public string ProduceBatchNum { get; set; }

    // 批次下总单数
    public int OrderTotal { get; set; }

    // 批次状态
    public int ProduceBatchStatus { get; set; }

    // 本批次下的所有订单
    [ObservableProperty]
    private ObservableCollection<BatchOrderVo> _batchOrderVoList = new ObservableCollection<BatchOrderVo>();
}