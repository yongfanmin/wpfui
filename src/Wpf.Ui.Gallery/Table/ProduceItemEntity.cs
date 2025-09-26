// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using SQLite;
using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Table;

public class ProduceItemEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    /// <summary>
    ///  生产批次号
    /// </summary>
    public string ProduceBatchNum { get; set; }
    /// <summary>
    ///  项批次号
    /// </summary>
    [Unique]
    public long BatchNum { get; set; }
    
    public string OrderNo { get; set; }
    
    public string? SkuAlias { get; set; }
    
    public ProduceBatchItemProcess ProduceBatchItemProcess { get; set; } = ProduceBatchItemProcess.等待数据;
    
    public DateTime CreateTime { get; set; } = DateTime.Now;
    
    public DateTime UpdateTime { get; set; } = DateTime.Now;
    
    public string ProduceBatchDetail { get; set; }
    
    public string ProduceImgLocalPath { get; set; }
    
    public string ProduceImgName { get; set; }
}