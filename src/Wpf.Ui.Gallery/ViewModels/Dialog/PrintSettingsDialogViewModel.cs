// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Wpf.Ui.Gallery.Component;
using Wpf.Ui.Gallery.LocalConfig;

namespace Wpf.Ui.Gallery.ViewModels.Dialog;

public partial class PrintSettingsDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private PrintTaskConfig _printTaskConfig;

    public Action? CloseAction { get; set; }

    public PrintSettingsDialogViewModel()
    {
        _printTaskConfig = LocalAppConfig.AppSetting.PrintTaskConfig;
        _printTaskConfig.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PrintTaskConfig.KeepAlivePS))
            {
                PhotoshopService.KeepAlive = _printTaskConfig.KeepAlivePS;
            }
            LocalAppConfig.Save(LocalAppConfig.AppSetting);
        };
    }

    [RelayCommand]
    private void Save()
    {
        LocalAppConfig.Save(LocalAppConfig.AppSetting);
        CloseAction?.Invoke();
    }
    
    [RelayCommand]
    private void Cancel()
    {
        CloseAction?.Invoke();
    }
}