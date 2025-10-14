// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.Picking;
using Wpf.Ui.Gallery.Models;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Utils;
using Wpf.Ui.Gallery.Vo;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using TextBox = System.Windows.Controls.TextBox;

namespace Wpf.Ui.Gallery.ViewModels.Pages;

public partial class PickingViewModel : ObservableObject
{
    
    private readonly IOrderApi _orderApi;
    private readonly LoginInfoService _loginInfoService;
    
    private readonly IContentDialogService _contentDialogService;
    private readonly ISnackbarService _snackbarService;
    private readonly WindowsProviderService _windowsProviderService;

    private readonly object _lockObject = new object();

    [ObservableProperty] private ObservableCollection<OrderPick> _orderPickBasketList = new ObservableCollection<OrderPick>();

    [ObservableProperty] private int _basketCount = 5; // Default value

    [ObservableProperty] private string _pickOrderCode = string.Empty;
    
    [ObservableProperty] private string _scanEnterValue = string.Empty;

    public PickingViewModel(
        IContentDialogService contentDialogService, 
        ISnackbarService snackbarService,
        IOrderApi orderApi,
        LoginInfoService loginInfoService,
        WindowsProviderService windowsProviderService
        )
    {
        _contentDialogService = contentDialogService;
        _snackbarService = snackbarService;
        _orderApi = orderApi;
        _loginInfoService = loginInfoService;
        _windowsProviderService = windowsProviderService;
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
            OrderNo = "",
            OrderCode = orderCode,
            ItemCount = 0,
        });
    }

    // 拣货
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
                            thisOrderPick.PickCount++;
                            thisOrderPick.IsPicked = thisOrderPick.PickCount > 0 && thisOrderPick.PickCount >= thisOrderPick.ItemCount;
                            if (thisOrderPick.IsPicked)
                            {
                                AudioPlayer.PlayCompleteAudio();
                            }
                            else
                            {
                                AudioPlayer.PlaySuccessAudio();
                            }
                        }
                    }
                }
            }
            else
            {
                OrderPick thisOrderPick = OrderPickBasketList.FirstOrDefault(item => string.IsNullOrEmpty(item.OrderCode));
                if (thisOrderPick is not null)
                {
                    //获取订单数据
                    string token = _loginInfoService.getToken();
                    FactoryApiResponse<Object> orderDetailReturn = await _orderApi.getOrderDetailByOrderCode(
                        new OrderCodeRequest()
                        {
                            OrderCode = orderPick.OrderCode
                        },
                        token
                    );
                    if (orderDetailReturn.Data is null)
                    {
                        AudioPlayer.PlayErrorAudio();
                        var messageBox = new Wpf.Ui.Controls.MessageBox
                        {
                            Title = "警告", Content = "订单编码不存在", CloseButtonText = "好的 (Esc)"
                        };

                        _ = await messageBox.ShowDialogAsync();
                    }
                    else
                    {
                        // TODO 需要语音播报几号篮
                        // 开头第一个固定分拣数量为1
                        thisOrderPick.PickCount = 1;
                        thisOrderPick.OrderCode = orderPick.OrderCode;
                        thisOrderPick.OrderNo = orderPick.OrderNo;
                        thisOrderPick.ItemCount = orderPick.ItemCount;
                        OrderDetailVo orderDetailVo = JsonSerializer.Deserialize<OrderDetailVo>(orderDetailReturn.Data.ToString());
                        if (orderDetailVo is not null)
                        {
                            thisOrderPick.OrderNo = orderDetailVo.OrderNo;
                            thisOrderPick.ItemCount = orderDetailVo.ItemCount;
                            // 如果总条目等于一条 扫码则立即完成拣货
                            thisOrderPick.IsPicked = thisOrderPick.PickCount > 0 && thisOrderPick.PickCount >= thisOrderPick.ItemCount;
                            if (thisOrderPick.IsPicked)
                            {
                                AudioPlayer.PlayCompleteAudio();
                            }
                            else
                            {
                                AudioPlayer.PlaySuccessAudio();
                            }
                        }
                    }
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

    // 调整分拣数
    [RelayCommand]
    private async void AdjustSortQuantity(IList<object> selectedItems)
    {
        if (selectedItems.Count == 0)
        {
            AudioPlayer.PlayErrorAudio();
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "警告", Content = "请先选中需要调整的订单", CloseButtonText = "好的 (Esc)"
            };
            _ = await messageBox.ShowDialogAsync();
            return;
        }

        if (selectedItems.Count > 1)
        {
            AudioPlayer.PlayErrorAudio();
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "警告", Content = "一次只能调整一个订单", CloseButtonText = "好的 (Esc)"
            };
            _ = await messageBox.ShowDialogAsync();
            return;
        }

        var selectedOrder = selectedItems.Cast<OrderPick>().Single();
        var dialog = new ContentDialog(_contentDialogService.GetDialogHost());
        var numberBox = new NumberBox { Value = selectedOrder.PickCount };

        dialog.Title = "调整分拣数";
        dialog.Content = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = $"输入订单 {selectedOrder.OrderNo} 的已分拣数量:" },
                numberBox
            }
        };
        dialog.PrimaryButtonText = "确定";
        dialog.CloseButtonText = "取消";

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            int newPickCount = (int)numberBox.Value;
            if (newPickCount < 0)
            {
                _snackbarService.Show("错误", "分拣数量不能小于0", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(3));
                return;
            }
            if (newPickCount > selectedOrder.ItemCount)
            {
                _snackbarService.Show("错误", "分拣数量不能大于总数", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(3));
                return;
            }

            selectedOrder.PickCount = newPickCount;
            selectedOrder.IsPicked = newPickCount > 0 && newPickCount >= selectedOrder.ItemCount;
        }
    }
    
    [RelayCommand]
    private async void OnConfirmPick()
    {
        ScanOrder(PickOrderCode);
    }

    // 清空分拣篮
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
                    TimeSpan.FromSeconds(5)
                );
            }
        }
    }

    // 打印发货单
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
        else if(selectedItems.Count == 1)
        {
            var selectedOrders = selectedItems.Cast<OrderPick>().ToList();
            var selectedOrderPick = selectedItems.Cast<OrderPick>().Single();
            var printDialog = _windowsProviderService.GetWindow<Views.Windows.PrintDialog>();
            printDialog.ViewModel.FetchAndDownloadWaybill(selectedOrderPick);
            printDialog.Show();
        }
        else
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "警告", Content = "一次只能打印一个订单的面单", CloseButtonText = "好的 (Esc)"
            };
            _ = await messageBox.ShowDialogAsync();
        }
    }

    // 设置开始发货 设置发货
    [RelayCommand]
    private async void SetStartDelivery(IList<object> selectedItems)
    {
        var selectedBasket = selectedItems.Cast<OrderPick>().Where(item => !item.isEmpty()).ToList();
        if (selectedBasket.Count == 0)
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "警告", Content = "请选中需要设置发货的订单", CloseButtonText = "好的 (Esc)"
            };
            _ = await messageBox.ShowDialogAsync();
        }
        else
        {
            var dialog = new ContentDialog(_contentDialogService.GetDialogHost());
            var numberBox = new NumberBox { Value = BasketCount };

            dialog.Title = "发货提示";
            dialog.Content = new StackPanel { Children = { new TextBox() { Text = string.Join(" ", selectedBasket.Select(item => $"发货单号: {item.OrderNo}")) , IsReadOnly = true } } };
            dialog.PrimaryButtonText = "确定发货";
            dialog.CloseButtonText = "取消";

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string token = _loginInfoService.getToken();
                foreach (OrderPick orderSelect in selectedBasket)
                {
                    FactoryApiResponse<Object> response = await _orderApi.setOrderCompleteByOrderCode(
                        new OrderCodeRequest()
                        {
                            OrderCode = orderSelect.OrderCode
                        },
                        token
                        );
                    if (response.IsSuccess)
                    {
                        SetStartDeliveryStatus(orderSelect.OrderNo);
                    }
                    else
                    {
                        var messageBox = new Wpf.Ui.Controls.MessageBox
                        {
                            Title = "警告", Content = $"发货失败[{response.Msg}]，单号{orderSelect.OrderNo}", CloseButtonText = "好的 (Esc)"
                        };
                        _ = await messageBox.ShowDialogAsync();
                    }
                }
            }
        }
    }

    // 设置发货状态
    private void SetStartDeliveryStatus(string orderNo)
    {
        foreach (OrderPick orderPick in OrderPickBasketList)
        {
            if (orderPick.OrderNo == orderNo)
            {
                orderPick.Status = orderPick.IsPicked ? OrderPickStatus.全部发货 : OrderPickStatus.部分发货;
            }
        }
    }

    [RelayCommand]
    private async Task OpenSettingsDialog()
    {
        var dialog = new ContentDialog(_contentDialogService.GetDialogHost());
        var numberBox = new NumberBox { Value = BasketCount };

        dialog.Title = "设置分拣篮总数量";
        dialog.Content = new StackPanel { Children = { new TextBlock { Text = "输入当前分拣篮总数量:" }, numberBox } };
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