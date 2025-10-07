// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Services;

namespace Wpf.Ui.Gallery.ViewModels.Pages;

public partial class ProcessStepScanViewModel : ObservableObject
{
    private readonly LoginInfoService _loginInfoService;
    
    private readonly IProduceBatchApi _produceBatchApi;
    
    [ObservableProperty]
    private string _batchNo = string.Empty;


    public ProcessStepScanViewModel(
        LoginInfoService loginInfoService,
        IProduceBatchApi produceBatchApi
    )
    {
        _loginInfoService = loginInfoService;
        _produceBatchApi = produceBatchApi;
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