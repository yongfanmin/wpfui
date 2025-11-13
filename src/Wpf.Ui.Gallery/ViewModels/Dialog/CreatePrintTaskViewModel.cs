// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Text.Json;
using DataJuggler.RealESRGAN;
using DataJuggler.RealESRGAN.Enumerations;
using NetVips;
using Wpf.Ui.Controls;
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
using Wpf.Ui.Gallery.ViewModels.Dialog;
using Wpf.Ui.Gallery.Views.Dialog;
using Image = NetVips.Image;

namespace Wpf.Ui.Gallery.ViewModels.Windows
{
    public partial class CreatePrintTaskViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;

        private readonly IContentDialogService _contentDialogService;

        [ObservableProperty] private ObservableCollection<string> _produceBatchNumbers = new();

        public string ProduceBatchNumbersText => string.Join(" | ", ProduceBatchNumbers);

        [ObservableProperty] private string _destinationFolder = string.Empty;

        [ObservableProperty] private double _copyProgress = 0;

        [ObservableProperty] private bool _isExecuting = false;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartBatchPrintCommand))]
        private bool _isPrintButtonEnabled = false;

        // 转成CYMK
        [ObservableProperty] private bool _isConvertToCmyk = true;

        //白墨烫画专用工艺 [CMYK+内缩2px的专色通道]
        [ObservableProperty] private bool _isWhiteInkSpot = false;

        [ObservableProperty] private OutputFormat _outputFormatOb = OutputFormat.Png;

        [ObservableProperty] private LayoutOption _layoutOptionOb = LayoutOption.Automatic;

        partial void OnOutputFormatObChanged(OutputFormat value)
        {
            LocalAppConfig.AppSetting.PrintTaskConfig.OutputFormat = value;
            LocalAppConfig.Save(LocalAppConfig.AppSetting);
        }

        public CreatePrintTaskViewModel(IEnumerable<string> produceBatchNumbers, IDatabaseService databaseService,
            IContentDialogService contentDialogService)
        {
            ProduceBatchNumbers = new ObservableCollection<string>(produceBatchNumbers);
            _databaseService = databaseService;
            _contentDialogService = contentDialogService;
            DestinationFolder = LocalAppConfig.AppSetting.PrintTaskDestinationFolder;
            OutputFormatOb = LocalAppConfig.AppSetting.PrintTaskConfig.OutputFormat;
        }


        public class CopyFile
        {
            public string SourceFile { get; set; }
            public string UniFileName { get; set; }
            
            public OrderTrackInfo OrderTrackInfo { get; set; }
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

        public event Func<Task>? ShowSettingsDialogRequested;

        [RelayCommand]
        private async Task OpenSettingsDialog()
        {
            if (ShowSettingsDialogRequested != null)
            {
                await ShowSettingsDialogRequested.Invoke();
            }
        }

        // 无法正确处理模式为索引的图 只能处理RGB图  (CMYK模式也不兼容)
        // 执行印花图归集 合批打印  执行转换格式 png->TIFF(CMYK)->移动文件到打印文件夹
        [RelayCommand]
        private async Task OnConvertFormat2print()
        {
            if (string.IsNullOrEmpty(DestinationFolder) || !Directory.Exists(DestinationFolder))
            {
                // Ideally, show a message to the user
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "文件夹不存在", Content = "请先选择一个文件夹用于存放即将印刷的印花生产图", CloseButtonText = "好的"
                };

                _ = await messageBox.ShowDialogAsync();
                return;
            }

            // 打印任务配置
            PrintTaskConfig printTaskConfig = new PrintTaskConfig()
            {
                OutputFormat = OutputFormatOb, LayoutOption = LayoutOptionOb
            };

            IsExecuting = true;
            CopyProgress = 0;
            string targetPath = Path.Combine(DestinationFolder, string.Join("--", ProduceBatchNumbers));
            // 异步运行 防止UI线程卡死
            await Task.Run(async () =>
            {
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
                                // 多任务 (代表着 多印花面)
                                /*foreach (ProductionTask productionTask in uniqueBatchItem.ProductionTasks)
                                {
                                    allFilesToCopy.Add(new CopyFile()
                                    {
                                        SourceFile =
                                            produceItemEntity.ProduceImgLocalPath +
                                            $"{productionTask.ViewId}-{productionTask.ViewName}{ImgFormat2Extend.GetExtend(ImgSupportFormat.Png)}",
                                        UniFileName =
                                            $"{produceItemEntity.ProduceBatchNum}-{produceItemEntity.Color}-{produceItemEntity.Size}-{produceItemEntity.SkuAlias}-{produceItemEntity.OrderDetailId}"
                                    });
                                }*/
                                allFilesToCopy.Add(new CopyFile()
                                {
                                    OrderTrackInfo = new OrderTrackInfo()
                                    {
                                        OrderNo = produceItemEntity.OrderNo,
                                        OrderDetailId = produceItemEntity.OrderDetailId,
                                        ProductId = uniqueBatchItem.ProductId,
                                        BuyIndex = uniqueBatchItem.BuyIndex,
                                        ProductName = uniqueBatchItem.ProductName,
                                        SkuAlias = produceItemEntity.SkuAlias,
                                        SkuInfo = $"{produceItemEntity.Size} - {produceItemEntity.Color}"
                                    },
                                    SourceFile =
                                        produceItemEntity.ProduceImgLocalPath +
                                        produceItemEntity.ProduceImgName,
                                    UniFileName =
                                        $"{produceItemEntity.ProduceBatchNum}-{produceItemEntity.Color}-{produceItemEntity.Size}-{produceItemEntity.SkuAlias}-{produceItemEntity.OrderDetailId}"
                                });
                            }
                        }
                    }
                }

                // 使用 GroupBy 和 ToDictionary 进行分组  按照ProductId(成品id进行分组)
                /*Dictionary<long, List<CopyFile>> result = allFilesToCopy
                    .GroupBy(file => file.ProductId)
                    .ToDictionary(group => group.Key, group => group.ToList());*/

                // 先按照成品id分类 再按照 单件分类(同件印花图归类)
                Dictionary<long, Dictionary<long, List<CopyFile>>> groupByProductIdAndBuyIndex = allFilesToCopy
                    .GroupBy(file => file.OrderTrackInfo.ProductId) // 第一级分组：按照 ProductId
                    .ToDictionary(
                        outerGroup => outerGroup.Key, // 外层字典的键：ProductId
                        outerGroup => outerGroup
                            .GroupBy(file => file.OrderTrackInfo.BuyIndex) // 第二级分组：对第一级分组内的元素，按照 BuyIndex
                            .ToDictionary(
                                innerGroup => innerGroup.Key, // 内层字典的键：BuyIndex
                                innerGroup => innerGroup.ToList() // 内层字典的值：该 BuyIndex 下的 CopyFile 列表
                            )
                    );

                int machinePrintWidthMm = LocalAppConfig.AppSetting.PrintTaskConfig.MachinePrintWidthMm;
                // 打印机安全边缘 (左右各 ? 毫米)
                int machinePrintSafeEdgeMm = LocalAppConfig.AppSetting.PrintTaskConfig.MachinePrintSafeEdgeMm;
                // 打印机打印精度
                int printerDpi = LocalAppConfig.AppSetting.PrintTaskConfig.MachineDpi;
                // 印花图安全间距( ? 毫米) 用于裁剪
                int printImgPaddingMm = LocalAppConfig.AppSetting.PrintTaskConfig.PrintImgPaddingMm;

                // 可安全排版的宽度
                int machineLayoutSafeWidthMm =
                    machinePrintWidthMm - (machinePrintSafeEdgeMm * 2) + (printImgPaddingMm * 2);
                // 出血位 (印刷机器 左右两侧夹具占用空间 无法印刷的宽度)
                int safeEdgeWithoutPaddingMm = machinePrintSafeEdgeMm - printImgPaddingMm;


                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }


                List<LayoutImg> printImgList = new List<LayoutImg>();
                int id = 0;
                foreach (CopyFile copyFile in allFilesToCopy)
                {
                    using Image image = Image.NewFromFile(copyFile.SourceFile);
                    if (image.HasAlpha())
                    {
                        //只裁切出不透明部分 默认不开启 [不方便因为在衣服上对位 ] 只有小作坊不怎么考虑人力的情况下才可能启用? 或者有先进的解决方案, 比如投影对位?
                        if (!LocalAppConfig.AppSetting.PrintTaskConfig.IsWhiteInkCropTransparent)
                        {
                            printImgList.Add(new LayoutImg()
                            {
                                WidthPx = (uint)image.Width,
                                HeightPx = (uint)image.Height,
                                Id = id++,
                                ImgPath = copyFile.SourceFile,
                                LayoutCropImg = image.Copy(),
                                OrderTrackInfo = copyFile.OrderTrackInfo
                            });
                        }
                        else
                        {
                            object[] trimResult = image.FindTrim();

                            // FindTrim 返回一个 object[] { left, top, width, height }
                            // 我们需要将它们转换为正确的类型 (通常是 int 或 long)
                            int left = Convert.ToInt32(trimResult[0]);
                            int top = Convert.ToInt32(trimResult[1]);
                            int width = Convert.ToInt32(trimResult[2]);
                            int height = Convert.ToInt32(trimResult[3]);

                            if (width == 0 || height == 0)
                            {
                                Console.WriteLine("图片内容为空（完全透明）。");
                            }

                            Console.WriteLine($"内容边界框: X={left}, Y={top}, 宽度={width}, 高度={height}");
                            // --- 第二步: Crop ---
                            Image croppedImage = image.Crop(left, top, width, height);
                            printImgList.Add(new LayoutImg()
                            {
                                WidthPx = (uint)croppedImage.Width,
                                HeightPx = (uint)croppedImage.Height,
                                Id = id++,
                                ImgPath = copyFile.SourceFile,
                                LayoutCropImg = croppedImage,
                                OrderTrackInfo = copyFile.OrderTrackInfo
                            });
                        }
                    }
                    else
                    {
                        printImgList.Add(new LayoutImg()
                        {
                            WidthPx = (uint)image.Width,
                            HeightPx = (uint)image.Height,
                            Id = id++,
                            ImgPath = copyFile.SourceFile,
                            LayoutCropImg = image.Copy(),
                            OrderTrackInfo = copyFile.OrderTrackInfo
                        });
                    }
                }

                foreach (LayoutImg layoutImg in printImgList)
                {
                    // 加入 跟踪码信息 默认加入跟踪码
                    if (LocalAppConfig.AppSetting.PrintTaskConfig.IsOrderTrack)
                    {
                        OrderTrackConfig orderTrackConfig = LocalAppConfig.AppSetting.PrintTaskConfig.OrderTrackConfig;
                        
                        // 2. 创建一个不透明的黑色Banner
                        using(Image orderTrackBannerTransparent = Image.Black(
                                  layoutImg.LayoutCropImg.Width,
                                  ImageHelper.ConvertMmToPixels(orderTrackConfig.HeightMm + orderTrackConfig.QrCodeBorderMm * 2, printerDpi),
                                  bands: 4 // <-- 关键：直接请求4个通道
                              ))
                            
                        // 创建二维码写入单号
                        using(Image qrCode = ImageHelper.GenerateQrCodeWithBorder(
                            layoutImg.OrderTrackInfo.OrderNo, 
                            ImageHelper.ConvertMmToPixels(
                                orderTrackConfig.HeightMm, printerDpi),
                            ImageHelper.ConvertMmToPixels(
                                orderTrackConfig.HeightMm, printerDpi
                            ),
                            ImageHelper.ConvertMmToPixels(
                                orderTrackConfig.QrCodeBorderMm, printerDpi
                            )))
                        // 然后附加一个值为255的常量波段作为Alpha通道，使其变为不透明
                        using (Image orderTrackBannerOpaque = orderTrackBannerTransparent.Copy(interpretation: Enums.Interpretation.Srgb))
                        // 确保色彩空间解释正确
                        using (Image orderTrackBanner =
                               orderTrackBannerOpaque.Copy(interpretation: Enums.Interpretation.Srgb))
                        {
                            // 是空白区域的最左边坐标
                            int emptyPositionX = 0;
                            // 将二维码叠加在不透明的Banner上
                            // 计算x坐标以实现右对齐
                            int qrCodeX = orderTrackBanner.Width - qrCode.Width;
                            var orderTrackBannerWithInfo = orderTrackBanner.Composite(
                                qrCode,
                                Enums.BlendMode.Over,
                                x: qrCodeX,
                                y: 0
                            );
                            
                            // 创建左边块 (商品名称 大写)
                            using (Image productNameImg = ImageHelper.CreateTextImage(
                                       layoutImg.OrderTrackInfo.ProductName,
                                       ImageHelper.ConvertPixelsToMm(
                                           Convert.ToInt32(orderTrackBanner.Width * LocalAppConfig.AppSetting
                                               .PrintTaskConfig.OrderTrackConfig.ProductNameInBannerWidthRatio),
                                           printerDpi),
                                       Convert.ToInt32(
                                           LocalAppConfig.AppSetting.PrintTaskConfig.OrderTrackConfig.HeightMm +
                                           LocalAppConfig.AppSetting.PrintTaskConfig.OrderTrackConfig.QrCodeBorderMm *
                                           2)))
                            {
                                
                                orderTrackBannerWithInfo = orderTrackBannerWithInfo.Composite(productNameImg,
                                    Enums.BlendMode.Over,
                                    x: 0,
                                    y: 0
                                );
                                emptyPositionX += productNameImg.Width;
                            }
                            // 创建印花位置指示箭头
                            using (Image arrowImg = ImageHelper.ScaleImageToHeight(
                                       Image.NewFromFile(FileName.getPrintImgTargetArrow()),
                                       LocalAppConfig.AppSetting.PrintTaskConfig.OrderTrackConfig.HeightMm + LocalAppConfig.AppSetting.PrintTaskConfig.OrderTrackConfig.QrCodeBorderMm * 2,
                                       printerDpi))
                            {
                                orderTrackBannerWithInfo = orderTrackBannerWithInfo.Composite(arrowImg,
                                    Enums.BlendMode.Over,
                                    x: emptyPositionX,
                                    y: 0
                                );
                                emptyPositionX += arrowImg.Width;
                            }
                            //创建 左2块 (商品成品图)

                            // 创建 跟踪条 中间模块 (单号 + 仓位 + SKU + 此单总件数)
                            using (Image orderNo = ImageHelper.CreateTextImage(
                                       layoutImg.OrderTrackInfo.OrderNo,
                                       ImageHelper.ConvertPixelsToMm(
                                           Convert.ToInt32(orderTrackBanner.Width * LocalAppConfig.AppSetting
                                               .PrintTaskConfig.OrderTrackConfig.ProductInfoInBannerWidthRatio),
                                           printerDpi),
                                       Convert.ToInt32(
                                           LocalAppConfig.AppSetting.PrintTaskConfig.OrderTrackConfig.HeightMm +
                                           LocalAppConfig.AppSetting.PrintTaskConfig.OrderTrackConfig.QrCodeBorderMm *
                                           2)/2))
                            {
                                using (Image skuImg = ImageHelper.CreateTextImage(
                                           layoutImg.OrderTrackInfo.SkuInfo,
                                           ImageHelper.ConvertPixelsToMm(
                                               Convert.ToInt32(orderTrackBanner.Width * LocalAppConfig.AppSetting
                                                   .PrintTaskConfig.OrderTrackConfig.ProductInfoInBannerWidthRatio),
                                               printerDpi),
                                           Convert.ToInt32(
                                               LocalAppConfig.AppSetting.PrintTaskConfig.OrderTrackConfig.HeightMm +
                                               LocalAppConfig.AppSetting.PrintTaskConfig.OrderTrackConfig.QrCodeBorderMm *
                                               2)/2))
                                {
                                    using (Image orderNoWithSku = orderNo.Join(
                                               skuImg,
                                               Enums.Direction.Vertical,
                                               expand: true,
                                               align: Enums.Align.Centre
                                               // shim: ImageHelper.ConvertMmToPixels(2, printerDpi),
                                               // background: new double[] { 255, 255, 255 }
                                           ))
                                    {
                                        orderTrackBannerWithInfo = orderTrackBannerWithInfo.Composite(orderNoWithSku,
                                            Enums.BlendMode.Over,
                                            x: emptyPositionX,
                                            y: 0
                                        );
                                        emptyPositionX += orderNoWithSku.Width;
                                    }
                                }
                            }
                            

                            // 4. 将最终的Banner拼接到印花图底部
                            // (您的原始代码，保持不变, 但请注意使用using管理内存)
                            // 假设 layoutImg.LayoutCropImg 是需要被替换的
                            using var originalImg = layoutImg.LayoutCropImg;
                            // JOIN会让 alpha通道 发生一个叫 "通道预乘" 的优化, 导致这时把透明通道输出成png查看 会偏黑
                            layoutImg.LayoutCropImg = originalImg.Join(
                                orderTrackBannerWithInfo,
                                Enums.Direction.Vertical,
                                expand: true,
                                align: Enums.Align.Centre,
                                shim: ImageHelper.ConvertMmToPixels(LocalAppConfig.AppSetting.PrintTaskConfig.OrderTrackConfig.PrintPaddingMm,printerDpi)
                            );
                            // 不清楚怎么将 通道预乘 转成正常的alpha通道, 又不想写入磁盘再读取回来浪费磁盘IO， 先这样写入缓存再从缓存读取回来 也能实现通道预乘转成正常的alpha通道
                            layoutImg.LayoutCropImg = Image.NewFromBuffer(layoutImg.LayoutCropImg.WriteToBuffer(ImgFormat2Extend.GetExtend(ImgSupportFormat.Png)));
                        }
                    }

                    if (printImgPaddingMm > 0)
                    {
                        // 给图片一个空白的边距框
                        var oldImage = layoutImg.LayoutCropImg;
                        layoutImg.LayoutCropImg = ImageHelper.AddTransparentPadding(layoutImg.LayoutCropImg,
                            ImageHelper.ConvertMmToPixels(printImgPaddingMm, printerDpi));
                        oldImage.Dispose();
                        layoutImg.WidthPx = (uint)layoutImg.LayoutCropImg.Width;
                        layoutImg.HeightPx = (uint)layoutImg.LayoutCropImg.Height;
                    }
                }

                if (printTaskConfig.IsNeedLayout())
                {
                    // 需要排版 就先读取原来的图片 然后排版 再进行格式转换
                    if (allFilesToCopy.Count > 0)
                    {
                        LayoutResult layoutResult = StripPackingLayout.SkylineLayout(
                            printImgList,
                            (uint)ImageHelper.ConvertMmToPixels(machineLayoutSafeWidthMm, printerDpi));
                        // 创建排版画布 将排版数据换成印花图排版到 画布上
                        await ProduceImageProcessor.CreateLayoutTiffFromPxSize(layoutResult,
                            Path.Combine(targetPath,
                                Path.GetFileName(FileName.getLayoutTargetName(ProduceBatchNumbers))),
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
                        var destinationPath = Path.Combine(targetPath,
                            FileName.getLayoutTargetName(ProduceBatchNumbers, allFilesToCopy[i].UniFileName,
                                Path.GetFileName(sourcePath)));

                        // 如果格式为PNG，则直接复制
                        if (printTaskConfig.OutputFormat == OutputFormat.Png)
                        {
                            await Task.Run(() => File.Copy(sourcePath, destinationPath, true));
                        }
                        else
                        {
                            // 否则，进行格式转换
                            await ConvertImageAsync(sourcePath, destinationPath, printTaskConfig);
                        }

                        CopyProgress = (double)(i + 1) / allFilesToCopy.Count * 100;
                    }
                }
                // 自动排版 输入需要排版的图片 与 机器打印宽度  (实际都是毫米 但是计算库只支持 无符号整数 所以按照像素排版 然后再转成毫米)
            });

            IsExecuting = false;
            IsPrintButtonEnabled = true;
            // 自动打开文件夹
            Process.Start("explorer.exe", $"/select,\"{targetPath}\"");
            AudioPlayer.PlayManualWaiting();
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

        public async Task ConvertImageAsync(string sourcePath, string destinationPath, PrintTaskConfig printTaskConfig)
        {
            await Task.Run(async () =>
            {
                using var image = Image.NewFromFile(sourcePath);
                var extension = "." + printTaskConfig.OutputFormat.ToString().ToLower();
                var finalPath = Path.ChangeExtension(destinationPath, extension);

                if (printTaskConfig.IsCymk())
                {
                    //await PrintTaskImgProcess(sourcePath, finalPath, printTaskConfig);
                }
                else
                {
                    image.WriteToFile(finalPath);
                }
            });
        }
    }
}