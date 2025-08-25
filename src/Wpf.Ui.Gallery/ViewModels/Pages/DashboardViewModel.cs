// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Net.Http;
using System.Text.Json;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Dto.FormatAdapter;
using Wpf.Ui.Gallery.Models;
using Wpf.Ui.Gallery.Helpers;
using Wpf.Ui.Gallery.ImageProcessor;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Services.Downloader;

namespace Wpf.Ui.Gallery.ViewModels.Pages;

//public partial class DashboardViewModel(INavigationService navigationService) : ViewModel
public partial class DashboardViewModel : ObservableObject
{
    private readonly IProduceBatchApi _produceBatchApi;

    private readonly IProduceBatchInfoApi _produceBatchInfoApi;

    private readonly IProduceBatchDetailApi _produceBatchDetailApi;

    private readonly LoginInfoService _loginInfoService;

    private readonly IImageDownloader _imageDownloader;

    private readonly IProduceImageProcessor _produceImageProcessor;

    private readonly INavigationService _navigationService;

    [ObservableProperty] private ObservableCollection<ProduceBatchItem> _batchItems = new();

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
    }

    [RelayCommand]
    private async Task OnPageLoaded()
    {
        await LoadBatchDataAsync();
    }

    private async Task LoadBatchDataAsync()
    {
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
            FactoryApiResponse<List<ProduceBatchItem>> produceBatchListResponse =
                await _produceBatchApi.getProduceBatchList(produceBatchRequest, token, machineid);
            if (produceBatchListResponse.IsSuccess)
            {
                BatchItems = new ObservableCollection<ProduceBatchItem>(produceBatchListResponse.Data);
                foreach (ProduceBatchItem produceBatchItem in BatchItems)
                {
                    ProduceBatchInfoRequest produceBatchInfoRequest = new ProduceBatchInfoRequest();
                    produceBatchInfoRequest.Num = 1;
                    produceBatchInfoRequest.ProduceBatchNumber = produceBatchItem.ProduceBatchNumber;
                    produceBatchInfoRequest.DesignProductIds = produceBatchRequest.DesignProductIds;
                    FactoryApiResponse<List<ProductBatchInfo>> produceBatchInfo =
                        await _produceBatchInfoApi.getProduceBatchInfo(produceBatchInfoRequest, token, machineid);
                    foreach (ProductBatchInfo productBatchInfo in produceBatchInfo.Data)
                    {
                        ProduceBatchDetailRequest produceBatchDetailRequest = new ProduceBatchDetailRequest();
                        produceBatchDetailRequest.BatchNo = productBatchInfo.BatchNum;
                        /*FactoryApiResponse<object> produceBatchDetailObj =
                            await _produceBatchDetailApi.getProduceBatchDetailObjTest(produceBatchDetailRequest, token,
                                machineid);*/
                        // 获取到此订单实际要生产得的 裁片/印花图 以及合成图需要的位置信息数据
                        FactoryApiResponse<List<OrderPrintBatch>> produceBatchDetail =
                            await _produceBatchDetailApi.getProduceBatchDetail(produceBatchDetailRequest, token,
                                machineid);
                        var taskBuilder = new ProductionTaskBuilder();
                        foreach (OrderPrintBatch orderPrintBatch in produceBatchDetail.Data)
                        {
                            //订单生产信息 转换成本软件 用于制造生产的图最少信息 (可以写各种方法 用于兼容其他平台的生产数据 转换成我们生产软件专用的数据结构)
                            List<ProductionTask> productionTasks = taskBuilder.BuildTasksFromOrder(orderPrintBatch);

                            foreach (ProductionTask productionTask in productionTasks)
                            {
                                try
                                {
                                    // 下载裁片图
                                    LocalImgInfo? patternPieceImg2localImg = await _imageDownloader.DownloadImageAsync(
                                        productionTask.PatternPieceImageUrl,
                                        FileName.getPatternPieceImgPath(productionTask.FactoryId,
                                            productionTask.DesignProductId), productionTask.ViewId.ToString());
                                    productionTask.PatternPieceImageLocalImg = patternPieceImg2localImg;
                                    // 下载裁片对应印花图
                                    foreach (PrintLayerInfo taskPrintLayer in productionTask.PrintLayers)
                                    {
                                        LocalImgInfo? patternPrintImg2localImg = await _imageDownloader.DownloadImageAsync(
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
                            // 目前同步阻塞
                            // 下载完成图片 生产图处理开始
                            List<ProductionTask> ProduceResult = await _produceImageProcessor.processProductionTask(productionTasks);
                            
                            // 并行任务 异步回调写法
                            //var runningTasks = new List<Task<Result>>();
                            // foreach runningTasks.Add(runTask()); 
                            // Result[] allResults = await Task.WhenAll(runningTasks);
                        }
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


    [RelayCommand]
    private void OnCardClick(string parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter))
        {
            return;
        }

        Type? pageType = NameToPageTypeConverter.Convert(parameter);

        if (pageType == null)
        {
            return;
        }

        _ = _navigationService.Navigate(pageType);
    }

}