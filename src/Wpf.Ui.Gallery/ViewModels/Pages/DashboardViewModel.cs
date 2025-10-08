// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Timers;
using CommunityToolkit.Mvvm.Messaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Converters;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Dto.FormatAdapter;
using Wpf.Ui.Gallery.Dto.Machine;
using Wpf.Ui.Gallery.ImageProcessor;
using Wpf.Ui.Gallery.LocalConfig;
using Wpf.Ui.Gallery.Message;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Services.Database;
using Wpf.Ui.Gallery.Services.Downloader;
using Wpf.Ui.Gallery.Table;
using Wpf.Ui.Gallery.Views.Pages;
using Wpf.Ui.Gallery.Vo;

namespace Wpf.Ui.Gallery.ViewModels.Pages;

//public partial class DashboardViewModel(INavigationService navigationService) : ViewModel
public partial class DashboardViewModel : ObservableObject, IRecipient<NetworkActivityChangedMessage>
{
    private readonly IProduceBatchApi _produceBatchApi;

    private readonly IProduceBatchInfoApi _produceBatchInfoApi;

    private readonly IProduceBatchDetailApi _produceBatchDetailApi;

    private readonly LoginInfoService _loginInfoService;

    private readonly IImageDownloader _imageDownloader;

    private readonly IProduceImageProcessor _produceImageProcessor;

    private readonly INavigationService _navigationService;

    private readonly IDatabaseService _databaseService;


    [ObservableProperty] private bool _isAcceptingOrders = false;

    // 闪烁的指示灯
    [ObservableProperty] private bool _isIndicatorBlinking = false;
    [ObservableProperty] private string _batchNo = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DateFilterButton> _dateFilterButtons = new();

    
    // DispatcherTimer 使用的是UI线程 会对UI产生阻塞导致卡顿 死锁
    // private readonly DispatcherTimer _pollingTimer;
    private readonly System.Timers.Timer _pollingTimer;

    // [新增] 一个锁，用于防止因计时器事件重叠而导致的并发问题
    private readonly SemaphoreSlim _pollingLock = new SemaphoreSlim(1, 1);

    private long RequestTimeAt = 0;

    [ObservableProperty]
    private ObservableCollection<ProduceBatchVo> _productBatchCollection = new ObservableCollection<ProduceBatchVo>();

    public DashboardViewModel(
        IProduceBatchApi produceBatchApi,
        IProduceBatchInfoApi produceBatchInfoApi,
        IProduceBatchDetailApi produceBatchDetailApi,
        LoginInfoService loginInfoService,
        IImageDownloader imageDownloader,
        IProduceImageProcessor produceImageProcessor,
        INavigationService navigationService,
        IDatabaseService databaseService
    )
    {
        _produceBatchApi = produceBatchApi;
        _produceBatchInfoApi = produceBatchInfoApi;
        _produceBatchDetailApi = produceBatchDetailApi;
        _loginInfoService = loginInfoService;
        _imageDownloader = imageDownloader;
        _produceImageProcessor = produceImageProcessor;
        _navigationService = navigationService;
        _databaseService = databaseService;

        /*_pollingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(LocalAppConfig.AppSetting.OrderRequestDurationSec)
        };
        _pollingTimer.Tick += async (sender, args) => await LoadBatchDataAsync();*/
        // 2. 使用无参数的构造函数来创建 System.Timers.Timer
        _pollingTimer = new System.Timers.Timer();

        // 3. 将 TimeSpan 直接转换为毫秒，并赋给 .Interval 属性
        _pollingTimer.Interval =
            TimeSpan.FromSeconds(LocalAppConfig.AppSetting.OrderRequestDurationSec).TotalMilliseconds;

        // 4. 订阅 Elapsed 事件
        _pollingTimer.Elapsed += OnTimerElapsed;

        // 5. 确保计时器会自动重复
        _pollingTimer.AutoReset = true;

        // 5. 在构造函数中，将自己注册为消息的接收者
        WeakReferenceMessenger.Default.Register(this);
        GenerateDateFilterButtons();
    }

