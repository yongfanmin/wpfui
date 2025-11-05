// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.ViewModels.Windows;

namespace Wpf.Ui.Gallery.Views.Windows
{
    /// <summary>
    /// Interaction logic for CreatePrintTaskDialog.xaml
    /// </summary>
    public partial class CreatePrintTaskDialog
    {
        public CreatePrintTaskViewModel ViewModel { get; }

        public CreatePrintTaskDialog(CreatePrintTaskViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
