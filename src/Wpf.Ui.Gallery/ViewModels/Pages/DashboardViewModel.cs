// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.Messaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Dto.FormatAdapter;
using Wpf.Ui.Gallery.Models;
using Wpf.Ui.Gallery.Helpers;
using Wpf.Ui.Gallery.ImageProcessor;
using Wpf.Ui.Gallery.LocalConfig;
using Wpf.Ui.Gallery.Message;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Services.Downloader;
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

    [ObservableProperty] private ObservableCollection<ProduceBatchVo> _batchItems = new();
    [ObservableProperty] private bool _isAcceptingOrders = false;

    // 闪烁的指示灯
    [ObservableProperty] private bool _isIndicatorBlinking = false;


    private readonly DispatcherTimer _pollingTimer;
    private long RequestTimeAt = 0;

    public DashboardViewModel(
        IProduceBatchApi produceBatchApi,
        IProduceBatchInfoApi produceBatchInfoApi,
        IProduceBatchDetailApi produceBatchDetailApi,
        LoginInfoService loginInfoService,
        IImageDownloader imageDownloader,
        IProduceImageProcessor produceImageProcessor,
        INavigationService navigationService
    )
    {
        _produceBatchApi = produceBatchApi;
        _produceBatchInfoApi = produceBatchInfoApi;
        _produceBatchDetailApi = produceBatchDetailApi;
        _loginInfoService = loginInfoService;
        _imageDownloader = imageDownloader;
        _produceImageProcessor = produceImageProcessor;
        _navigationService = navigationService;

        _pollingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(LocalAppConfig.AppSetting.OrderRequestDurationSec)
        };
        _pollingTimer.Tick += async (sender, args) => await LoadBatchDataAsync();

        // 5. 在构造函数中，将自己注册为消息的接收者
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(NetworkActivityChangedMessage message)
    {
        // 7. 更新自己的属性，以响应收到的消息
        //    message.Value 就是 MainWindowViewModel 中 IsNetworkActive 的新值
        IsIndicatorBlinking = message.Value;
    }

    [RelayCommand]
    private async Task OnPageLoaded()
    {
    }

    /*public void setTableData(List<ProduceBatchItem> data)
    {
        BatchItems = new ObservableCollection<ProduceBatchItem>(data);
    }*/

    // 把接口返回的批次列表格式化成可现实的表格格式
    private ObservableCollection<ProduceBatchVo> getProduceBatchVoByProduceBatchItem(
        List<ProduceBatchItem> produceBatchItemList)
    {
        ObservableCollection<ProduceBatchVo> produceBatchList = new ObservableCollection<ProduceBatchVo>();
        foreach (ProduceBatchItem produceBatchItem in produceBatchItemList)
        {
            ProduceBatchVo produceBatchVo = new ProduceBatchVo();
            produceBatchVo.ProduceBatchNum = produceBatchItem.ProduceBatchNumber;
            produceBatchVo.OrderTotal = produceBatchItem.NumTotal;
            produceBatchVo.ProduceBatchStatus = produceBatchItem.Status;
            produceBatchList.Add(produceBatchVo);
        }

        return produceBatchList;
    }

    // 获取到批次订单,则把订单信息装载在表格数据上
    private void LoadOrderInfo2Batch(BatchOrderVo batchOrderVo)
    {
        foreach (ProduceBatchVo produceBatchVo in BatchItems)
        {
            bool isExist = false;
            foreach (BatchOrderVo orderVo in produceBatchVo.BatchOrderVoList)
            {
                if (batchOrderVo.BatchNum.Equals(orderVo.BatchNum))
                {
                    orderVo.OrderProduceProcess = batchOrderVo.OrderProduceProcess;
                    isExist = true;
                }
            }

            if (!isExist)
            {
                produceBatchVo.BatchOrderVoList.Add(batchOrderVo);
            }
        }
    }

    private BatchOrderVo OrderPrintBatch2BatchOrder(
        OrderPrintBatch orderPrintBatch,
        OrderProduceProcess orderProduceProcess)
    {
        BatchOrderVo batchOrderVo = new BatchOrderVo();
        batchOrderVo.ProduceBatchNum = orderPrintBatch.ProduceBatchNumber;
        batchOrderVo.BatchNum = orderPrintBatch.BatchNum;
        batchOrderVo.OrderNo = orderPrintBatch.OrderNo;
        batchOrderVo.PatternName = orderPrintBatch.DesignName;
        //batchOrderVo.SkuAlias = "";
        //batchOrderVo.PayTime = "";
        batchOrderVo.OrderProduceProcess = orderProduceProcess;
        return batchOrderVo;
    }

    private async Task LoadBatchDataAsync()
    {
        RequestTimeAt = DateTimeOffset.Now.ToUnixTimeSeconds();
        try
        {
            // 获取订单
            ProduceBatchRequest produceBatchRequest = new ProduceBatchRequest();
            // TODO 写死测试公版 T恤-3D 单幅3D教学 YM-女士T
            produceBatchRequest.DesignProductIds = "5666,5491,4800";
            // JD-桌布-偏白涤麻
            produceBatchRequest.DesignProductIds += ",5637";
            // TODO 写死固定获取一条
            produceBatchRequest.Num = 1;
            // TODO 写死印花机编码(热转印,白墨)
            string machineid = "68,405";
            string token = _loginInfoService.getToken();
            // 获取并锁定批次
            FactoryApiResponse<List<ProduceBatchItem>> produceBatchListResponse =
                await _produceBatchApi.getProduceBatchList(produceBatchRequest, token, machineid);
            if (produceBatchListResponse.IsSuccess)
            {
                BatchItems = getProduceBatchVoByProduceBatchItem(produceBatchListResponse.Data);
                foreach (ProduceBatchItem produceBatchItem in produceBatchListResponse.Data)
                {
                    ProduceBatchInfoRequest produceBatchInfoRequest = new ProduceBatchInfoRequest();
                    // 这个批次有多少订单?
                    produceBatchInfoRequest.Num = produceBatchItem.NumTotal;
                    produceBatchInfoRequest.ProduceBatchNumber = produceBatchItem.ProduceBatchNumber;
                    produceBatchInfoRequest.DesignProductIds = produceBatchRequest.DesignProductIds;
                    // 获取项批次信息 (订单信息)
                    FactoryApiResponse<List<ProductBatchOrderInfo>> produceBatchOrderList =
                        await _produceBatchInfoApi.getProduceBatchInfo(produceBatchInfoRequest, token, machineid);
                    foreach (ProductBatchOrderInfo productBatchOrderInfo in produceBatchOrderList.Data)
                    {
                        ProduceBatchDetailRequest produceBatchDetailRequest = new ProduceBatchDetailRequest();
                        produceBatchDetailRequest.BatchNo = productBatchOrderInfo.BatchNum;

                        // 获取项批次详情 (订单详情) 同一个订单不同产品不同批次号
                        FactoryApiResponse<List<JsonNode?>> produceBatchOrderDetailObj =
                            await _produceBatchDetailApi.getProduceBatchDetailObjTest(produceBatchDetailRequest, token,
                                machineid);
                        List<OrderPrintBatch> orderPrintBatchList = new List<OrderPrintBatch>();
                        orderPrintBatchList = OrderPrintBatch.ConstructByArrayJson(produceBatchOrderDetailObj.Data);
                        // 获取到此订单实际要生产得的 裁片/印花图 以及合成图需要的位置信息数据 (结构太过复杂且不确定 自动化格式化容易出错)
                        // FactoryApiResponse<List<OrderPrintBatch>> produceBatchDetail =
                        //    await _produceBatchDetailApi.getProduceBatchDetail(produceBatchDetailRequest, token,
                        //        machineid);
                        // orderPrintBatchList = produceBatchDetail.Data;
                        productBatchOrderInfo.OrderProduceProcess = OrderProduceProcess.数据已加载;
                        var taskBuilder = new ProductionTaskBuilder();
                        // 项批次详情对应工位批次列表 (一般只有一个工位批次  一个订单)
                        foreach (OrderPrintBatch orderPrintBatch in orderPrintBatchList)
                        {
                            LoadOrderInfo2Batch(OrderPrintBatch2BatchOrder(orderPrintBatch, OrderProduceProcess.数据已加载));
                        }

                        foreach (OrderPrintBatch orderPrintBatch in orderPrintBatchList)
                        {
                            try
                            {
                                //订单生产信息 转换成本软件 用于制造生产的图最少信息 (可以写各种方法 用于兼容其他平台的生产数据 转换成我们生产软件专用的数据结构)
                                List<ProductionTask> productionTasks = taskBuilder.BuildTasksFromOrder(orderPrintBatch);

                                foreach (ProductionTask productionTask in productionTasks)
                                {
                                    try
                                    {
                                        // 下载裁片图
                                        LocalImgInfo? patternPieceImg2localImg =
                                            await _imageDownloader.DownloadImageAsync(
                                                productionTask.PatternPieceImageUrl,
                                                FileName.getPatternPieceImgPath(productionTask.FactoryId,
                                                    productionTask.DesignProductId), productionTask.ViewId.ToString());
                                        productionTask.PatternPieceImageLocalImg = patternPieceImg2localImg;
                                        // 下载裁片对应印花图
                                        foreach (PrintLayerInfo taskPrintLayer in productionTask.PrintLayers)
                                        {
                                            LocalImgInfo? patternPrintImg2localImg =
                                                await _imageDownloader.DownloadImageAsync(
                                                    taskPrintLayer.DesignImageUrl,
                                                    FileName.getPatternPrintImgPath(productionTask.FactoryId,
                                                        taskPrintLayer.GalleryId), taskPrintLayer.GalleryId.ToString());
                                            taskPrintLayer.DesignImageLocalImg = patternPrintImg2localImg;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"任务 {productionTask.TaskId} 下载图片出错。");
                                        // 将 task 或整个 order 持久化到失败列表
                                        //await _failedOrderService.SaveFailedTaskAsync(task, ex.Message); 
                                    }
                                }

                                LoadOrderInfo2Batch(OrderPrintBatch2BatchOrder(orderPrintBatch,
                                    OrderProduceProcess.图片已加载));
                                // 目前同步阻塞
                                // 下载完成图片 生产图处理开始
                                List<ProductionTask> ProduceResult =
                                    await _produceImageProcessor.processProductionTask(productionTasks);

                                // 并行任务 异步回调写法
                                //var runningTasks = new List<Task<Result>>();
                                // foreach runningTasks.Add(runTask()); 
                                // Result[] allResults = await Task.WhenAll(runningTasks);
                                LoadOrderInfo2Batch(OrderPrintBatch2BatchOrder(orderPrintBatch,
                                    OrderProduceProcess.裁片已合成));
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e);
                            }
                        }

                        productBatchOrderInfo.OrderProduceProcess = OrderProduceProcess.等待数据;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., log them)
            Debug.WriteLine(ex);
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
}