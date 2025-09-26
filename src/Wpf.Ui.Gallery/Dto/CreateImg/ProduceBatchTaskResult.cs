// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Dto.Machine;

namespace Wpf.Ui.Gallery.Dto.CreateImg;

public class ProduceBatchTaskResult
{
    public bool isNeedLayout { get; set; }
    public SaveLocalInfo saveLocalInfo { get; set; }
    public List<ProductionTask> ProductionTasks { get; set; }
}