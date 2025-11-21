// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.Picking;
using Wpf.Ui.Gallery.LocalConfig;
using Wpf.Ui.Gallery.Message;
using Wpf.Ui.Gallery.Models;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Services.Database;
using Wpf.Ui.Gallery.Table;
using Wpf.Ui.Gallery.Utils;
using Wpf.Ui.Gallery.Vo;
using Wpf.Ui.Gallery.ViewModels.Windows;
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
    private readonly PrintDialogViewModel _printDialogViewModel;
    private readonly IDatabaseService _databaseService;
    private readonly object _lockObject = new object();

    [ObservableProperty]
    private ObservableCollection<OrderPick> _orderPickBasketList = new ObservableCollection<OrderPick>();

    [ObservableProperty] private int _basketCount = 5; // Default value

    [ObservableProperty] private string _pickOrderCode = string.Empty;

    [ObservableProperty] private string _scanEnterValue = string.Empty;

    public PickingViewModel(
        IContentDialogService contentDialogService,
        ISnackbarService snackbarService,
        IOrderApi orderApi,
        LoginInfoService loginInfoService,
        WindowsProviderService windowsProviderService,
        PrintDialogViewModel printDialogViewModel,
        IMessenger messenger,
        IDatabaseService databaseService
    )
    {
        _contentDialogService = contentDialogService;
        _snackbarService = snackbarService;
        _orderApi = orderApi;
        _loginInfoService = loginInfoService;
        _windowsProviderService = windowsProviderService;
        _printDialogViewModel = printDialogViewModel;
        _databaseService = databaseService;
        messenger.Register<PrintSuccessMessage>(this, (recipient, message) =>
        {
            // Handle the message here
            var orderPick = message.Value;
            var targetOrderPick = OrderPickBasketList.FirstOrDefault(o => o.OrderCode == orderPick.OrderCode);
            if (targetOrderPick != null)
            {
                targetOrderPick.Status = OrderPickStatus.已打发货单;
            }
        });
        LoadBasketSortHistory();
        UpdateBasketList();
    }

    private bool UpdateBasketList()
    {
        // 不能清空数据 如果分拣篮内有东西 数据会丢失
        // OrderPickBasketList.Clear();
        if (OrderPickBasketList.Count > BasketCount)
        {
            BasketCount = OrderPickBasketList.Count;
        }

        for (int num = OrderPickBasketList.Count; num <= BasketCount; num++)
        {
            for (int basketNum = 1; basketNum <= BasketCount; basketNum++)
            {
                OrderPick orderPick = OrderPickBasketList.FirstOrDefault(basket => basket.BasketNumber == basketNum);
                if (orderPick is null)
                {
                    // 不存在 则新增分拣篮
                    OrderPickBasketList.Add(OrderPick.Init(basketNum));
                    break;
                }
            }
        }

        var needRemoveList = OrderPickBasketList.Where((basket, index) => index >= BasketCount).ToList();
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

        SaveBasketSort();
        return true;
    }

    private void ScanOrder(string scanCode)
    {
        PickOrderCode = string.Empty;
        AddOrder(new OrderPick() { OrderNo = "", OrderCode = scanCode, ItemCount = 0, });
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
            string errorMessage = "";
            string token = _loginInfoService.getToken();
            FactoryApiResponse<Object> orderDetailReturn = null;
            if (StringUtil.IsBatchNo(orderPick.OrderCode))
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "警告", Content = "暂不支持项批号查询", CloseButtonText = "好的 (Esc)"
                };

                _ = await messageBox.ShowDialogAsync();
                return;
                /*errorMessage = "项批号不存在";
                orderDetailReturn = await _orderApi.getOrderDetailByBatchNo(
                    new OrderCodeRequest() { OrderCode = orderPick.OrderCode },
                    token
                );*/
            }
            else if (StringUtil.IsItem(orderPick.OrderCode))
            {
                errorMessage = "子项号不存在";
                orderDetailReturn = await _orderApi.getOrderDetailByItemId(
                    new ItemIdRequest() { ItemId = orderPick.OrderCode },
                    token
                );
                OrderReturnDetail orderReturnDetail =
                    JsonSerializer.Deserialize<OrderReturnDetail>(orderDetailReturn.Data.ToString());
                orderPick.OrderCode = orderReturnDetail.OrderCode;
            }
            else if (StringUtil.IsOrderNo(orderPick.OrderCode))
            {
                ProduceItemEntity produceItemEntity = _databaseService.GetProduceItemByOrderNo(orderPick.OrderCode);
                if (produceItemEntity is null)
                {
                    // TODO 本地存储条目为空 可能不是下载生产计划数据的电脑 需要从远程拉取数据
                    
                }
                else
                {
                    orderPick.OrderCode = produceItemEntity.OrderCode;
                }
            }
            if (OrderPickBasketList.Any(item => orderPick.OrderCode.Equals(item.OrderCode)))
            {
                OrderPick thisOrderPick =
                    OrderPickBasketList.FirstOrDefault(item => orderPick.OrderCode.Equals(item.OrderCode));
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
                            thisOrderPick.IsPicked = thisOrderPick.PickCount > 0 &&
                                                     thisOrderPick.PickCount >= thisOrderPick.ItemCount;
                            if (thisOrderPick.IsPicked)
                            {
                                // 分拣完成
                                thisOrderPick.Status = OrderPickStatus.分拣完成;
                                AudioPlayer.PlayCompleteAudio();
                                if (LocalAppConfig.AppSetting.AutoPrintAfterPicking)
                                {
                                    AutoPrintWaybill(thisOrderPick);
                                }
                            }
                            else
                            {
                                thisOrderPick.Status = OrderPickStatus.分拣中;
                                AudioPlayer.PlaySuccessAudio();
                            }
                        }
                    }
                }
            }
            else
            {
                OrderPick thisOrderPick =
                    OrderPickBasketList.FirstOrDefault(item => string.IsNullOrEmpty(item.OrderCode));
                if (thisOrderPick is not null)
                {
                    //获取订单数据
                    errorMessage = "订单编码不存在";
                    orderDetailReturn = await _orderApi.getOrderDetailByOrderCode(
                        new OrderCodeRequest() { OrderCode = orderPick.OrderCode },
                        token
                    );
                    if (orderDetailReturn.Data is null)
                    {
                        AudioPlayer.PlayErrorAudio();
                        var messageBox = new Wpf.Ui.Controls.MessageBox
                        {
                            Title = "警告", Content = errorMessage, CloseButtonText = "好的 (Esc)"
                        };

                        _ = await messageBox.ShowDialogAsync();
                    } else {
                        // TODO 需要语音播报几号篮
                        // 开头第一个固定分拣数量为1
                        thisOrderPick.PickCount = 1;
                        thisOrderPick.Status = OrderPickStatus.分拣中;
                        thisOrderPick.OrderCode = orderPick.OrderCode;
                        thisOrderPick.OrderNo = orderPick.OrderNo;
                        thisOrderPick.ItemCount = orderPick.ItemCount;
                        OrderDetailVo orderDetailVo =
                            JsonSerializer.Deserialize<OrderDetailVo>(orderDetailReturn.Data.ToString());
                        if (orderDetailVo is not null)
                        {
                            thisOrderPick.OrderNo = orderDetailVo.OrderNo;
                            thisOrderPick.ItemCount = orderDetailVo.ItemCount;
                            // 如果总条目等于一条 扫码则立即完成拣货
                            thisOrderPick.IsPicked = thisOrderPick.PickCount > 0 &&
                                                     thisOrderPick.PickCount >= thisOrderPick.ItemCount;
                            if (thisOrderPick.IsPicked)
                            {
                                thisOrderPick.Status = OrderPickStatus.分拣完成;
                                //分拣完成
                                AudioPlayer.PlayCompleteAudio();
                                if (LocalAppConfig.AppSetting.AutoPrintAfterPicking)
                                {
                                    AutoPrintWaybill(thisOrderPick);
                                }
                            }
                            else
                            {
                                thisOrderPick.Status = OrderPickStatus.分拣中;
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
            Children = { new TextBlock { Text = $"输入订单 {selectedOrder.OrderNo} 的已分拣数量:" }, numberBox }
        };
        dialog.PrimaryButtonText = "确定";
        dialog.CloseButtonText = "取消";

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            int newPickCount = (int)numberBox.Value;
            if (newPickCount < 0)
            {
                _snackbarService.Show("错误", "分拣数量不能小于0", ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(3));
                return;
            }

            if (newPickCount > selectedOrder.ItemCount)
            {
                _snackbarService.Show("错误", "分拣数量不能大于总数", ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(3));
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
        else if (selectedItems.Count == 1)
        {
            var selectedOrders = selectedItems.Cast<OrderPick>().ToList();
            var selectedOrderPick = selectedItems.Cast<OrderPick>().Single();
            
            if (selectedOrderPick.PickCount == 0)
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "警告", Content = $"无法操作空篮子", CloseButtonText = "好的 (Esc)"
                };
                _ = messageBox.ShowDialogAsync();
            }
            else
            {
                var printDialog = _windowsProviderService.GetWindow<Views.Windows.PrintDialog>();
                printDialog.ViewModel.FetchAndDownloadWaybill(selectedOrderPick);
                printDialog.Show();
            }
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
            dialog.Content = new StackPanel
            {
                Children =
                {
                    new TextBox()
                    {
                        Text = string.Join(" ", selectedBasket.Select(item => $"发货单号: {item.OrderNo}")),
                        IsReadOnly = true
                    }
                }
            };
            dialog.PrimaryButtonText = "确定发货";
            dialog.CloseButtonText = "取消";

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string token = _loginInfoService.getToken();
                foreach (OrderPick orderSelect in selectedBasket)
                {
                    FactoryApiResponse<Object> response = await _orderApi.setOrderCompleteByOrderCode(
                        new OrderCodeRequest() { OrderCode = orderSelect.OrderCode },
                        token
                    );
                    if (response.IsSuccess)
                    {
                        SetStartDeliveryStatus(orderSelect.OrderNo);
                        _snackbarService.Show("发货成功", $"单号 {orderSelect.OrderNo} 发货成功",
                            ControlAppearance.Success, new SymbolIcon(SymbolRegular.Check24),
                            TimeSpan.FromSeconds(5));
                    }
                    else
                    {
                        var messageBox = new Wpf.Ui.Controls.MessageBox
                        {
                            Title = "警告",
                            Content = $"发货失败 [{response.Msg}]，单号{orderSelect.OrderNo}",
                            PrimaryButtonText = "强制发货[忽略生产状态]",
                            CloseButtonText = "好的 (Esc)"
                        };
                        var failResult = await messageBox.ShowDialogAsync();
                        if (failResult == Wpf.Ui.Controls.MessageBoxResult.Primary)
                        {
                            await Task.Yield();
                            FactoryApiResponse<Object> responseAgain = await _orderApi.setOrderCompleteByOrderCodeForce(
                                new OrderCodeRequest() { OrderCode = orderSelect.OrderCode, Force = true },
                                token
                            );
                            if (responseAgain.IsSuccess)
                            {
                                SetStartDeliveryStatus(orderSelect.OrderNo);
                                _snackbarService.Show("发货成功", $"单号 {orderSelect.OrderNo} 发货成功",
                                    ControlAppearance.Success, new SymbolIcon(SymbolRegular.Check24),
                                    TimeSpan.FromSeconds(5));
                            }
                            else
                            {
                                var messageBoxAgain = new Wpf.Ui.Controls.MessageBox
                                {
                                    Title = "错误",
                                    Content = $"强制发货失败 [{responseAgain.Msg}]，单号{orderSelect.OrderNo}",
                                    CloseButtonText = "好的 (Esc)"
                                };
                                await messageBoxAgain.ShowDialogAsync();
                            }
                        }
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

        // Basket count setting
        var numberBox = new NumberBox { Value = BasketCount };

        // Printer setting
        var printerComboBox = new System.Windows.Controls.ComboBox();
        var printers = new List<string>();
        foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
        {
            printers.Add(printer);
        }
        printerComboBox.ItemsSource = printers;
        printerComboBox.SelectedItem = LocalAppConfig.AppSetting.DefaultWaybillPrinterName;

        // Auto-print setting
        var autoPrintToggle = new ToggleSwitch
        {
            Margin = new System.Windows.Thickness(0, 10, 0, 0),
            IsChecked = LocalAppConfig.AppSetting.AutoPrintAfterPicking,
            Content = "分拣完成后自动打印面单"
        };

        dialog.Title = "设置";
        dialog.Content = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = "输入当前分拣篮子数量:" },
                numberBox,
                new TextBlock { Text = "选择默认打印机:", Margin = new Thickness(0, 10, 0, 0) },
                printerComboBox,
                autoPrintToggle
            }
        };
        dialog.PrimaryButtonText = "确定";
        dialog.CloseButtonText = "取消";

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // Save basket count
            int currentBasketCount = BasketCount;
            int newBasketCount = (int)numberBox.Value;
            if (newBasketCount != currentBasketCount)
            {
                BasketCount = newBasketCount;
                if (!UpdateBasketList())
                {
                    BasketCount = currentBasketCount; // Revert on failure
                }
            }

            // Save printer and auto-print settings
            LocalAppConfig.AppSetting.DefaultWaybillPrinterName = printerComboBox.SelectedItem as string;
            LocalAppConfig.AppSetting.AutoPrintAfterPicking = autoPrintToggle.IsChecked ?? false;
            LocalAppConfig.Save(LocalAppConfig.AppSetting);
        }
    }

    private async void AutoPrintWaybill(OrderPick orderPick)
    {
        await _printDialogViewModel.FetchAndDownloadWaybill(orderPick);
        _printDialogViewModel.SelectedPrinter = LocalAppConfig.AppSetting.DefaultWaybillPrinterName;
        try
        {
            _printDialogViewModel.PrintCommand.Execute(null);
            _snackbarService.Show("发货单自动打印中...", $"订单编码: {orderPick.OrderCode}",
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.Check24),
                TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "警告", Content = $"分拣完成, 打印面单失败 {ex.Message}", CloseButtonText = "好的 (Esc)"
            };
            _ = messageBox.ShowDialogAsync();
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

    private void SaveBasketSort()
    {
        LocalAppConfig.AppSetting.BasketSortList.Clear();
        for (int i = 0; i < OrderPickBasketList.Count; i++)
        {
            LocalAppConfig.AppSetting.BasketSortList.Add(new BasketSort
            {
                BasketNumber = OrderPickBasketList[i].BasketNumber, Sort = i
            });
        }

        LocalAppConfig.Save(LocalAppConfig.AppSetting);
    }

    private void LoadBasketSortHistory()
    {
        if (LocalAppConfig.AppSetting.BasketSortList.Any())
        {
            var sortedBaskets = LocalAppConfig.AppSetting.BasketSortList
                .OrderBy(b => b.Sort)
                .Select(b => OrderPick.Init(b.BasketNumber))
                .ToList();

            OrderPickBasketList = new ObservableCollection<OrderPick>(sortedBaskets);
        }
    }

    [RelayCommand]
    private async void ReplaceBasket(object parameter)
    {
        if (parameter is OrderPick orderPick)
        {
            var dialog = new ContentDialog(_contentDialogService.GetDialogHost());
            var textBox = new Wpf.Ui.Controls.TextBox { PlaceholderText = "输入新的篮号" };

            dialog.Title = $"替换 {orderPick.BasketNumber} 号篮";
            dialog.Content = textBox;
            dialog.PrimaryButtonText = "确认替换";
            dialog.CloseButtonText = "取消";

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (int.TryParse(textBox.Text, out int newBasketNumber))
                {
                    if (OrderPickBasketList.Any(b => b.BasketNumber == newBasketNumber && b != orderPick))
                    {
                        _snackbarService.Show("错误", $"篮号 {newBasketNumber} 已存在", ControlAppearance.Danger,
                            new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(5));
                        return;
                    }

                    foreach (OrderPick thisOrderPick in OrderPickBasketList)
                    {
                        int oldBasketNumber = orderPick.BasketNumber;
                        if (thisOrderPick.BasketNumber == oldBasketNumber)
                        {
                            thisOrderPick.BasketNumber = newBasketNumber;
                            _snackbarService.Show("成功", $"{oldBasketNumber} 号篮 已替换成 {newBasketNumber} 号篮",
                                ControlAppearance.Success, new SymbolIcon(SymbolRegular.Check24),
                                TimeSpan.FromSeconds(5));
                        }
                    }

                    SaveBasketSort();
                }
                else
                {
                    _snackbarService.Show("错误", "请输入有效的篮号", ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(5));
                }
            }
        }
    }

    [RelayCommand]
    private void ClearSingleBasket(object parameter)
    {
        if (parameter is OrderPick orderPick)
        {
            ClearBasket(new List<object> { orderPick });
        }
    }

    [RelayCommand]
    private void PrintSingleDeliveryBill(object parameter)
    {
        if (parameter is OrderPick orderPick)
        {
            if (orderPick.PickCount == 0)
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "警告", Content = $"无法操作空篮子", CloseButtonText = "好的 (Esc)"
                };
                _ = messageBox.ShowDialogAsync();
            }
            else
            {
                PrintDeliveryBill(new List<object> { orderPick });
            }
        }
    }

    [RelayCommand]
    private void MoveUp(object parameter)
    {
        if (parameter is OrderPick orderPick)
        {
            var oldIndex = OrderPickBasketList.IndexOf(orderPick);
            if (oldIndex > 0)
            {
                OrderPickBasketList.Move(oldIndex, oldIndex - 1);
                SaveBasketSort();
            }
        }
    }

    [RelayCommand]
    private void MoveDown(object parameter)
    {
        if (parameter is OrderPick orderPick)
        {
            var oldIndex = OrderPickBasketList.IndexOf(orderPick);
            if (oldIndex < OrderPickBasketList.Count - 1)
            {
                OrderPickBasketList.Move(oldIndex, oldIndex + 1);
                SaveBasketSort();
            }
        }
    }
    
    [RelayCommand]
    private void AdjustSingleSortQuantity(object parameter)
    {
        if (parameter is OrderPick orderPick)
        {
            if (orderPick.PickCount == 0)
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "警告", Content = $"无法操作空篮子", CloseButtonText = "好的 (Esc)"
                };
                _ = messageBox.ShowDialogAsync();
            }
            else
            {
                AdjustSortQuantity(new List<object> { orderPick });
            }
        }
    }

    [RelayCommand]
    private void ConfirmSingleShipment(object parameter)
    {
        if (parameter is OrderPick orderPick)
        {
            if (orderPick.PickCount == 0)
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "警告", Content = $"无法操作空篮子", CloseButtonText = "好的 (Esc)"
                };
                _ = messageBox.ShowDialogAsync();
            }
            else
            {
                SetStartDelivery(new List<object> { orderPick });
            }
        }
    }
    
    [RelayCommand]
    private void ToggleSelectAll(object parameter)
    {
        if (parameter is bool isChecked)
        {
            foreach (var item in OrderPickBasketList)
            {
                item.IsSelected = isChecked;
            }
        }
    }
}