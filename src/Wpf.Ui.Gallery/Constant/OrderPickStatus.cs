// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.ComponentModel;

namespace Wpf.Ui.Gallery.Constant;

public enum OrderPickStatus
{
    [Description("空篮")]
    空篮,
    
    [Description("未发货")]
    未发货,
    
    [Description("分拣中")]
    分拣中,
    
    [Description("分拣完成")]
    分拣完成,
    [Description("已打发货单")]
    已打发货单,
    
    [Description("部分发货")]
    部分发货,
    
    [Description("全部发货")]
    全部发货
}