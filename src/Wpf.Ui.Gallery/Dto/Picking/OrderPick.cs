// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Dto.Picking;

public partial class OrderPick : ObservableObject
{
    [ObservableProperty] private int _basketNumber; // 篮子编号

    [ObservableProperty] private string _orderNo = string.Empty; // 订单号

    [ObservableProperty] private string _orderCode = string.Empty; // 订单编码

    [ObservableProperty] private int _itemCount; // 总数

    [ObservableProperty] private int _pickCount; // 已拣

    [ObservableProperty] private OrderPickStatus _status = OrderPickStatus.空篮; // 状态

    [ObservableProperty] private bool _isPicked; // 是否已经完成拣货
    
    [ObservableProperty] private bool _isSelected; // 是否选中

    public WaybillInfo WaybillInfo { get; set; }

    public static OrderPick Init(int basketNumber)
    {
        return new OrderPick
        {
            BasketNumber = basketNumber,
            OrderCode = string.Empty,
            OrderNo = "空篮",
            PickCount = 0,
            ItemCount = 0
        };
    }

    public void Clear()
    {
        this.OrderCode = string.Empty;
        this.OrderNo = "空篮";
        this.PickCount = 0;
        this.ItemCount = 0;
        this.IsPicked = false;
    }

    public bool isEmpty()
    {
        return this.PickCount == 0 && this.ItemCount == 0;
    }
}