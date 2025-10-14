// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.Picking;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Services.Downloader;
using Wpf.Ui.Gallery.Utils;
using Wpf.Ui.Gallery.Vo;

namespace Wpf.Ui.Gallery.ViewModels.Windows
{
    public partial class PrintDialogViewModel : ObservableObject
    {
        private readonly IOrderApi _orderApi; 
        private readonly LoginInfoService _loginInfoService;
        private readonly ISnackbarService _snackbarService;
        private readonly HttpClient _httpClient;
        public Action CloseWindow { get; set; }


        [ObservableProperty]
        private ObservableCollection<string> _printers = new();

        [ObservableProperty]
        private string _selectedPrinter;

        [ObservableProperty]
        private string _statusMessage = "正在获取面单信息...";

        [ObservableProperty]
        private bool _isReadyToPrint = false;

        private WaybillInfo _waybillInfo;
        
        public PrintDialogViewModel(
            IOrderApi orderApi, 
            LoginInfoService loginInfoService,
            ISnackbarService snackbarService,
            HttpClient httpClient
            )
        {
            _orderApi = orderApi;
            _loginInfoService = loginInfoService;
            _snackbarService = snackbarService;
            _httpClient = httpClient;
            LoadPrinters();
        }
        
        private void LoadPrinters()
        {
            try
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    Printers.Add(printer);
                }

                if (Printers.Any())
                {
                    var defaultPrinterSettings = new PrinterSettings();
                    SelectedPrinter = defaultPrinterSettings.PrinterName;
                    
                    if (!Printers.Contains(SelectedPrinter))
                    {
                        SelectedPrinter = Printers.First();
                    }
                }
                else
                {
                    StatusMessage = "未找到任何打印机.";
                    IsReadyToPrint = false;
                }
            }
            catch (System.Exception)
            {
                StatusMessage = "获取打印机列表时出错.";
                IsReadyToPrint = false;
            }
        }


        [RelayCommand]
        private void Print()
        {
            if (string.IsNullOrEmpty(_waybillInfo.LocalUrl) || string.IsNullOrEmpty(SelectedPrinter))
            {
                StatusMessage = "没有文件可以打印或者没有选中打印机.";
                return;
            }

            try
            {
                string extension = Path.GetExtension(_waybillInfo.LocalUrl).ToLower();
                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp")
                {
                    PrintDocument pd = new PrintDocument();
                    pd.PrinterSettings.PrinterName = SelectedPrinter;
                    pd.PrintPage += (sender, args) =>
                    {
                        System.Drawing.Image img = System.Drawing.Image.FromFile(_waybillInfo.LocalUrl);
                        args.Graphics.DrawImage(img, args.MarginBounds);
                    };
                    pd.Print();
                    StatusMessage = "打印任务已发送.";
                }
                else
                {
                    StatusMessage = "不支持的图片格式, 请手动打印.";
                    Process.Start(new ProcessStartInfo(_waybillInfo.LocalUrl) { UseShellExecute = true });
                }
            }
            catch (System.Exception e)
            {
                StatusMessage = $"打印出错: {e.Message}";
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseWindow?.Invoke();
        }

        public async Task FetchAndDownloadWaybill(OrderPick orderPick)
        {
            try
            {
                string token = _loginInfoService.getToken();
                FactoryApiResponse<Object> response = await _orderApi.getOrderExpressInfoByOrderCode(new OrderCodeRequest() { OrderCode = orderPick.OrderCode }, token);

                if (response.IsSuccess && response?.Data is not null)
                {
                    OrderWaybillVo orderWaybillVo = JsonSerializer.Deserialize<OrderWaybillVo>(response.Data.ToString());
                    string waybillUrl = orderWaybillVo.ExpressWaybillUrl;
                    StatusMessage = "正在下载面单...";
                    string downloadPath = FileName.getOrderExpressWaybillPath(orderWaybillVo.FactoryId);
                    string fileExtension = Path.GetExtension(waybillUrl);
                    string fileName = $"Order-{orderWaybillVo.OrderNo}.{fileExtension}";
                    string fullFilePath = Path.Combine(downloadPath, fileName);
                    if (!File.Exists(fullFilePath))
                    {
                        if (!Directory.Exists(downloadPath))
                        {
                            Directory.CreateDirectory(downloadPath);
                        }
                        await using var memoryStream = new MemoryStream();
                        await using (var networkStream = await _httpClient.GetStreamAsync(NetworkHelper.ParseUrl(waybillUrl)))
                        {
                            // 将网络流的所有内容异步复制到内存流
                            await networkStream.CopyToAsync(memoryStream);
                        }
                        if (memoryStream.Length == 0)
                        {
                            // 下载失败 下载0字节
                            AudioPlayer.PlayErrorAudio();
                            var messageBox = new Wpf.Ui.Controls.MessageBox
                            {
                                Title = "警告", Content = $"订单[{orderWaybillVo.OrderNo}]发货面单下载失败", CloseButtonText = "好的 (Esc)"
                            };
                            _ = messageBox.ShowDialogAsync();
                            return ;
                        }
                        else
                        {
                            memoryStream.Position = 0;
                            await using (var fileStream = new FileStream(fullFilePath, FileMode.Create,
                                             FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true))
                            {
                                // 将内存流的所有内容异步复制到文件流
                                await memoryStream.CopyToAsync(fileStream);
                                StatusMessage = "发货面单下载成功";
                                orderPick.WaybillInfo = new WaybillInfo()
                                {
                                    OrderCode = orderPick.OrderCode,
                                    OrderNo = orderWaybillVo.OrderNo,
                                    Url = waybillUrl,
                                    LocalUrl = fullFilePath,
                                    IsPrint = false
                                };
                            }
                        }
                    }
                    else
                    {
                        orderPick.WaybillInfo = new WaybillInfo()
                        {
                            OrderCode = orderPick.OrderCode,
                            OrderNo = orderWaybillVo.OrderNo,
                            Url = waybillUrl,
                            LocalUrl = fullFilePath,
                            IsPrint = false
                        };
                    }
                    _waybillInfo = orderPick.WaybillInfo;
                    // _filePathToPrint = await _fileDownloader.DownloadFileAsync(url, downloadPath, orderNo);
                    _snackbarService.Show(
                        "打印发货单",
                        $"正在打印单号: {string.Join(", ", orderWaybillVo.OrderNo)}的面单",
                        ControlAppearance.Success,
                        new SymbolIcon(SymbolRegular.Print24),
                        TimeSpan.FromSeconds(5)
                    );
                    if (File.Exists(fullFilePath))
                    {
                        StatusMessage = waybillUrl;
                        IsReadyToPrint = true;
                    }
                    else
                    {
                        StatusMessage = "下载面单失败.";
                    }
                }
                else
                {
                    StatusMessage = response?.Msg;
                }
            }
            catch (System.Exception e)
            {
                StatusMessage = $"获取面单时出错: {e.Message}";
            }
        }
    }
}
