// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Dto;
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
    private async void OnEnterConfirmStartProduce()
    {
        if (!string.IsNullOrWhiteSpace(BatchNo))
        {
            string token = _loginInfoService.getToken();
            BatchNo2Produce batchNo2Produce = new BatchNo2Produce();
            // 后端接口层出现歧义 实际是扫面单上的 item_id (工位批次) 进行核验, 但是参数名称叫 batchNo (项批次)
            batchNo2Produce.BatchNo = BatchNo;
            FactoryApiResponse<ProduceItemScanResultVo> setBatchNo2ProduceResponse =
                await _produceBatchApi.setBatchNo2Produce(batchNo2Produce, token);
            if (setBatchNo2ProduceResponse.IsSuccess)
            {
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
                    Title = "扫码核验失败[无法开始生产]", Content = setBatchNo2ProduceResponse.Msg, CloseButtonText = "OK"
                };
                _ = await messageBox.ShowDialogAsync();
            }

            // ProduceItemEntity produceItemEntity = _databaseService.GetProduceItem(batchNo2Produce.BatchNo);
            // if (produceItemEntity is null)
            // {
                // 不存在本地数据库 需要从远程抓取 (可能是多电脑 多扫码枪的情况下)
            // }
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