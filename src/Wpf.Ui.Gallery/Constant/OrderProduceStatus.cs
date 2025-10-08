// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Constant;

public enum OrderProduceStatus
{
    未知状态 = 0,
    待排单 = 1,
    待生产 = 2,
    待发货 = 3,
    已生产 = 4,
    生产中 = 5,
    取消生产 = 6,
    已发货 = 8,
    生产异常 = 9,
}