    private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // [关键] 这个方法现在是在一个线程池线程上被调用的！
        await LoadBatchDataAsync();
    }

    public void Receive(NetworkActivityChangedMessage message)
    {
        // 7. 更新自己的属性，以响应收到的消息
        //    message.Value 就是 MainWindowViewModel 中 IsNetworkActive 的新值
        IsIndicatorBlinking = message.Value;
    }

    [ObservableProperty] private ProduceBatchItemProcess _produceBatchItemProcess = ProduceBatchItemProcess.等待数据;

    [RelayCommand]
    private async void OnDateFilter(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        foreach (var button in DateFilterButtons)
        {
            button.IsSelected = button.Value == value;
            if (button.IsSelected)
            {
                await SearchBatchDataAsync(button.Value);
            }
        }
        
    }

    private DateFilterButton GetSelectedDateFilterButton()
    {
        foreach (DateFilterButton button in DateFilterButtons)
        {
            if (button.IsSelected)
            {
                return button;
            }
        }
        var today = DateTime.Today;
        return DateFilterButtons.FirstOrDefault() ?? new DateFilterButton
        {
            DisplayText = "最近两天", IsSelected = true,
            Value = $"{today.AddDays(-1).ToString("yyyy-MM-dd")+","+today.ToString("yyyy-MM-dd")}",
        };
    }

    private void GenerateDateFilterButtons()
    {
        DateFilterButtons.Clear();
        var today = DateTime.Today;

        DateFilterButtons.Add(new DateFilterButton
        {
            DisplayText = "最近两天", IsSelected = true,
            Value = $"{today.AddDays(-1).ToString("yyyy-MM-dd")+","+today.ToString("yyyy-MM-dd")}",
        });
        DateFilterButtons.Add(new DateFilterButton
        {
            DisplayText = "前天",
            Value = today.AddDays(-2).ToString("yyyy-MM-dd"),
        });

        var dayBeforeYesterday = today.AddDays(-3);
        for (int i = 0; i < 6; i++)
        {
            var date = dayBeforeYesterday.AddDays(-i);
            DateFilterButtons.Add(new DateFilterButton
            {
                DisplayText = date.ToString("M月d日"),
                Value = date.ToString("yyyy-MM-dd")
            });
        }
        
        DateFilterButtons.Add(new DateFilterButton
        {
            DisplayText =  $"{today.AddDays(-9).ToString("M月d日")+" ~ "+today.AddDays(-30).ToString("M月d日")}",
            Value = $"{today.AddDays(-30).ToString("yyyy-MM-dd")+","+today.AddDays(-9).ToString("yyyy-MM-dd")}",
        });
    }

    [RelayCommand]
    private async Task OnPageLoaded()
    {
        Console.WriteLine("界面初始化,加载数据库数据");
        // 从数据库还原数据
        DateFilterButton dateFilterButton = GetSelectedDateFilterButton();
        SearchBatchDataAsync(dateFilterButton.Value);
    }

    private async Task SearchBatchDataAsync(string createTimeValue)
    {
        ProductBatchCollection = new ObservableCollection<ProduceBatchVo>();
        List<ProducePlanEntity> produceBatchList = _databaseService.GetProduceBatchList(createTimeValue);
        foreach (ProducePlanEntity producePlanEntity in produceBatchList)
        {
            ProductBatchCollection.Add(new ProduceBatchVo
            {
                ProduceBatchNum = producePlanEntity.ProduceBatchNum,
                AvlProduceBatchItemCount = producePlanEntity.AvlProduceBatchItemCount,
                DataDownloadCount = producePlanEntity.DataDownloadCount,
                ImgDownloadCount = producePlanEntity.ImgDownloadCount,
                PiecePrintCount = producePlanEntity.PiecePrintCount,
                LayoutCreateCount = producePlanEntity.LayoutCreateCount,
                NeedLayoutCount = producePlanEntity.NeedLayoutCount,
                ProduceBatchItemCount = producePlanEntity.ProduceBatchItemCount,
                FactoryGetTime = producePlanEntity.FactoryGetTime,
                ProduceBatchStatus = producePlanEntity.ProduceBatchStatus,
            });
        }
    }

    /*public void setTableData(List<ProduceBatchItem> data)
    {
        BatchItems = new ObservableCollection<ProduceBatchItem>(data);
    }*/

    // 把接口返回的批次列表格式化成可现实的表格格式
    private ObservableCollection<ProduceBatchVo> getProduceBatchVoByProduceBatchItem(
        List<ProduceBatchInfo> produceBatchItemList)
    {
        ObservableCollection<ProduceBatchVo> produceBatchList = new ObservableCollection<ProduceBatchVo>();
        foreach (ProduceBatchInfo produceBatchItem in produceBatchItemList)
        {
            ProduceBatchVo produceBatchVo = new ProduceBatchVo();
            produceBatchVo.ProduceBatchNum = produceBatchItem.ProduceBatchNumber;
            produceBatchVo.AvlProduceBatchItemCount = produceBatchItem.NumTotal;
            produceBatchVo.ProduceBatchItemCount = produceBatchItem.ProduceBatchNumberTotal;
            produceBatchVo.ProduceBatchStatus = ProduceStatusToStringConverter.Convert(produceBatchItem.Status);
            produceBatchVo.FactoryGetTime = DateTime.Now;
            produceBatchList.Add(produceBatchVo);
        }

        return produceBatchList;
    }

    private void AddOrUpdateBatch(IEnumerable<ProduceBatchVo> newBatchList)
    {
        // 1. 为了高效查找，将现有批次转换为一个以ProduceBatchNum为键的字典
        var existingBatchesDict = ProductBatchCollection.ToDictionary(
            b => b.ProduceBatchNum,
            StringComparer.OrdinalIgnoreCase // 使用不区分大小写的键
        );

        foreach (ProduceBatchVo newBatch in newBatchList.Reverse())
        {
            try
            {
                // _databaseService.AddProduceBatch(newBatch);
                // 2. 检查新批次是否已经存在
                if (existingBatchesDict.TryGetValue(newBatch.ProduceBatchNum, out var existingBatch))
                {
                    // **[更新逻辑]**
                    // 如果批次已存在，我们不添加，而是用新数据更新旧数据
                    // 这是一个更健壮的设计，可以反映状态的变化
                    existingBatch.ProduceBatchNum = newBatch.ProduceBatchNum;
                    existingBatch.ProduceBatchItemCount = newBatch.ProduceBatchItemCount;
                    existingBatch.AvlProduceBatchItemCount = newBatch.AvlProduceBatchItemCount;

                    existingBatch.DataDownloadCount = 0;
                    existingBatch.ImgDownloadCount = 0;
                    existingBatch.PiecePrintCount = 0;
                    existingBatch.LayoutCreateCount = 0;
                    existingBatch.NeedLayoutCount = 0;
                    existingBatch.ProduceBatchStatus = newBatch.ProduceBatchStatus;
                }
                else
                {
                    // 在UI线程上更新 实时显示在列表上
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 插入在第一个 而不是使用Add 方法插入在最后一个位置
                        ProductBatchCollection.Insert(0, newBatch);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("生产批次信息解析与显示出错:" + ex.Message);
            }
        }

        foreach (ProduceBatchVo newBatch in newBatchList)
        {
            _databaseService.AddProduceBatch(newBatch);
        }
    }

    private ProduceBatchItemVo OrderPrintBatch2BatchOrder(
        ProduceBatchItemDetail produceBatchItemDetail,
        ProduceBatchItemProcess produceBatchItemProcess)
    {
        ProduceBatchItemVo produceBatchItemVo = new ProduceBatchItemVo();
        produceBatchItemVo.ProduceBatchNum = produceBatchItemDetail.ProduceBatchNumber;
        produceBatchItemVo.BatchNum = produceBatchItemDetail.BatchNum;
        produceBatchItemVo.OrderNo = produceBatchItemDetail.OrderNo;
        produceBatchItemVo.OrderDetailId = produceBatchItemDetail.OrderDetailId;
        produceBatchItemVo.SkuAlias = produceBatchItemDetail.DesignName;
        //batchOrderVo.SkuAlias = "";
        //batchOrderVo.PayTime = "";
        produceBatchItemVo.ProduceBatchItemProcess = produceBatchItemProcess;
        return produceBatchItemVo;
    }

    private async Task LoadBatchDataAsync()
    {
        try
        {
            // 获取订单
            ProduceBatchRequest produceBatchRequest = new ProduceBatchRequest();
            // TODO 写死固定获取10条
            produceBatchRequest.Num = 10;
            string token = _loginInfoService.getToken();
            // 获取并锁定批次
            FactoryApiResponse<List<ProduceBatchInfo>> produceBatchListResponse =
                await _produceBatchApi.getProduceBatchList(produceBatchRequest, token);
            if (produceBatchListResponse.IsSuccess)
            {
                Console.WriteLine("批次信息抓取成功");
                AddOrUpdateBatch(getProduceBatchVoByProduceBatchItem(produceBatchListResponse.Data));
                foreach (ProduceBatchInfo produceBatchInfo in produceBatchListResponse.Data)
                {
                    ThreadPoolConfig.EnqueueAsync(async () =>
                    {
                        // 下载数据
                        List<UniqueBatchItemNum> uniqueBatchItemNumList =
                            await DownloadProduceBatchDataAsync(produceBatchInfo);
                        // 下载图片
                        await DownloadProduceBatchImgAsync(uniqueBatchItemNumList);
                        // 合成图片
                        await ComposeProduceImgAsync(uniqueBatchItemNumList);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., log them)
            Debug.WriteLine(ex);
        }
        finally
        {
            RequestTimeAt = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
    }

    public async Task ComposeProduceImgAsync(List<UniqueBatchItemNum> uniqueBatchItemNumList)
    {
        List<ProduceItemEntity> produceItemEntityList =
            _databaseService.GetProduceBatchItemList(uniqueBatchItemNumList);
        var composeActions = new List<Action>();
        foreach (ProduceItemEntity produceItemEntity in produceItemEntityList)
        {
            try
            {
                composeActions.Add(() =>
                {
                    UniqueBatchItem uniqueBatchItem =
                        JsonSerializer.Deserialize<UniqueBatchItem>(produceItemEntity.ProduceBatchDetail);
                    ProduceBatchTaskResult produceResult =
                        _produceImageProcessor.ProcessProductionTask(uniqueBatchItem);
                    if (produceResult is null)
                    {
                        // null 的情况 无法生产运行合成图 可能没有印花图层 或者报错
                        updateProduceBatchItemStatus(
                            uniqueBatchItem.ProduceBatchNum,
                            uniqueBatchItem.BatchNum,
                            ProduceBatchItemProcess.裁片已合成);
                    }
                    else
                    {
                        // 并行任务 异步回调写法
                        //var runningTasks = new List<Task<Result>>();
                        // foreach runningTasks.Add(runTask());
                        // Result[] allResults = await Task.WhenAll(runningTasks);
                        updateProduceBatchItemStatus(
                            uniqueBatchItem.ProduceBatchNum,
                            uniqueBatchItem.BatchNum,
                            ProduceBatchItemProcess.裁片已合成);
                        updateProduceBatchItemLocalInfo(
                            uniqueBatchItem.ProduceBatchNum,
                            uniqueBatchItem.BatchNum,
                            produceResult.saveLocalInfo);
                        if (produceResult.isNeedLayout)
                        {
                            // 需要排版 任务执行完成 即排版完成
                            updateProduceBatchItemStatus(
                                uniqueBatchItem.ProduceBatchNum,
                                uniqueBatchItem.BatchNum,
                                ProduceBatchItemProcess.生产稿件已合成);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        await ParallelTaskRunner.RunAllWithLimitedConcurrencyAsync(composeActions,
            LocalAppConfig.AppSetting.GetParallelThreads());
    }


    public async Task DownloadProduceBatchImgAsync(List<UniqueBatchItemNum> uniqueBatchItemNumList)
    {
        List<ProduceItemEntity> produceItemEntityList =
            _databaseService.GetProduceBatchItemList(uniqueBatchItemNumList);
        var downloadTasks = new List<Task>();
        foreach (ProduceItemEntity produceItemEntity in produceItemEntityList)
        {
            try
            {
                UniqueBatchItem uniqueBatchItem =
                    JsonSerializer.Deserialize<UniqueBatchItem>(produceItemEntity.ProduceBatchDetail);
                List<ProductionTask> productionTaskList = uniqueBatchItem.ProductionTasks;
                foreach (ProductionTask productionTask in productionTaskList)
                {
                    downloadTasks.Add(Task.Run(async () =>
                    {
                        // 下载裁片图
                        LocalImgInfo? patternPieceImg2localImg =
                            await _imageDownloader.DownloadImageAsync(
                                productionTask.PatternPieceImageUrl,
                                FileName.getPatternPieceImgPath(productionTask.FactoryId,
                                    productionTask.DesignProductId),
                                productionTask.ViewId.ToString());
                        // TODO 图片为空 需要报错
                        productionTask.PatternPieceImageLocalImg = patternPieceImg2localImg;
                        // 下载裁片对应印花图
                        foreach (PrintLayerInfo taskPrintLayer in productionTask.PrintLayers)
                        {
                            string fileName = taskPrintLayer.GalleryId.ToString();
                            if (taskPrintLayer.GalleryId == -1)
                            {
                                // 目前进入这个逻辑的是 文字印花  没有图库图片id ; 不能使用图库id命名文件
                                fileName = Path.GetFileNameWithoutExtension(taskPrintLayer.DesignImageUrl);
                                fileName = fileName.Replace("-ftp-product","-print-product");
                            }
                            LocalImgInfo? patternPrintImg2localImg =
                                await _imageDownloader.DownloadImageAsync(
                                    taskPrintLayer.DesignImageUrl,
                                    FileName.getPatternPrintImgPath(productionTask.FactoryId,
                                        taskPrintLayer.GalleryId),
                                    fileName);
                            // TODO 图片为空 需要报错
                            taskPrintLayer.DesignImageLocalImg = patternPrintImg2localImg;
                        }
                    }));
                    await Task.WhenAll(downloadTasks);
                }

                updateProduceBatchItemDetail(
                    uniqueBatchItem,
                    ProduceBatchItemProcess.图片已加载
                );
                // TODO 整批图片下载不完全的时候需要额外校验 部分出错不能算整批图片下载成功
            }
            catch (Exception ex)
            {
                Console.WriteLine($"任务 {produceItemEntity.ProduceBatchNum}-{produceItemEntity.BatchNum} 下载图片出错。");
                // 将 task 或整个 order 持久化到失败列表
                //await _failedOrderService.SaveFailedTaskAsync(task, ex.Message); 
            }
        }
    }


    public async Task<List<UniqueBatchItemNum>> DownloadProduceBatchDataAsync(
        ProduceBatchInfo produceBatchItem)
    {
        string token = _loginInfoService.getToken();
        List<UniqueBatchItemNum> downloadDataList = new List<UniqueBatchItemNum>();
        ProduceBatchInfoRequest produceBatchInfoRequest = new ProduceBatchInfoRequest();
        // 这个批次有多少订单?
        produceBatchInfoRequest.Num = produceBatchItem.ProduceBatchNumberTotal;
        produceBatchInfoRequest.ProduceBatchNumber = produceBatchItem.ProduceBatchNumber;
        // 获取项批次信息 (订单信息)
        FactoryApiResponse<List<ProductBatchItemInfo>> produceBatchOrderList =
            await _produceBatchInfoApi.getProduceBatchInfo(produceBatchInfoRequest, token);
        if (produceBatchOrderList.Data.Count != produceBatchItem.ProduceBatchNumberTotal ||
            produceBatchItem.ProduceBatchNumberTotal != produceBatchItem.NumTotal)
        {
            Console.WriteLine("批次号:" + produceBatchItem.ProduceBatchNumber + " 存在此账号为未被授权生产的产品");
        }

        // 写入条目
        _databaseService.AddProduceBatchItemList(
            produceBatchItem.ProduceBatchNumber,
            produceBatchOrderList.Data);
        Console.WriteLine("项批次" + produceBatchItem.ProduceBatchNumber + "详情抓取成功");
        foreach (ProductBatchItemInfo produceBatchItemInfo in produceBatchOrderList.Data)
        {
            ProduceBatchDetailRequest produceBatchDetailRequest = new ProduceBatchDetailRequest();
            produceBatchDetailRequest.BatchNo = produceBatchItemInfo.BatchNum;

            // 获取项位批次详情 (订单详情) 同一个订单不同产品不同批次号
            FactoryApiResponse<List<JsonNode?>> produceBatchOrderDetailObj =
                await _produceBatchDetailApi.getProduceBatchDetailObjTest(
                    produceBatchDetailRequest,
                    token);
            Console.WriteLine("批次" + produceBatchItem.ProduceBatchNumber + "-项位批次" +
                              produceBatchItemInfo.BatchNum + "详情抓取成功");
            List<ProduceBatchItemDetail> orderPrintBatchList =
                ProduceBatchItemDetail.ConstructByArrayJson(produceBatchOrderDetailObj.Data);
            var taskBuilder = new ProductionTaskBuilder();
            // 项批次详情对应工位批次列表 (一般只有一个工位批次  一个订单)
            Console.WriteLine("批次" + produceBatchItem.ProduceBatchNumber + "所有工位批次数据已加载");
            UpdateProduceBatchStatus(produceBatchItem.ProduceBatchNumber, ProduceBatchStatus.处理中);
            foreach (ProduceBatchItemDetail produceBatchItemDetail in orderPrintBatchList)
            {
                try
                {
                    AddProduceBatchNeedLayoutItemCount(produceBatchItem.ProduceBatchNumber,
                        produceBatchItemDetail.IsMultiPiece);
                    //订单生产信息 转换成本软件 用于制造生产的图最少信息 (可以写各种方法 用于兼容其他平台的生产数据 转换成我们生产软件专用的数据结构)
                    List<ProductionTask> productionTasks =
                        taskBuilder.BuildTasksFromItem(produceBatchItemDetail);
                    //TODO 兼容: 理论上DPI应该设置在整个布料排版上与设备绑定, 但是现在DPI却设置在裁片上
                    int targetDpi = Decimal.ToInt32(
                        produceBatchItemDetail.ProducePrintInfo
                            .FirstOrDefault().Value.TargetDpi);
                    UniqueBatchItem uniqueBatchItem = new UniqueBatchItem()
                    {
                        DesignProductId = produceBatchItemDetail.DesignProductId,
                        BatchNum = produceBatchItemDetail.BatchNum,
                        ProduceBatchNum = produceBatchItemDetail.ProduceBatchNumber,
                        Size = produceBatchItemDetail.Attributes.SizeAlias,
                        SizeId = produceBatchItemDetail.Attributes.SizeId,
                        Color = produceBatchItemDetail.Attributes.ColorAlias,
                        ProductName = produceBatchItemDetail.DesignName,
                        OrderNo = produceBatchItemDetail.OrderNo,
                        ItemId = produceBatchItemDetail.ItemId,
                        OrderDetailId = produceBatchItemDetail.OrderDetailId,
                        TargetDpi = targetDpi,
                        ProductionTasks = productionTasks
                    };
                    updateProduceBatchItemDetail(uniqueBatchItem, ProduceBatchItemProcess.数据已加载);
                    downloadDataList.Add(new UniqueBatchItemNum()
                    {
                        ProduceBatchNum = uniqueBatchItem.ProduceBatchNum, BatchNum = uniqueBatchItem.BatchNum,
                    });
                    Console.WriteLine($"生产批次{uniqueBatchItem.ProduceBatchNum}的项批次{uniqueBatchItem.BatchNum}数据已写入数据库");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }


        return downloadDataList;
    }

    
    public void UpdateProduceBatchStatus(string produceBatchNum, ProduceBatchStatus produceBatchStatus)
    {
        ProduceBatchVo produceBatchVo =
            ProductBatchCollection.FirstOrDefault(item => item.ProduceBatchNum.Equals(produceBatchNum));
        if (!(produceBatchVo is null))
        {
            produceBatchVo.ProduceBatchStatus = produceBatchStatus;
        }

        _databaseService.UpdateProduceBatchStatus(produceBatchNum, produceBatchStatus);
    }

    public void AddProduceBatchNeedLayoutItemCount(string produceBatchNum, bool isMultiPiece)
    {
        if (isMultiPiece)
        {
            _databaseService.AddProduceBatchNeedLayoutItemCount(produceBatchNum);
            foreach (ProduceBatchVo produceBatchVo in ProductBatchCollection)
            {
                if (produceBatchVo.ProduceBatchNum.Equals(produceBatchNum))
                {
                    produceBatchVo.NeedLayoutCount += 1;
                }
            }
        }
    }
    
    public void updateProduceBatchItemDetail(UniqueBatchItem uniqueBatchItem,
        ProduceBatchItemProcess produceBatchItemProcess)
    {
        _databaseService.setProductBatchItemInfo(uniqueBatchItem.ProduceBatchNum, uniqueBatchItem.BatchNum,
            uniqueBatchItem);
        updateProduceBatchItemStatus(
            uniqueBatchItem.ProduceBatchNum,
            uniqueBatchItem.BatchNum,
            produceBatchItemProcess);
    }


    public void updateProduceBatchItemLocalInfo(string produceBatchNumber, long batchNum, SaveLocalInfo saveLocalInfo)
    {
        // 写入本地保存路径
        _databaseService.updateProduceBatchItemSaveLocalInfo(produceBatchNumber, batchNum, saveLocalInfo);
    }

    public void updateProduceBatchItemStatus(string produceBatchNumber, long batchNum, ProduceBatchItemProcess status)
    {
        _databaseService.updateProduceItemStatus(produceBatchNumber, batchNum, status);
        // 把进度更新到生产批次表 (需要实时更新)
        _databaseService.updateProduceBatchProcess(produceBatchNumber, status);
        foreach (ProduceBatchVo produceBatchVo in ProductBatchCollection)
        {
            if (produceBatchVo.ProduceBatchNum.Equals(produceBatchNumber))
            {
                if (status.Equals(ProduceBatchItemProcess.数据已加载))
                {
                    produceBatchVo.DataDownloadCount += 1;
                }
                else if (status.Equals(ProduceBatchItemProcess.图片已加载))
                {
                    produceBatchVo.ImgDownloadCount += 1;
                }
                else if (status.Equals(ProduceBatchItemProcess.裁片已合成))
                {
                    produceBatchVo.PiecePrintCount += 1;
                }
                else if (status.Equals(ProduceBatchItemProcess.生产稿件已合成))
                {
                    produceBatchVo.LayoutCreateCount += 1;
                }
            }
        }

        ProducePlanEntity producePlanEntity = _databaseService.GetProducePlan(produceBatchNumber);
        if (producePlanEntity.NeedLayoutCount > 0 && (producePlanEntity.NeedLayoutCount == producePlanEntity.LayoutCreateCount))
        {
            // 多裁片印花且排版完成
            UpdateProduceBatchStatus(produceBatchNumber, ProduceBatchStatus.生产准备就绪);
        }
        else if(producePlanEntity.NeedLayoutCount == 0 && (producePlanEntity.AvlProduceBatchItemCount == producePlanEntity.PiecePrintCount))
        {
            // 非多裁片 裁片印花完成
            UpdateProduceBatchStatus(produceBatchNumber, ProduceBatchStatus.生产准备就绪);
        }
    }

    partial void OnIsAcceptingOrdersChanged(bool value)
    {
        if (value)
        {
            if (DateTimeOffset.Now.ToUnixTimeSeconds() - RequestTimeAt >
                LocalAppConfig.AppSetting.OrderRequestDurationSec)
            {
                // 上次请求时间间隔大于请求最小时间间隔 则在打开开关瞬间就发出请求
                _ = LoadBatchDataAsync();
            }

            _pollingTimer.Start();
            System.Diagnostics.Debug.WriteLine("Machine is now accepting orders. Polling started.");
        }
        else
        {
            _pollingTimer.Stop();
            System.Diagnostics.Debug.WriteLine("Machine is now stopped. Polling stopped.");
        }
    }

    [RelayCommand]
    private async Task ToggleLightOrDark()
    {
        var currentTheme = ApplicationThemeManager.GetAppTheme();
        ApplicationTheme newTheme =
            currentTheme == ApplicationTheme.Light ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(
            newTheme
        );
        LocalAppConfig.AppSetting.ApplicationTheme = newTheme;
        LocalAppConfig.Save(LocalAppConfig.AppSetting);
    }

    [RelayCommand]
    private void OnOpenFolder()
    {
        string path = LocalAppConfig.AppSetting.PrintedPatternFilePath;

        // 1. 健壮性检查：确保路径存在
        //    这可以防止因路径无效而导致无法预测的行为
        if (!Directory.Exists(path))
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "打开文件夹出错", Content = "生产稿件保存路径不存在 " + path, CloseButtonText = "OK"
            };
            _ = messageBox.ShowDialogAsync();
            // 可以考虑弹出一个提示框告知用户
            // MessageBox.Show($"目录不存在: {path}");
            return;
        }

        try
        {
            // 2. 创建一个 ProcessStartInfo 对象
            ProcessStartInfo startInfo = new ProcessStartInfo { FileName = path, UseShellExecute = true };

            // 5. 启动进程
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            // 捕获可能发生的异常，例如权限问题
            Console.WriteLine($"打开文件夹时发生错误: {ex}");
            // File.WriteAllText("error.log", ex.ToString());
        }
    }

    [RelayCommand]
    private async void OnOpenProduceItemWindow(Object? produceBatchVoObj)
    {
        if (produceBatchVoObj is ProduceBatchVo batchVo)
        {
            // 1. 获取批次号
            string produceBatchNum = batchVo.ProduceBatchNum;

            // 3. 使用 INavigationService 执行导航
            //    导航到 SettingsPage
            _navigationService.Navigate(typeof(ProduceBatchItemPage));

            // 2. 发送一个包含了批次号的消息
            //    我们假设 SettingsViewModel 会监听这个消息
            WeakReferenceMessenger.Default.Send(new ProduceBatchNumMessage() { ProduceBatchNumber = produceBatchNum });
        }
    }
}


public partial class DateFilterButton : ObservableObject
{
    [ObservableProperty]
    private string _displayText = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private bool _isSelected = false;
}