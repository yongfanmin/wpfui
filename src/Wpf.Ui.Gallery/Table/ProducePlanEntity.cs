// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using SQLite;
using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Table;

public class ProducePlanEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    //生产计划编号
    [Unique]
    public string ProduceBatchNum { get; set; }
    // 已授权项总数
    public int AvlProduceBatchItemCount { get; set; }
    // 批次项总数
    public int ProduceBatchItemCount { get; set; }
    // 生产数据下载数量
    public int DataDownloadCount { get; set; }
    //图片下载数量
    public int ImgDownloadCount { get; set; }
    //裁片印花完成数量
    public int PiecePrintCount { get; set; }
    //生产排版完成数量
    public int LayoutCreateCount { get; set; }
    
    //需要排版的数量
    public int NeedLayoutCount { get; set; }

    //生产计划状态
    public ProduceBatchStatus ProduceBatchStatus { get; set; }
    public DateTime FactoryGetTime { get; set; }
    public DateTime CheckTime { get; set; }
}