// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.ViewModels.Dialog;

namespace Wpf.Ui.Gallery.Views.Dialog;

public partial class PrintSettingsDialog
{
    public PrintSettingsDialog(PrintSettingsDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}