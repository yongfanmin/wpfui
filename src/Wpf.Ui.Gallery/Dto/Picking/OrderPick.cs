// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Dto.Picking;

public partial class OrderPick : ObservableObject
{
    [ObservableProperty] public int _basketNumber;
    [ObservableProperty] public string _orderNo;

    public string OrderCode { get; set; }
    [ObservableProperty] public int _pickCount;
    [ObservableProperty] public int _itemCount;
    [ObservableProperty] private bool _isPicked;
    
    [ObservableProperty] private OrderPickStatus _status;
    
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