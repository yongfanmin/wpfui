// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Constant;

public enum ProduceBatchItemProcess
{
    等待数据 = 0,
    数据已加载 = 1,
    图片已加载 = 2,
    裁片已合成 = 4,
    生产稿件已合成 = 8,
}