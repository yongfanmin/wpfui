// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.ControlsLookup;
using Wpf.Ui.Gallery.ViewModels.Pages;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace Wpf.Ui.Gallery.Views.Pages;

[GalleryPage("Produce Batch.", SymbolRegular.GridKanban20)]
public partial class ProduceBatchItemPage : INavigableView<ProduceBatchItemViewModel>
{
    public ProduceBatchItemPage(ProduceBatchItemViewModel itemViewModel)
    {
        ViewModel = itemViewModel;
        DataContext = this;

        InitializeComponent();
    }

    public ProduceBatchItemViewModel ViewModel { get; }
    
    private void DataGridRow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not DataGridRow { ContextMenu: { } } row) return;

        var menuItem = row.ContextMenu.Items.OfType<MenuItem>().FirstOrDefault(x => x.Header as string == "重置生产");
        if (menuItem == null) return;

        // If the right-clicked row is not in the current selection,
        // clear the selection and select only the right-clicked row.
        if (!ProductBatchItemDataGrid.SelectedItems.Contains(row.DataContext))
        {
            ProductBatchItemDataGrid.SelectedItems.Clear();
            ProductBatchItemDataGrid.SelectedItems.Add(row.DataContext);
        }

        menuItem.CommandParameter = ProductBatchItemDataGrid.SelectedItems;
    }
}