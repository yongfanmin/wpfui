// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.ViewModels.Windows;

namespace Wpf.Ui.Gallery.Views.Windows;

public partial class CreatePrintTaskWindow : FluentWindow
{
    public CreatePrintTaskWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is CreatePrintTaskViewModel viewModel)
        {
            RootGrid.Children.Clear();
            RootGrid.Children.Add(new CreatePrintTaskDialog(viewModel));
            DataContextChanged -= OnDataContextChanged;
        }
    }
}