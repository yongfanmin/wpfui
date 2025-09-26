// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using SQLite;

namespace Wpf.Ui.Gallery.Table;

public class ProduceOrderEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string ProduceBatchNum { get; set; }
    public DateTime FactoryGetTime { get; set; }
}