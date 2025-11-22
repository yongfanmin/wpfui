// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.ViewModels.Windows;

namespace Wpf.Ui.Gallery.Views.Windows;

public partial class LoginWindow
{
    private readonly LoginWindowViewModel _viewModel;
    public LoginWindow(LoginWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }

    private async void LoginWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeViewModelAsync();
    }
}