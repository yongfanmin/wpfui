// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.ViewModels.Dialog;
using Wpf.Ui.Gallery.ViewModels.Windows;
using Wpf.Ui.Gallery.Views.Dialog;

namespace Wpf.Ui.Gallery.Views.Windows;

public partial class CreatePrintTaskWindow : FluentWindow
{
    public CreatePrintTaskViewModel? ViewModel => DataContext as CreatePrintTaskViewModel;


    public CreatePrintTaskWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closing += OnClosing;
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (ViewModel is not { IsExecuting: true })
        {
            return;
        }

        // Prevent the window from closing immediately
        e.Cancel = true;

        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = "打印任务进行中",
            Content = "关闭窗口任务会在后台继续运行",
            PrimaryButtonText = "确认",
            CloseButtonText = "取消"
        };

        var result = await messageBox.ShowDialogAsync();

        if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
        {
            // The user confirmed they want to close the window.
            // Unsubscribe from the Closing event to prevent re-entry and close the window.
            Closing -= OnClosing;
            Close();
        }
    }


    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is CreatePrintTaskViewModel oldViewModel)
        {
            oldViewModel.ShowSettingsDialogRequested -= OnShowSettingsDialogRequested;
        }

        if (e.NewValue is CreatePrintTaskViewModel newViewModel)
        {
            RootGrid.Children.Clear();
            RootGrid.Children.Add(new CreatePrintTaskDialog(newViewModel));
            newViewModel.ShowSettingsDialogRequested += OnShowSettingsDialogRequested;
        }
    }

    private async Task OnShowSettingsDialogRequested()
    {
        var dialog = new ContentDialog(DialogPresenter)
        {
            Title = "高级印刷设置",
            // IsFooterVisible = false
            CloseButtonText = "取消"
        };

        var viewModel = new PrintSettingsDialogViewModel();
        var content = new PrintSettingsDialog(viewModel);
        
        viewModel.CloseAction = () => dialog.Hide();
        dialog.Content = content;
        
        await dialog.ShowAsync();
    }
}
