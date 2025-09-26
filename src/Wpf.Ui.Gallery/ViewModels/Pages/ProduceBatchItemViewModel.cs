// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Windows.Input;
using Windows.Foundation.Metadata;
using CommunityToolkit.Mvvm.Messaging;
using Wpf.Ui.Gallery.Models;
using Wpf.Ui.Gallery.Services.Database;
using Wpf.Ui.Gallery.Table;
using Wpf.Ui.Gallery.Vo;

namespace Wpf.Ui.Gallery.ViewModels.Pages;

public sealed partial class ProduceBatchItemViewModel : ObservableObject, IRecipient<ProduceBatchNumMessage>
{
    [ObservableProperty] private string _selectedProduceBatchNumber = "";
    
    private readonly IDatabaseService _databaseService;

    private readonly ObservableCollection<ProduceBatchItemVo> _originalProductBatchCollection = new ObservableCollection<ProduceBatchItemVo>();

    [ObservableProperty] private ObservableCollection<ProduceBatchItemVo> _productBatchItemCollection;

    public ProduceBatchItemViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        // 3. 在构造函数中注册为消息接收者
        WeakReferenceMessenger.Default.Register<ProduceBatchNumMessage>(this);
    }

    // 4. 实现Receive方法，来处理接收到的消息
    public void Receive(ProduceBatchNumMessage message)
    {
        // 当收到消息时，更新属性
        SelectedProduceBatchNumber = message.ProduceBatchNumber;
        Search(message.ProduceBatchNumber);
    }

    [RelayCommand]
    private void Search(string produceBatchNumber)
    {
        if (string.IsNullOrWhiteSpace(produceBatchNumber))
        {
            ProductBatchItemCollection = new ObservableCollection<ProduceBatchItemVo>(_originalProductBatchCollection);
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "请输入需要搜索的生产批次号"
            };
            messageBox.ShowDialogAsync();
            return;
        }

        List<ProduceItemEntity> produceBatchItemList = _databaseService.GetProduceBatchItemList(produceBatchNumber);
        List<ProduceBatchItemVo> produceBatchItemVoList = new List<ProduceBatchItemVo>();
        foreach (ProduceItemEntity produceItemEntity in produceBatchItemList)
        {
            produceBatchItemVoList.Add(new ProduceBatchItemVo()
            {
                ProduceBatchNum = produceBatchNumber,
                BatchNum = produceItemEntity.BatchNum,
                OrderNo = produceItemEntity.OrderNo,
                //PatternName = produceItemEntity.PatternName,
                SkuAlias = produceItemEntity.SkuAlias,
                //PayTime = produceItemEntity.PayTime,
                ProduceImgLocalPath = produceItemEntity.ProduceImgLocalPath,
                ProduceImgName = produceItemEntity.ProduceImgName,
                ProduceBatchItemProcess = produceItemEntity.ProduceBatchItemProcess,
            });
        }

        ProductBatchItemCollection = new ObservableCollection<ProduceBatchItemVo>(produceBatchItemVoList);
    }
    
    [RelayCommand]
    private async void OnOpenProduceItem(Object? produceBatchItemVoObj)
    {
        if (produceBatchItemVoObj is ProduceBatchItemVo batchVo)
        {
            string path = batchVo.ProduceImgLocalPath;
            string fileName = batchVo.ProduceImgName;
            // 1. 健壮性检查：确保路径存在
            //    这可以防止因路径无效而导致无法预测的行为
            if (!Directory.Exists(path))
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "打开文件夹出错", Content = "生产稿件保存路径不存在 " + path, CloseButtonText = "OK"
                };
                _ = messageBox.ShowDialogAsync();
                // 可以考虑弹出一个提示框告知用户
                // MessageBox.Show($"目录不存在: {path}");
                return;
            }
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                // a. 构建文件的完整路径
                string fullFilePath = Path.Combine(path, fileName);

                // b. 检查这个特定文件是否存在
                if (File.Exists(fullFilePath))
                {
                    // c. 如果文件存在，使用 /select, 参数来打开并选中它
                    try
                    {
                        //ProcessStartInfo startInfo = new ProcessStartInfo { FileName = path, UseShellExecute = true };
                        //Process.Start(startInfo);
                        Process.Start("explorer.exe", $"/select,\"{fullFilePath}\"");
                        return; // 操作完成，直接返回
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"打开并选中文件时发生错误: {ex.Message}");
                        // 如果失败，将回退到只打开文件夹
                    }
                }
                else
                {
                    // 如果指定的文件不存在，可以给用户一个提示
                    Console.WriteLine($"警告: 尝试选中的文件不存在: {fullFilePath}");
                    // 将继续执行后续代码，只打开文件夹
                }
            }
        }
    }
}