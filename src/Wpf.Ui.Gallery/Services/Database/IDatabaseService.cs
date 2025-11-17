// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Dto.FormatAdapter;
using Wpf.Ui.Gallery.Dto.Machine;
using Wpf.Ui.Gallery.Table;
using Wpf.Ui.Gallery.Vo;

namespace Wpf.Ui.Gallery.Services.Database;

public interface IDatabaseService
{
    public void InitializeDatabase();
    public void AddProduceBatch(ProduceBatchVo produceBatchVo);
    
    public void AddProduceBatchNeedLayoutItemCount(string produceBatchNum);
    public void UpdateProduceBatchStatus(string produceBatchNum, ProduceBatchStatus produceBatchStatus);
    public void AddProduceBatchItem(ProductBatchItemInfo productBatchItemInfo);
    public void UpdateProduceBatchStatus(IEnumerable<string> produceBatchNumbers, ProduceBatchStatus produceBatchStatus);

    public void AddProduceBatchItemList(string produceBatchNumber, List<ProductBatchItemInfo> productBatchOrderInfoList);

    public List<ProducePlanEntity> GetProduceBatchList(string createTimeValue);
    
    public List<ProducePlanEntity> GetProduceBatchList(int pageNum, int pageLen);
    
    public ProducePlanEntity GetProducePlan(string produceBatchNum);

    public List<ProduceItemEntity> GetProduceBatchItemList(string produceBatchNum,long batchNum);
    
    // 根据子项 获取生产项信息
    public ProduceItemEntity GetProduceItemByItemId(string itemId);
    
    public ProduceItemEntity GetProduceItemByOrderNo(string orderNo);
    
    public ProduceItemEntity GetProduceItemByBatchNo(long batchNo);
    
    public List<ProduceItemEntity> GetProduceBatchItemList(List<UniqueBatchItemNum> uniqueBatchItemNumList);
    
    public void setProductBatchItemInfo(string produceBatchNum,long batchNum, UniqueBatchItem uniqueBatchItem);

    public void updateProduceItemStatus(string produceBatchNum, long batchNum, ProduceBatchItemProcess produceBatchItemProcess);

    public void updateProduceBatchItemSaveLocalInfo(string produceBatchNum, long batchNum , SaveLocalInfo saveLocalInfo);
    
    
    public void updateProduceBatchProcess(string produceBatchNum, ProduceBatchItemProcess produceBatchItemProcess);
    
    public ProduceItemEntity GetProduceBatchItem(string produceBatchNum, long batchNum);

    public List<ProduceItemEntity> GetProduceItemList(string produceBatchNum);
    
    // 删除数据
    public void DeleteOldProductionData(int days);
}