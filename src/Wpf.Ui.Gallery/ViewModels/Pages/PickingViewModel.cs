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
    private readonly object _lockObject = new object();

    [ObservableProperty] private ObservableCollection<OrderPick> _orderPickBasketList = new ObservableCollection<OrderPick>();

    [ObservableProperty] private int _basketCount = 5; // Default value

    [ObservableProperty] private string _pickOrderCode = string.Empty;

    public PickingViewModel(IContentDialogService contentDialogService, ISnackbarService snackbarService)
    {
        _contentDialogService = contentDialogService;
        _snackbarService = snackbarService;
        UpdateBasketList();
    }

    private void UpdateBasketList()
    {
        OrderPickBasketList.Clear();
        for (int i = 1; i <= BasketCount; i++)
        {
            OrderPickBasketList.Add(new OrderPick
            {
                BasketNumber = i + 1,
                OrderNo = "空篮", // Placeholder data
                PickCount = 0,
                ItemCount = 0
            });
        }
    }

    private void ScanOrder(string orderCode)
    {
        AddOrder(new OrderPick()
        {
            OrderNo = "xxxxxxxxxxx",
            OrderCode = orderCode,
            ItemCount = 10,
        });
    }

    private void AddOrder(OrderPick orderPick)
    {
        if (OrderPickBasketList.Any(item => item.OrderNo.Equals(orderPick.OrderNo)))
        {
            lock (_lockObject)
            {
                // 分拣篮内已经存在 则 增加已经拣货的件数
                OrderPick thisOrderPick = OrderPickBasketList.FirstOrDefault(item => item.OrderNo.Equals(orderPick.OrderNo));
                if (thisOrderPick is not null)
                {
                    thisOrderPick.PickCount++;
                    thisOrderPick.IsPicked = thisOrderPick.PickCount > 0 && thisOrderPick.PickCount >= thisOrderPick.ItemCount;
                }
            }
        }
        else
        {
            // 开头第一个固定分拣数量为1
            orderPick.PickCount = 1;
            orderPick.IsPicked = orderPick.PickCount > 0 && orderPick.PickCount >= orderPick.ItemCount;
            // 不存在则新增到分拣篮内
            OrderPickBasketList.Add(orderPick);
            // 请求数据
        }
    }

    [RelayCommand]
    private async void OnEnterConfirmPick()
    {
        ScanOrder(PickOrderCode);
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

        dialog.Title = "设置分拣篮数量";
        dialog.Content = new StackPanel { Children = { new TextBlock { Text = "输入当前分拣篮数量:" }, numberBox } };
        dialog.PrimaryButtonText = "确定";
        dialog.CloseButtonText = "取消";

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            BasketCount = (int)numberBox.Value;
            UpdateBasketList();
        }
    }
}