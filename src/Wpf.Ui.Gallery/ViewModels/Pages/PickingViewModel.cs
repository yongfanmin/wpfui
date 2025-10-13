// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Dto.Picking;
using Wpf.Ui.Gallery.Models;
using Wpf.Ui.Gallery.Utils;
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
    
    [ObservableProperty] private string _scanEnterValue = string.Empty;

    public PickingViewModel(IContentDialogService contentDialogService, ISnackbarService snackbarService)
    {
        _contentDialogService = contentDialogService;
        _snackbarService = snackbarService;
        UpdateBasketList();
    }

    private bool UpdateBasketList()
    {
        // 不能清空数据 如果分拣篮内有东西 数据会丢失
        // OrderPickBasketList.Clear();
        for (int num = 1; num <= BasketCount; num++)
        {
            OrderPick orderPick = OrderPickBasketList.FirstOrDefault(basket => basket.BasketNumber == num);
            if (orderPick is null)
            {
                // 不存在 则新增分拣篮
                OrderPickBasketList.Add(OrderPick.Init(num));
            }
            else
            {
                // 存在 维持不变
            }
        }

        var needRemoveList = OrderPickBasketList.Where(basket => basket.BasketNumber > BasketCount).ToList();
        foreach (var item in needRemoveList)
        {
            if (item.isEmpty())
            {
                OrderPickBasketList.Remove(item);
            }
            else
            {
                AudioPlayer.PlayErrorAudio();
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "警告", Content = $"{item.BasketNumber}号篮不为空 请先清空再移除", CloseButtonText = "好的 (Esc)"
                };
                _ = messageBox.ShowDialogAsync();
                return false;
            }
        }
        return true;
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

    private async void AddOrder(OrderPick orderPick)
    {
        if (string.IsNullOrEmpty(orderPick.OrderCode))
        {
            AudioPlayer.PlayErrorAudio();
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "警告", Content = "分拣的订单编码为空", CloseButtonText = "好的 (Esc)"
            };

            _ = await messageBox.ShowDialogAsync();
        }
        else
        {
            if (OrderPickBasketList.Any(item => orderPick.OrderCode.Equals(item.OrderCode)))
            {
                OrderPick thisOrderPick = OrderPickBasketList.FirstOrDefault(item => orderPick.OrderCode.Equals(item.OrderCode));
                if (thisOrderPick is not null && thisOrderPick.PickCount >= thisOrderPick.ItemCount)
                {
                    AudioPlayer.PlayErrorAudio();
                    var messageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "警告", Content = "超出订单总项数", CloseButtonText = "好的 (Esc)"
                    };
                    _ = await messageBox.ShowDialogAsync();
                }
                else
                {
                    lock (_lockObject)
                    {
                        // 分拣篮内已经存在 则 增加已经拣货的件数
                        if (thisOrderPick is not null)
                        {
                            AudioPlayer.PlaySuccessAudio();
                            thisOrderPick.PickCount++;
                            thisOrderPick.IsPicked = thisOrderPick.PickCount > 0 && thisOrderPick.PickCount >= thisOrderPick.ItemCount;
                        }
                    }
                }
            }
            else
            {
                OrderPick thisOrderPick = OrderPickBasketList.FirstOrDefault(item => string.IsNullOrEmpty(item.OrderCode));
                if (thisOrderPick is not null)
                {
                    // 开头第一个固定分拣数量为1
                    // TODO 从接口加载订单数据: 使用订单编码获取订单编号 订单总项数
                    thisOrderPick.PickCount = 1;
                    thisOrderPick.OrderCode = orderPick.OrderCode;
                    thisOrderPick.OrderNo = orderPick.OrderNo;
                    thisOrderPick.ItemCount = orderPick.ItemCount;
                    orderPick.IsPicked = orderPick.PickCount > 0 && orderPick.PickCount >= orderPick.ItemCount;
                    AudioPlayer.PlaySuccessAudio();
                }
                else
                {
                    AudioPlayer.PlayErrorAudio();
                    // 请求数据
                    // TODO 没有任何空篮 不能分拣新订单
                    var messageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "警告", Content = "没有空的分拣篮", CloseButtonText = "好的 (Esc)"
                    };

                    _ = await messageBox.ShowDialogAsync();
                }
            }
        }
    }

    [RelayCommand]
    private async void OnConfirmPick()
    {
        ScanOrder(PickOrderCode);
    }

    [RelayCommand]
    private async void ClearBasket(IList<object> selectedItems)
    {
        if (selectedItems.Count == 0)
        {
            AudioPlayer.PlayErrorAudio();
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "警告", Content = "请先选中需要清空的篮号", CloseButtonText = "好的 (Esc)"
            };
            _ = await messageBox.ShowDialogAsync();
        }
        else
        {
            var dialog = new ContentDialog(_contentDialogService.GetDialogHost());

            dialog.Title = "提示";
            dialog.Content = "清空分拣篮后无法恢复";
            dialog.PrimaryButtonText = "确认清空";
            dialog.CloseButtonText = "取消";

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var selectedBasket = selectedItems.Cast<OrderPick>().ToList();
                foreach (OrderPick orderPick in OrderPickBasketList)
                {
                    if (selectedBasket.Exists(selectItem => selectItem.BasketNumber == orderPick.BasketNumber))
                    {
                        orderPick.Clear();
                    }
                }
                AudioPlayer.PlayClearBasketAudio();
                _snackbarService.Show(
                    "分拣篮清空",
                    $"已清空的篮子编号: {string.Join(" ", selectedBasket.Select(o => $"{o.BasketNumber}号篮"))}",
                    ControlAppearance.Success,
                    new SymbolIcon(SymbolRegular.Checkmark24),
                    TimeSpan.FromSeconds(3)
                );
            }
        }
    }

    [RelayCommand]
    private async void PrintDeliveryBill(IList<object> selectedItems)
    {
        if (selectedItems.Count == 0)
        {
            AudioPlayer.PlayErrorAudio();
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "警告", Content = "请先选中需要打印面单的篮号", CloseButtonText = "好的 (Esc)"
            };
            _ = await messageBox.ShowDialogAsync();
        }
        else
        {
            var selectedOrders = selectedItems.Cast<OrderPick>().ToList();
            _snackbarService.Show(
                "Print Delivery Bills",
                $"Printing delivery bills for orders: {string.Join(", ", selectedOrders.Select(o => o.OrderNo))}",
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.Print24),
                TimeSpan.FromSeconds(3)
            );
        }
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
            int currentBasketCount = BasketCount;
            int newBasketCount = (int)numberBox.Value;
            if (newBasketCount > currentBasketCount)
            {
                // 新增分拣篮 .... 如果需要进行判断
                BasketCount = newBasketCount;
            }
            else if (newBasketCount < currentBasketCount)
            {
                // 新增分拣篮 .... 如果需要进行判断
                BasketCount = newBasketCount;
            }
            else
            {
                // 分拣篮数量不变 不需要更新
                return;
            }

            if (!UpdateBasketList())
            {
                // 更新失败 恢复原来的分拣篮数量
                BasketCount = currentBasketCount;
            }
        }
    }
    
    // 回车事件
    [RelayCommand]
    private async void OnEnterConfirmBtn()
    {
        PickOrderCode = string.IsNullOrEmpty(PickOrderCode) ? ScanEnterValue : PickOrderCode;
        OnConfirmPick();
        ScanEnterValue = string.Empty;
    }
}