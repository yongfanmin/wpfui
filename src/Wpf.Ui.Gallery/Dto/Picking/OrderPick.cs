// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.Picking;

public partial class OrderPick : ObservableObject
{
    public int BasketNumber { get; set; }
    public string OrderNo { get; set; }

    public string OrderCode { get; set; }
    [ObservableProperty] public int _pickCount;
    [ObservableProperty] public int _itemCount;
    [ObservableProperty] private bool _isPicked;
}