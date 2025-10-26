// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.ControlsLookup;
using Wpf.Ui.Gallery.ViewModels.Pages;
using MenuItem = System.Windows.Controls.MenuItem;

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

        var menuItem = row.ContextMenu.Items.OfType<MenuItem>().FirstOrDefault(x => x.Tag as string == "ResetMenuItem");
        if (menuItem == null) return;

        var PGrid = ProductBatchItemDataGrid;
        // If the right-clicked row is not in the current selection,
        // clear the selection and select only the right-clicked row.
        if (!PGrid.SelectedItems.Contains(row.DataContext))
        {
            PGrid.SelectedItems.Clear();
            PGrid.SelectedItems.Add(row.DataContext);
        }
        if (PGrid.SelectedItems.Count > 0)
        {
            menuItem.CommandParameter = PGrid.SelectedItems;
        }
    }
}