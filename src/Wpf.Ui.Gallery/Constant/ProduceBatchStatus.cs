// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Constant;

public enum ProduceBatchStatus
{
    // 工厂未抓取订单进行生产
    工厂未提单 = 0,
    // 已经被工厂获取
    等待生产数据 = 1,
    处理中 = 2,
    生产准备就绪 = 3,
    
    分拣中 = 4,
    已发货 = 5,
}