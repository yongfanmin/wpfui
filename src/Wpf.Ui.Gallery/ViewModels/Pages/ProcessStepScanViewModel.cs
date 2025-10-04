// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.ViewModels.Pages;

public partial class ProcessStepScanViewModel : ObservableObject
{
    [ObservableProperty]
    private string _batchNo = string.Empty;

    [RelayCommand]
    private void OnEnterConfirm()
    {
        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = "扫码枪确认", Content = $"已确认批次号: {BatchNo}", CloseButtonText = "OK"
        };
        _ = messageBox.ShowDialogAsync();
        // TODO: Add logic to be executed when Enter is pressed.
    }
}