// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto.Picking;

public class WaybillInfo
{
    public string OrderCode { get; set; }
    public string OrderNo { get; set; }
    public string BasketNum { get; set; }
    public string Url { get; set; }
    public string LocalUrl { get; set; }

    public bool IsPrint { get; set; } = false;
}