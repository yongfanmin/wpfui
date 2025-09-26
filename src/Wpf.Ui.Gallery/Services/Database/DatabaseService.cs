// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json;
using SQLite;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Converters;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Dto.FormatAdapter;
using Wpf.Ui.Gallery.Dto.Machine;
using Wpf.Ui.Gallery.Table;
using Wpf.Ui.Gallery.Vo;

namespace Wpf.Ui.Gallery.Services.Database;

public class DatabaseService : IDatabaseService
{
    private static readonly SemaphoreSlim _dbWriteLock = new SemaphoreSlim(1, 1);

    private readonly string _dbPathAndFileName;
    private readonly string _password;

    public DatabaseService()
    {
        // 定义数据库文件的路径
        _dbPathAndFileName = FileName.DatabaseFilePath + FileName.DatabaseFileName;
        if (!File.Exists(_dbPathAndFileName))
        {
            Directory.CreateDirectory(FileName.DatabaseFilePath);
        }

        // 设置您的数据库密码
        _password = "myLeFoLeng";
    }

    private SQLiteConnection GetConnection()
    {
        // 创建带有密码的连接选项
        var options = new SQLiteConnectionString(_dbPathAndFileName, true, key: _password);
        var connection = new SQLiteConnection(options);
        return connection;
    }

    public void InitializeDatabase()
    {
        using (var db = GetConnection())
        {
            // 如果表不存在，则创建它
            db.CreateTable<ProducePlanEntity>();
            db.CreateTable<ProduceItemEntity>();
        }
    }

