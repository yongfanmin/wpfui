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
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Converters;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Dto.PrintTask;
using Wpf.Ui.Gallery.ImageProcessor;
using Wpf.Ui.Gallery.LocalConfig;
using Wpf.Ui.Gallery.Services.Creator;
using Wpf.Ui.Gallery.Services.Database;
using Wpf.Ui.Gallery.Table;
using Wpf.Ui.Gallery.Utils;

namespace Wpf.Ui.Gallery.ViewModels.Windows
{
    public partial class CreatePrintTaskViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;


        [ObservableProperty] private ObservableCollection<string> _produceBatchNumbers = new();

        public string ProduceBatchNumbersText => string.Join(" | ", ProduceBatchNumbers);

        [ObservableProperty] private string _destinationFolder = string.Empty;

        [ObservableProperty] private double _copyProgress = 0;

        [ObservableProperty] private bool _isExecuting = false;

        [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartBatchPrintCommand))]
        private bool _isPrintButtonEnabled = false;

        // 转成CYMK
        [ObservableProperty] private bool _isConvertToCmyk = true;

        //白墨烫画专用工艺 [CMYK+内缩2px的专色通道]
        [ObservableProperty] private bool _isWhiteInkSpot = false;

        [ObservableProperty] private OutputFormat _outputFormatOb = OutputFormat.Png;

        [ObservableProperty] private LayoutOption _layoutOptionOb = LayoutOption.Automatic;


        public CreatePrintTaskViewModel(IEnumerable<string> produceBatchNumbers, IDatabaseService databaseService)
        {
            ProduceBatchNumbers = new ObservableCollection<string>(produceBatchNumbers);
            _databaseService = databaseService;
            DestinationFolder = LocalAppConfig.AppSetting.PrintTaskDestinationFolder;
        }


        public class CopyFile
        {
            public string SourceFile { get; set; }
            public string UniFileName { get; set; }
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

            // 打印任务配置
            PrintTaskConfig printTaskConfig = new PrintTaskConfig()
            {
                OutputFormat = OutputFormatOb, LayoutOption = LayoutOptionOb
            };

            IsExecuting = true;
            CopyProgress = 0;

            var allFilesToCopy = new List<CopyFile>();
            foreach (var produceBatchNumber in ProduceBatchNumbers)
            {
                List<ProduceItemEntity> items = _databaseService.GetProduceItemList(produceBatchNumber);
                foreach (var produceItemEntity in items)
                {
                    if (!string.IsNullOrEmpty(produceItemEntity.ProduceBatchDetail))
                    {
                        if (!string.IsNullOrEmpty(produceItemEntity.ProduceImgLocalPath))
                        {
                            UniqueBatchItem uniqueBatchItem =
                                JsonSerializer.Deserialize<UniqueBatchItem>(produceItemEntity.ProduceBatchDetail);
                            if (uniqueBatchItem.ProductionTasks.Count == 1)
                            {
                                allFilesToCopy.Add(new CopyFile()
                                {
                                    SourceFile = produceItemEntity.ProduceImgLocalPath + produceItemEntity.ProduceImgName,
                                    UniFileName = $"{produceItemEntity.ProduceBatchNum}-{produceItemEntity.Color}-{produceItemEntity.Size}-{produceItemEntity.SkuAlias}-{produceItemEntity.OrderDetailId}"
                                });
                            }
                            else
                            {
                                // 多任务 (代表着 多印花面)
                                foreach (ProductionTask productionTask in uniqueBatchItem.ProductionTasks)
                                {
                                    allFilesToCopy.Add(new CopyFile()
                                    {
                                        SourceFile = produceItemEntity.ProduceImgLocalPath + $"{productionTask.ViewId}-{productionTask.ViewName}{ImgFormat2Extend.GetExtend(ImgSupportFormat.Png)}",
                                        UniFileName = $"{produceItemEntity.ProduceBatchNum}-{produceItemEntity.Color}-{produceItemEntity.Size}-{produceItemEntity.SkuAlias}-{produceItemEntity.OrderDetailId}"
                                    });
                                }
                            }
                        }
                    }
                }
            }


            int machinePrintWidthMm = 600;
            // 打印机安全边缘 (左右各 ? 毫米)
            int machinePrintSafeEdgeMm = 10;
            int printerDpi = 300;
            // 印花图安全间距( ? 毫米) 用于裁剪
            int printImgPaddingMm = 5;

            // 可安全排版的宽度
            int machineLayoutSafeWidthMm = machinePrintWidthMm - (machinePrintSafeEdgeMm * 2) + (printImgPaddingMm * 2);
            // 出血位 (印刷机器 左右两侧夹具占用空间 无法印刷的宽度)
            int safeEdgeWithoutPaddingMm = machinePrintSafeEdgeMm - printImgPaddingMm;

            string targetPath = Path.Combine(DestinationFolder, string.Join("--", ProduceBatchNumbers));
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            if (printTaskConfig.IsNeedLayout())
            {
                // 需要排版 就先读取原来的图片 然后排版 再进行格式转换
                if (allFilesToCopy.Count > 0)
                {
                    LayoutResult layoutResult = StripPackingLayout.SkylineLayout(allFilesToCopy.Select(item=>item.SourceFile).ToList(),
                        (uint)ImageHelper.ConvertMmToPixels(machineLayoutSafeWidthMm, printerDpi),
                        ImageHelper.ConvertMmToPixels(printImgPaddingMm, printerDpi));
                    // 创建排版画布 将排版数据换成印花图排版到 画布上
                    ProduceImageProcessor.CreateLayoutTiffFromPxSize(layoutResult,
                        Path.Combine(targetPath, Path.GetFileName(FileName.getLayoutTargetName(ProduceBatchNumbers))),
                        safeEdgeWithoutPaddingMm, printerDpi, printTaskConfig);
                }
                else
                {
                    throw new Exception("不存在需要排版的印花图");
                }
            }
            else
            {
                // 不需要排版 复制原来的印花到打印目录 如果需要格式转换的情况 复制的时候再转换
                //TODO 缺少格式转换
                for (int i = 0; i < allFilesToCopy.Count; i++)
                {
                    var sourcePath = allFilesToCopy[i].SourceFile;
                    var destinationPath = Path.Combine(targetPath, FileName.getLayoutTargetName(ProduceBatchNumbers, allFilesToCopy[i].UniFileName, Path.GetFileName(sourcePath)));
                    await Task.Run(() => File.Copy(sourcePath, destinationPath, true));
                    CopyProgress = (double)(i + 1) / allFilesToCopy.Count * 100;
                }
            }
            // 自动排版 输入需要排版的图片 与 机器打印宽度  (实际都是毫米 但是计算库只支持 无符号整数 所以按照像素排版 然后再转成毫米)


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
}