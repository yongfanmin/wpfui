// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Text.Json;
using NetVips;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Dto.PrintTask;
using Wpf.Ui.Gallery.ImageProcessor;
using Wpf.Ui.Gallery.LocalConfig;
using Wpf.Ui.Gallery.Services.Creator;
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
    
    // 转成CYMK
    [ObservableProperty]
    private bool _isConvertToCmyk = true;

    //白墨烫画专用工艺 [CMYK+内缩2px的专色通道]
    [ObservableProperty]
    private bool _isWhiteInkSpot = false;
    

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
        PrintTaskConfig printTaskConfig = new PrintTaskConfig()
        {
            ToCymk = IsConvertToCmyk,
            ToWhiteInkSpot = IsWhiteInkSpot,
        };
        
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

        List<LayoutImg> printImgList = new List<LayoutImg>();
        decimal machinePrintWidthMm = 600;
        int printerDpi = 300;
        
        
        for (int i = 0; i < allFilesToCopy.Count; i++)
        {
            var sourcePath = allFilesToCopy[i];
            var destinationPath = Path.Combine(DestinationFolder, Path.GetFileName(sourcePath));
            if (printTaskConfig.IsNeedProcess())
            {
                LayoutImg layoutImg = await ProduceImageProcessor.PrintTaskImgProcess(sourcePath, destinationPath,printTaskConfig);
                layoutImg.Id = i;
                printImgList.Add(layoutImg);
            }
            else
            {
                await Task.Run(() => File.Copy(sourcePath, destinationPath, true));
            }
            CopyProgress = (double)(i + 1) / allFilesToCopy.Count * 100;
        }
        
        // 自动排版 输入需要排版的图片 与 机器打印宽度  (实际都是毫米 但是计算库只支持 无符号整数 所以按照像素排版 然后再转成毫米)
        if (printImgList.Count > 0)
        {
            LayoutResult layoutResult = StripPackingLayout.SkylineLayout(printImgList, (uint)ImageHelper.ConvertMmToPixels(machinePrintWidthMm, printerDpi));
            // 创建排版画布 将排版数据换成印花图排版到 画布上
            ProduceImageProcessor.CreateLayoutTiffFromPxSize(layoutResult,Path.Combine(DestinationFolder, Path.GetFileName("layout.tif")), printerDpi);
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
