// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Dto.Picking;
using Wpf.Ui.Gallery.Models;
using TextBlock = Wpf.Ui.Controls.TextBlock;

namespace Wpf.Ui.Gallery.ViewModels.Pages;

public partial class PickingViewModel : ObservableObject
{
    private readonly IContentDialogService _contentDialogService;
    private readonly ISnackbarService _snackbarService;

    [ObservableProperty]
    private ObservableCollection<OrderPick> _orderPickBasketList = new();

    [ObservableProperty]
    private int _basketCount = 5; // Default value

    public PickingViewModel(IContentDialogService contentDialogService, ISnackbarService snackbarService)
    {
        _contentDialogService = contentDialogService;
        _snackbarService = snackbarService;
        UpdateBasketList();
    }

    private void UpdateBasketList()
    {
        OrderPickBasketList.Clear();
        for (int i = 0; i < BasketCount; i++)
        {
            OrderPickBasketList.Add(new OrderPick
            {
                BasketNumber = i + 1,
                OrderNo = $"Order-{i + 1}", // Placeholder data
                PickCount = 0,
                ItemCount = 10
            });
        }
    }

    [RelayCommand]
    private void ClearBasket(IList<object> selectedItems)
    {
        if (selectedItems == null) return;
        var selectedOrders = selectedItems.Cast<OrderPick>().ToList();
        _snackbarService.Show(
            "Clear Baskets",
            $"Clearing baskets for orders: {string.Join(", ", selectedOrders.Select(o => o.OrderNo))}",
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.Checkmark24),
            TimeSpan.FromSeconds(3)
        );
    }

    [RelayCommand]
    private void PrintDeliveryBill(IList<object> selectedItems)
    {
        if (selectedItems == null) return;
        var selectedOrders = selectedItems.Cast<OrderPick>().ToList();
        _snackbarService.Show(
            "Print Delivery Bills",
            $"Printing delivery bills for orders: {string.Join(", ", selectedOrders.Select(o => o.OrderNo))}",
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.Print24),
            TimeSpan.FromSeconds(3)
        );
    }

    [RelayCommand]
    private async Task OpenSettingsDialog()
    {
        var dialog = new ContentDialog(_contentDialogService.GetDialogHost());
        var numberBox = new NumberBox { Value = BasketCount };

        dialog.Title = "Set Basket Count";
        dialog.Content = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = "Enter the number of picking baskets:" },
                numberBox
            }
        };
        dialog.PrimaryButtonText = "Save";
        dialog.CloseButtonText = "Cancel";

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            BasketCount = (int)numberBox.Value;
            UpdateBasketList();
        }
    }
}
