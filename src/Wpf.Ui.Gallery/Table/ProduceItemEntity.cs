// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using SQLite;
using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Table;

public class ProduceItemEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    
    public int FactoryId { get; set; }
    /// <summary>
    ///  生产计划编号
    /// </summary>
    public string ProduceBatchNum { get; set; }

    /// <summary>
    ///  项批号号
    /// </summary>
    [Unique]
    public long BatchNum { get; set; }

    [Unique] public string ItemId { get; set; }

    public string OrderNo { get; set; }
    
    public string OrderCode { get; set; }

    public int OrderDetailId { get; set; }

    public string? SkuAlias { get; set; }
    
    public string? Color { get; set; }
    
    public string? Size { get; set; }

    public ProduceBatchItemProcess ProduceBatchItemProcess { get; set; } = ProduceBatchItemProcess.等待数据;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    public DateTime UpdateTime { get; set; } = DateTime.Now;

    public string ProduceBatchDetail { get; set; }

    public string ProduceImgLocalPath { get; set; }

    public string ProduceImgName { get; set; }
    
    public int ViewId { get; set; }
}