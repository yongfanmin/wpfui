// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.ViewModels.Pages;

namespace Wpf.Ui.Gallery.Views.Pages;

public partial class PickingPage : INavigableView<PickingViewModel>
{
    public PickingViewModel ViewModel { get; }

    public PickingPage(PickingViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
        this.PreviewKeyDown += Page_PreviewKeyDown;
        this.PreviewTextInput += Page_PreviewTextInput;
    }
    
    private void Page_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // TextBox不一定是输入框
        var focusedElement = Keyboard.FocusedElement as FrameworkElement;
        if (focusedElement?.Name != "PickOrderCode")
        {
            ViewModel.ScanEnterValue += e.Text;
            e.Handled = true;
        }
    }

    private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // If the Enter key is pressed, execute the command.

        if (e.Key == Key.Enter)
        {
            // The command will handle the logic based on the ScanEnterValue.
            ViewModel.EnterConfirmBtnCommand.Execute(null);
            // Mark the event as handled to prevent any default button clicks or further routing.
            e.Handled = true;
        }
        else if (e.Key == Key.Back && !string.IsNullOrEmpty(ViewModel.ScanEnterValue))
        {
            // 按下 Backspace按键 则移除最后一位字符
            ViewModel.ScanEnterValue = ViewModel.ScanEnterValue.Substring(0, ViewModel.ScanEnterValue.Length - 1);
            e.Handled = true;
        }
    }
}