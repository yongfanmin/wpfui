// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using Wpf.Ui.Gallery.LocalConfig;

namespace Wpf.Ui.Gallery.ViewModels.Dialog;

public partial class PrintSettingsDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private PrintTaskConfig _printTaskConfig;

    public PrintSettingsDialogViewModel()
    {
        _printTaskConfig = LocalAppConfig.AppSetting.PrintTaskConfig;
        _printTaskConfig.PropertyChanged += OnPrintTaskConfigPropertyChanged;
    }

    private void OnPrintTaskConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When a property changes, save the settings
        LocalAppConfig.Save(LocalAppConfig.AppSetting);
    }
}