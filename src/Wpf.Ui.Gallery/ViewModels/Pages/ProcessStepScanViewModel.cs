// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Text.Json;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Services.Database;
using Wpf.Ui.Gallery.Table;
using Wpf.Ui.Gallery.Vo;

namespace Wpf.Ui.Gallery.ViewModels.Pages;

public partial class ProcessStepScanViewModel : ObservableObject
{
    private readonly LoginInfoService _loginInfoService;

    private readonly IProduceBatchApi _produceBatchApi;

    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private string _batchNo = string.Empty;

    [ObservableProperty]
    private string _printLayerImgView = string.Empty;

    [ObservableProperty]
    private ProduceItemScanResultVo _currentProductionItem;

    [ObservableProperty]
    private ObservableCollection<ProduceItemScanResultVo> _recentProductionItems = new();

    public ProcessStepScanViewModel(
        LoginInfoService loginInfoService,
        IProduceBatchApi produceBatchApi,
        IDatabaseService databaseService
    )
    {
        _loginInfoService = loginInfoService;
        _produceBatchApi = produceBatchApi;
        _databaseService = databaseService;
    }

    [RelayCommand]
    private void SetProduce(object batchNo)
    {
        if (batchNo is not null)
        {
            BatchNo = batchNo.ToString();
        }
    }

    [RelayCommand]
    private async void OnEnterConfirmStartProduce()
    {
        if (!string.IsNullOrWhiteSpace(BatchNo))
        {
            string token = _loginInfoService.getToken();
            BatchNo2Produce batchNo2Produce = new BatchNo2Produce();
            // 后端接口层出现歧义 实际是扫面单上的 item_id (工位批次) 进行核验, 但是参数名称叫 batchNo (项批次)
            batchNo2Produce.BatchNo = BatchNo;
            FactoryApiResponse<Object> setBatchNo2ProduceResponse =
                await _produceBatchApi.setBatchNo2Produce(batchNo2Produce, token);
            ProduceItemScanResultVo produceItemScanResultVo = JsonSerializer.Deserialize<ProduceItemScanResultVo>(setBatchNo2ProduceResponse.Data.ToString());
            //ProduceItemScanResultVo produceItemScanResultVo = setBatchNo2ProduceResponse.Data;
            if (setBatchNo2ProduceResponse.IsSuccess)
            {
                CurrentProductionItem = produceItemScanResultVo;
                RecentProductionItems.Insert(0, produceItemScanResultVo);
                if (RecentProductionItems.Count > 20)
                {
                    RecentProductionItems.RemoveAt(20);
                }

                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "扫码枪确认", Content = $"开始生产批次号: {BatchNo}", CloseButtonText = "OK"
                };

                _ = await messageBox.ShowDialogAsync();
            }
            else
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "扫码核验失败[无法开始生产]",
                    Content = setBatchNo2ProduceResponse.Msg,
                    CloseButtonText = "OK"
                };
                _ = await messageBox.ShowDialogAsync();
                if (!string.IsNullOrEmpty(produceItemScanResultVo.ProduceBatchNum))
                {
                    CurrentProductionItem = produceItemScanResultVo;
                }
            }

            ProduceItemEntity produceItemEntity = _databaseService.GetProduceItem(batchNo2Produce.BatchNo);
            List<string> printLayerImgList = new List<string>();
            if (produceItemEntity is not null)
            {
                UniqueBatchItem uniqueBatchItem =
                    JsonSerializer.Deserialize<UniqueBatchItem>(produceItemEntity.ProduceBatchDetail);
                foreach (ProductionTask productionTask in uniqueBatchItem.ProductionTasks)
                {
                    foreach (PrintLayerInfo productionTaskPrintLayer in productionTask.PrintLayers)
                    {
                        printLayerImgList.Add(productionTaskPrintLayer.DesignImageLocalImg.LocalUrl);
                    }
                }

                if (printLayerImgList.Count > 0)
                {
                    // 渲染印花图预览
                    PrintLayerImgView = printLayerImgList[0];
                }
            }
            else
            {
                // 不存在本地数据库 需要从远程抓取 (可能是多电脑 多扫码枪的情况下)
                Console.WriteLine("本地缺少此项数据,可能不是合成图所在机器或数据被删除");
            }
        }
    }
    
    [RelayCommand]
    private async void OnEnterConfirmCompleteProduce()
    {
        if (!string.IsNullOrWhiteSpace(BatchNo))
        {
            string token = _loginInfoService.getToken();
            BatchNo2ProduceComplete batchNo2Produce = new BatchNo2ProduceComplete();
            batchNo2Produce.BatchNo = BatchNo;
            FactoryApiResponse<Object> setBatchNo2ProduceResponse =
                await _produceBatchApi.setBatchNo2ProduceComplete(batchNo2Produce, token);
            if (setBatchNo2ProduceResponse.IsSuccess)
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "扫码枪确认", Content = $"完成生产批次号: {BatchNo}", CloseButtonText = "OK"
                };

                _ = await messageBox.ShowDialogAsync();
            }
            else
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "扫码核验失败[无法完成生产]", Content = setBatchNo2ProduceResponse.Msg, CloseButtonText = "OK"
                };
                _ = await messageBox.ShowDialogAsync();
            }
        }
    }
}