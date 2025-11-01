// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Text.Json;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.ImageProcessor;
using Wpf.Ui.Gallery.LocalConfig;
using Wpf.Ui.Gallery.Services.Database;
using Wpf.Ui.Gallery.Table;
using Wpf.Ui.Gallery.Utils;

namespace Wpf.Ui.Gallery.ViewModels.Pages;

public partial class CreatePrintTaskViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private ObservableCollection<string> _produceBatchNumbers = new();

    public string ProduceBatchNumbersText => string.Join(" | ", ProduceBatchNumbers);

    [ObservableProperty]
    private string _destinationFolder = string.Empty;

    [ObservableProperty]
    private double _copyProgress = 0;

    [ObservableProperty]
    private bool _isExecuting = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartBatchPrintCommand))]
    private bool _isPrintButtonEnabled = false;

    public CreatePrintTaskViewModel(IEnumerable<string> produceBatchNumbers, IDatabaseService databaseService)
    {
        ProduceBatchNumbers = new ObservableCollection<string>(produceBatchNumbers);
        _databaseService = databaseService;
        DestinationFolder = LocalAppConfig.AppSetting.PrintTaskDestinationFolder;
    }
    

    [RelayCommand]
    private void OnSelectFolder()
    {
        var openFolderDialog = new Microsoft.Win32.OpenFolderDialog();
        if (openFolderDialog.ShowDialog() == true)
        {
            DestinationFolder = openFolderDialog.FolderName;
            LocalAppConfig.AppSetting.PrintTaskDestinationFolder = DestinationFolder;
            LocalAppConfig.Save(LocalAppConfig.AppSetting);
        }
    }

    // 执行印花图归集 合批打印  执行转换格式 png->TIFF(CMYK)->移动文件到打印文件夹
    [RelayCommand]
    private async Task OnConvertFormat2print()
    {
        if (string.IsNullOrEmpty(DestinationFolder) || !Directory.Exists(DestinationFolder))
        {
            // Ideally, show a message to the user
            return;
        }

        IsExecuting = true;
        CopyProgress = 0;

        var allFilesToCopy = new List<string>();
        foreach (var produceBatchNumber in ProduceBatchNumbers)
        {
            List<ProduceItemEntity> items = _databaseService.GetProduceItemList(produceBatchNumber);
            foreach (var produceItemEntity in items)
            {
                if (!string.IsNullOrEmpty(produceItemEntity.ProduceBatchDetail))
                {
                    if (!string.IsNullOrEmpty(produceItemEntity.ProduceImgLocalPath))
                    {
                        foreach (string file in FileHelper.GetAllFiles(produceItemEntity.ProduceImgLocalPath))
                        {
                            allFilesToCopy.Add(file);
                        }
                    }
                }
            }
        }

        for (int i = 0; i < allFilesToCopy.Count; i++)
        {
            var sourcePath = allFilesToCopy[i];
            var destinationPath = Path.Combine(DestinationFolder, Path.GetFileName(sourcePath));
            // await Task.Run(() => File.Copy(sourcePath, destinationPath, true));
            ProduceImageProcessor.ConvertToCmykWithSpotColor(sourcePath, destinationPath);
            CopyProgress = (double)(i + 1) / allFilesToCopy.Count * 100;
        }

        IsExecuting = false;
        IsPrintButtonEnabled = true;
    }
    

    [RelayCommand(CanExecute = nameof(CanStartBatchPrint))]
    private void OnStartBatchPrint()
    {
        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = "警告", Content = $"无法连接打印机,请手动打印", CloseButtonText = "好的 (Esc)"
        };
        _ = messageBox.ShowDialogAsync();
        // Logic to start batch printing will go here.
    }

    private bool CanStartBatchPrint()
    {
        return IsPrintButtonEnabled;
    }
}