    public void AddProduceBatch(ProduceBatchVo produceBatchVo)
    {
        _dbWriteLock.Wait();
        try
        {
            using (var db = GetConnection())
            {
                try
                {
                    db.Insert(new ProducePlanEntity()
                    {
                        ProduceBatchNum = produceBatchVo.ProduceBatchNum,
                        AvailableProduceBatchItemCount = produceBatchVo.AvailableProduceBatchItemCount,
                        ProduceBatchItemCount = produceBatchVo.ProduceBatchItemCount,
                        ProduceBatchStatus = produceBatchVo.ProduceBatchStatus,
                        FactoryGetTime = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    // 插入生产批次失败 1.检查一下是否已经存在 2.已经存在检查是否被重置生产可以覆盖已存在条目
                    ProducePlanEntity producePlanEntity = getProduceBatch(produceBatchVo.ProduceBatchNum);
                    if (producePlanEntity is null)
                    {
                        // 未知错误
                    }
                    else
                    {
                        // 已经存在生产批次 判断一下此批次已经被重置生产 [人为重置生产前 都需要先认为判断是否能够重置 所以只要被重置 都可以直接覆盖数据] TODO 待验证
                        // TODO 删除生产批次 也需要删除对应项批次数据
                        db.Delete(producePlanEntity);
                        db.Insert(
                            new ProducePlanEntity()
                            {
                                ProduceBatchNum = produceBatchVo.ProduceBatchNum,
                                AvailableProduceBatchItemCount = produceBatchVo.AvailableProduceBatchItemCount,
                                ProduceBatchItemCount = produceBatchVo.ProduceBatchItemCount,
                                ProduceBatchStatus = produceBatchVo.ProduceBatchStatus,
                                FactoryGetTime = DateTime.Now
                            });
                    }
                }
            }
        }
        finally
        {
            _dbWriteLock.Release();
        }
    }

    // 更新生产批次 各类项批次进度 (数据加载/图片下载/生产图合成)
    public void updateProduceBatchProcess(string produceBatchNum, ProduceBatchItemProcess produceBatchItemProcess)
    {
        _dbWriteLock.Wait();
        try
        {
            if (string.IsNullOrEmpty(produceBatchNum))
            {
                return; // 无效输入
            }

            try
            {
                using (var db = GetConnection())
                {
                    // 1. 先查询到要更新的实体对象
                    var planToUpdate = db.Table<ProducePlanEntity>()
                        .FirstOrDefault(p => p.ProduceBatchNum.ToUpper() == produceBatchNum.ToUpper());

                    // 2. 检查对象是否存在
                    if (planToUpdate != null)
                    {
                        if (produceBatchItemProcess.Equals(ProduceBatchItemProcess.数据已加载))
                        {
                            planToUpdate.DataDownloadCount += 1;
                        }
                        else if (produceBatchItemProcess.Equals(ProduceBatchItemProcess.图片已加载))
                        {
                            planToUpdate.ImgDownloadCount += 1;
                        }
                        else if (produceBatchItemProcess.Equals(ProduceBatchItemProcess.裁片已合成))
                        {
                            planToUpdate.PiecePrintCount += 1;
                        }
                        else if (produceBatchItemProcess.Equals(ProduceBatchItemProcess.生产稿件已合成))
                        {
                            planToUpdate.LayoutCreateCountCount += 1;
                        }

                        // 4. 调用 .Update() 将整个对象的更改保存回数据库
                        //    Update 方法会根据主键 (Id) 找到正确的行并更新所有字段
                        int rowsAffected = db.Update(planToUpdate);

                        // 5. 检查是否成功更新了一行
                        if (rowsAffected <= 0)
                        {
                            Console.WriteLine($"生产批次'{produceBatchNum}' 变更进度失败, 数据写入失败");
                        }
                    }
                    else
                    {
                        // 记录一个日志或警告：尝试为一个不存在的批次号增加计数
                        Console.WriteLine($"生产批次'{produceBatchNum}' 不存在,无法变更批次状态");
                    }
                }
            }
            catch (Exception ex)
            {
                // 捕获并记录任何可能发生的数据库异常
                Console.WriteLine($"生产批次 '{produceBatchNum} 状态更新异常': {ex.Message}");
            }
        }
        finally
        {
            _dbWriteLock.Release();
        }
    }

    public ProducePlanEntity getProduceBatch(string produceBatchNum)
    {
        using (var db = GetConnection())
        {
            return db.Table<ProducePlanEntity>()
                .SingleOrDefault(p => p.ProduceBatchNum.ToUpper() == produceBatchNum.ToUpper());
        }
    }


    public List<ProducePlanEntity> GetProduceBatchList(int pageNum, int pageLen)
    {
        // 对输入参数进行基本的验证
        if (pageNum < 1)
        {
            pageNum = 1; // 如果页码小于1，则默认为第1页
        }

        if (pageLen < 1)
        {
            pageLen = 10; // 如果页面长度小于1，则设置一个默认值，例如10
        }

        using (var db = GetConnection())
        {
            // 计算需要跳过的记录数
            // 例如：获取第 1 页，跳过 (1-1)*10 = 0 条
            //       获取第 2 页，跳过 (2-1)*10 = 10 条
            int recordsToSkip = (pageNum - 1) * pageLen;
            return db.Table<ProducePlanEntity>() // 1. 从 Order 表开始查询
                .OrderByDescending(o => o.FactoryGetTime) // 2. 按照 OrderDate 字段进行降序排列（逆序）
                .Skip(recordsToSkip) // 3. 跳过前面所有页的数据
                .Take(pageLen) // 4. 获取当前页所需数量的数据
                .ToList(); // 5. 执行查询并将结果转换为 List<Order>
        }
    }


    public void UpdateProduceBatchStatus(string produceBatchNum, ProduceBatchStatus produceBatchStatus)
    {
        _dbWriteLock.Wait();
        try
        {
            if (string.IsNullOrEmpty(produceBatchNum))
            {
                return; // 无效输入
            }

            try
            {
                using (var db = GetConnection())
                {
                    // 1. 先查询到要更新的实体对象
                    var planToUpdate = db.Table<ProducePlanEntity>()
                        .FirstOrDefault(p => p.ProduceBatchNum.ToUpper() == produceBatchNum.ToUpper());
                    planToUpdate.ProduceBatchStatus = produceBatchStatus;
                    int rowsAffected = db.Update(planToUpdate);

                    // 5. 检查是否成功更新了一行
                    if (rowsAffected <= 0)
                    {
                        Console.WriteLine($"生产批次'{produceBatchNum}' 变更状态失败, 数据写入失败");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"批次号'{produceBatchNum}'更新状态错误: {ex.Message}");
            }
        }
        finally
        {
            _dbWriteLock.Release();
        }
    }

    public void AddProduceBatchItem(ProductBatchItemInfo productBatchItemInfo)
    {
        _dbWriteLock.Wait();
        try
        {
            using (var db = GetConnection())
            {
                try
                {
                    db.Insert(new ProduceItemEntity()
                    {
                        ProduceBatchNum = productBatchItemInfo.ProduceBatchNumber,
                        BatchNum = productBatchItemInfo.BatchNum,
                        ProduceBatchItemProcess = ProduceBatchItemProcess.等待数据,
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("插入项批次 唯一索引冲突, 已存在项批次条目:" + ex.Message);
                }
            }
        }
        finally
        {
            _dbWriteLock.Release();
        }
    }

    public void AddProduceBatchItemList(string produceBatchNumber, List<ProductBatchItemInfo> productBatchOrderInfoList)
    {
        string productBatchNum = "";
        foreach (ProductBatchItemInfo productBatchOrderInfo in productBatchOrderInfoList)
        {
            //productBatchNum = productBatchOrderInfo.ProduceBatchNumber;
            AddProduceBatchItem(productBatchOrderInfo);
        }
    }

    public void setProductBatchItemInfo(string produceBatchNum, long batchNum, UniqueBatchItem uniqueBatchItem)
    {
        _dbWriteLock.Wait();
        try
        {
            using (var db = GetConnection())
            {
                // 1. 查找需要更新的实体对象
                var planToUpdate = db.Table<ProduceItemEntity>()
                    .FirstOrDefault(field => field.ProduceBatchNum == produceBatchNum & field.BatchNum == batchNum);

                // 2. 确保对象存在
                if (planToUpdate != null)
                {
                    // 3. 修改对象的属性
                    planToUpdate.ProduceBatchDetail = JsonSerializer.Serialize(uniqueBatchItem);
                    planToUpdate.SkuAlias = uniqueBatchItem.ProductName;
                    planToUpdate.OrderNo = uniqueBatchItem.OrderNo;
                    planToUpdate.UpdateTime = DateTime.Now;
                    planToUpdate.ProduceBatchItemProcess = ProduceBatchItemProcess.数据已加载;
                    // 4. 调用 Update 方法将更改保存回数据库
                    //    该方法会返回受影响的行数
                    if (db.Update(planToUpdate) > 0)
                    {
                        //更新成功
                    }
                    else
                    {
                        // TODO 更新失败
                    }
                }
            }
        }
        finally
        {
            _dbWriteLock.Release();
        }
    }


    // 获取生产批次号下面的所有项批次
    public List<ProduceItemEntity> GetProduceBatchItemList(string produceBatchNum)
    {
        using (var db = GetConnection())
        {
            return db.Table<ProduceItemEntity>()
                .Where(field => field.ProduceBatchNum == produceBatchNum)
                .OrderByDescending(o => o.CreateTime)
                .ToList();
        }
    }

    public List<ProduceItemEntity> GetProduceBatchItemList(List<UniqueBatchItemNum> uniqueBatchItemNumList)
    {
        var produceBatchNums = uniqueBatchItemNumList
            .Select(item => item.ProduceBatchNum)
            .ToList();

        // 3. 从对象列表中，提取出所有 BatchNum 的值
        var batchNums = uniqueBatchItemNumList
            .Select(item => item.BatchNum)
            .ToList();

        using (var db = GetConnection())
        {
            // 4. 在 .Where() 子句中，对这两个简单的值列表使用 Contains
            return db.Table<ProduceItemEntity>()
                .Where(item =>
                    produceBatchNums.Contains(item.ProduceBatchNum) &&
                    batchNums.Contains(item.BatchNum)
                )
                .ToList();
        }
    }

    public ProduceItemEntity GetProduceBatchItem(string produceBatchNum, long batchNum)
    {
        using (var db = GetConnection())
        {
            return db.Table<ProduceItemEntity>()
                .SingleOrDefault(field => field.ProduceBatchNum == produceBatchNum & field.BatchNum == batchNum);
        }
    }


    public void updateProduceItemStatus(string produceBatchNum, long batchNum,
        ProduceBatchItemProcess produceBatchItemProcess)
    {
        _dbWriteLock.Wait();
        try
        {
            using (var db = GetConnection())
            {
                // 1. 查找需要更新的实体对象
                var planToUpdate = db.Table<ProduceItemEntity>()
                    .FirstOrDefault(field => field.ProduceBatchNum == produceBatchNum & field.BatchNum == batchNum);

                // 2. 确保对象存在
                if (planToUpdate != null)
                {
                    // 3. 修改对象的属性
                    planToUpdate.ProduceBatchItemProcess = produceBatchItemProcess;
                    planToUpdate.UpdateTime = DateTime.Now;
                    // 4. 调用 Update 方法将更改保存回数据库
                    //    该方法会返回受影响的行数
                    if (db.Update(planToUpdate) > 0)
                    {
                        //更新成功
                    }
                    else
                    {
                        // TODO 更新失败
                    }
                }
            }
        }
        finally
        {
            _dbWriteLock.Release();
        }
    }

    public void updateProduceBatchItemSaveLocalInfo(string produceBatchNum, long batchNum, SaveLocalInfo saveLocalInfo)
    {
        _dbWriteLock.Wait();
        try
        {
            using (var db = GetConnection())
            {
                // 1. 查找需要更新的实体对象
                var planToUpdate = db.Table<ProduceItemEntity>()
                    .FirstOrDefault(field => field.ProduceBatchNum == produceBatchNum & field.BatchNum == batchNum);

                // 2. 确保对象存在
                if (planToUpdate != null)
                {
                    // 3. 修改对象的属性
                    planToUpdate.ProduceImgLocalPath = saveLocalInfo.LocalPath;
                    planToUpdate.ProduceImgName =
                        saveLocalInfo.Name + ImgFormat2Extend.GetExtend(saveLocalInfo.ImgFormat);
                    planToUpdate.UpdateTime = DateTime.Now;
                    // 4. 调用 Update 方法将更改保存回数据库
                    //    该方法会返回受影响的行数
                    if (db.Update(planToUpdate) > 0)
                    {
                        //更新成功
                    }
                    else
                    {
                        // TODO 更新失败
                    }
                }
            }
        }
        finally
        {
            _dbWriteLock.Release();
        }
    }
}