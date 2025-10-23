// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using NetVips;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Converters;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Dto.Machine;
using Wpf.Ui.Gallery.LocalConfig;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Services.Creator;
using Wpf.Ui.Gallery.Utils;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Rendering;
using Image = NetVips.Image;
using PixelFormat = System.Windows.Media.PixelFormat;

namespace Wpf.Ui.Gallery.ImageProcessor;

// 生产图处理器
// 传入 裁片信息 印花图信息 打印信息(旋转 位移 缩放 ..)
// 对印花图执行打印信息操作(旋转 位移 缩放 ..)
// 按照生产尺寸对 裁片与印花图 进行等比放大
// 位移信息可能需要按照放大信息进行等比放大处理
// 放大后的 裁片与印花图 进行叠加合成
public class ProduceImageProcessor : IProduceImageProcessor
{
    // 初始化 NetVips (在应用启动时执行一次即可)
    // NetVips.Logging.Log.Level = Enums.LogLevel.None; // 可选：关闭日志
    // NetVips.Config.Concurrency = 4; // 可选：设置并发线程数
    private readonly IImageCreator _imageCreator;

    private readonly ILayoutApi _layoutApi;

    private readonly LoginInfoService _loginInfoService;

    // 构造函数：声明自己需要一个 IImageProcessor
    public ProduceImageProcessor(
        IImageCreator imageCreator,
        ILayoutApi layoutApi,
        LoginInfoService loginInfoService
    )
    {
        _imageCreator = imageCreator;
        _layoutApi = layoutApi; // DI容器会自动提供实例
        _loginInfoService = loginInfoService;
    }

    // TODO 现在同产品多印花面 会打印N次 导致磁盘多写入N次 但生产图会覆盖 所以结果没问题 因为获取一个生产项信息的数据包含多个面信息 直接一次生成了全部生产图
    // TODO 使用Affine 一次性完整矩阵变换(缩放/旋转/位移) 似乎性能更高?
    public ProduceBatchTaskResult ProcessProductionTask(UniqueBatchItem uniqueBatchItem)
    {
        if (uniqueBatchItem.ProductionTasks.All(item => item.PrintLayers.Count == 0))
        {
            if (!uniqueBatchItem.IsMultiPiece)
            {
                Console.WriteLine($"批次号[{uniqueBatchItem.ProduceBatchNum}]项批号[{uniqueBatchItem.BatchNum}]没有任何印花图层 已跳过生产");
                return null;
            }
            // 这个写法有bug， 局部印的时候(印花单独制造的工艺) 没印花才不打印; 全印的时候, 没有印花 也需要打印出空白裁片
            /*Console.WriteLine($"批次号[{uniqueBatchItem.ProduceBatchNum}]项批号[{uniqueBatchItem.BatchNum}]没有任何印花图层 已跳过生产");
            return null;*/
        }

        bool isNeedLayout = false;
        Enums.BlendMode blendMode = Enums.BlendMode.Atop;
        // TODO 错误的数据层级 一个生产项 是 全印/局部印/局部裁剪 这类数据  应该绑定到产品上 而不是裁片上
        foreach (ProductionTask patternPieceTask in uniqueBatchItem.ProductionTasks)
        {
            Image tempCanvas = null;
            try
            {
                // 遵循SRT顺序：如果无法确认，那么按照先缩放 (Resize) -> 再旋转 (Rotate) -> 最后位移 (Embed) 的顺序
                // 公版裁片为基底作业流水线
                if (patternPieceTask.PrintCropType == PrintCropType.裁片指定印花区域裁切)
                {
                    // 直接打印出印花 比如 烫画 印花图先打印在薄膜上 ；所以不需要临时画布
                    if (patternPieceTask.PrintLayers.Count == 0)
                    {
                        // 局部印花类型 又没有印花图层 直接跳过
                        return null;
                    }
                    else
                    {
                        blendMode = Enums.BlendMode.Over;
                        //按照打印区域当作画布大小
                        tempCanvas = _imageCreator.CreateImageFromPhysicalSize(
                            decimal.ToDouble(patternPieceTask.PrintCropArea.WidthMm),
                            decimal.ToDouble(patternPieceTask.PrintCropArea.HeightMm),
                            patternPieceTask.TargetDpi,
                            ImgSupportFormat.Png,
                            backgroundColor: new double[] { 255, 255, 255, 0 }); // 透明 RGBA
                        // 算出多印花叠加需要的画布大小 [节约画布写法]
                        /*foreach (ProductionTask productionTask in patternPieceTask.PrintLayers)
                        {
    
                        }*/
                    }
                }
                else if ((patternPieceTask.PrintCropType == PrintCropType.裁片底图全印裁切) ||
                         (patternPieceTask.PrintCropType == PrintCropType.裁片满幅裁切))
                {
                    if (patternPieceTask.RenderType == RenderType.全印_叠加裁片)
                    {
                        isNeedLayout = true;
                        // 任何将被用于 Composite 操作下层的图像，都必须以 Random 模式加载，因为它需要被随机访问 ??? 待确认
                        // Enums.Access.Sequential 顺序读取不能用 因为要保存缩略图 如果用了顺序读取 保存完大图 指针会在图片末尾, 无法从头读取像素去制造缩略图
                        try
                        {
                            using Image patternPieceImg = Image.NewFromFile(
                                patternPieceTask.PatternPieceImageLocalImg.LocalUrl,
                                access: Enums.Access.Sequential);
                            tempCanvas = patternPieceImg.Resize(
                                ImageHelper.pixelSizeToPhysicalSizeNeedScale(
                                    patternPieceImg.Width,
                                    patternPieceTask.PatternPieceTargetWidthMm,
                                    patternPieceTask.TargetDpi),
                                vscale: ImageHelper.pixelSizeToPhysicalSizeNeedScale(
                                    patternPieceImg.Height,
                                    patternPieceTask.PatternPieceTargetWidthMm,
                                    patternPieceTask.TargetDpi));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                $"全印裁片合成失败 批次号:[{uniqueBatchItem.ProduceBatchNum}] 项:[{uniqueBatchItem.BatchNum}] {ex}");
                        }
                    }
                    else if (patternPieceTask.RenderType == RenderType.局部印_矩形框)
                    {
                        tempCanvas = _imageCreator.CreateImageFromPhysicalSize(
                            decimal.ToDouble(patternPieceTask.PatternPieceTargetWidthMm),
                            decimal.ToDouble(patternPieceTask.PatternPieceTargetHeightMm),
                            patternPieceTask.TargetDpi,
                            ImgSupportFormat.Png,
                            backgroundColor: new double[] { 255, 255, 255, 0 }); // 透明 RGBA
                    }
                    else
                    {
                        throw new Exception("无法处理的渲染类型【" + patternPieceTask.RenderType.ToString() + "】");
                    }
                }
                else
                {
                    throw new Exception($"未知印花裁切方式{patternPieceTask.PrintCropType}");
                }

                foreach (PrintLayerInfo patternPrintLayerTask in patternPieceTask.PrintLayers)
                {
                    try
                    {
                        using Image patternPrintImg = Image.NewFromFile(
                            patternPrintLayerTask.DesignImageLocalImg.LocalUrl,
                            access: Enums.Access.Random
                        );

                        // 1. 预处理：确保图像有Alpha通道和sRGB身份
                        using Image cleanPatternPrintImg = patternPrintImg.HasAlpha()
                            ? patternPrintImg.Colourspace(Enums.Interpretation.Srgb)
                            : patternPrintImg.AddAlpha().Copy().Colourspace(Enums.Interpretation.Srgb);
                        using Image flipImage = patternPrintLayerTask.XFlip
                            // 翻转 - 水平翻转
                            ? cleanPatternPrintImg.Flip(Enums.Direction.Horizontal)
                            // 翻转  - 垂直翻转
                            : (patternPrintLayerTask.YFlip
                                ? cleanPatternPrintImg.Flip(Enums.Direction.Vertical)
                                : cleanPatternPrintImg.Copy());

                        // 2. 缩放 (Scale)
                        using Image scalePatternPrintImg = flipImage.Resize(
                            ImageHelper.pixelSizeToPhysicalSizeNeedScale(
                                patternPrintImg.Width,
                                patternPrintLayerTask.DesignImageSizeMm.Width,
                                patternPieceTask.TargetDpi),
                            vscale: ImageHelper.pixelSizeToPhysicalSizeNeedScale(
                                patternPrintImg.Height,
                                patternPrintLayerTask.DesignImageSizeMm.Height,
                                patternPieceTask.TargetDpi));


                        double translateXPixel = ImageHelper.ConvertMmToPixels(
                            patternPrintLayerTask.TranslateX,
                            patternPieceTask.TargetDpi
                        );

                        double translateYPixel = ImageHelper.ConvertMmToPixels(
                            patternPrintLayerTask.TranslateY,
                            patternPieceTask.TargetDpi
                        );


                        double tileTranslateXPixel = 0;
                        double tileTranslateYPixel = 0;
                        // 旋转轴心 = 印花图的中心 (宽高的一半)
                        double pivotX = scalePatternPrintImg.Width / 2.0;
                        double pivotY = scalePatternPrintImg.Height / 2.0;

                        // --- [新增的平铺逻辑在这里] ---
                        Image tileImage;
                        int finalX = Convert.ToInt32(translateXPixel);
                        int finalY = Convert.ToInt32(translateYPixel);
                        if (patternPrintLayerTask.TileTool.TileType.Equals(TileType.无平铺))
                        {
                            // 如果不平铺，直接使用缩放后的图像
                            tileImage = scalePatternPrintImg.Copy();
                            pivotX = Math.Abs(translateXPixel) + (scalePatternPrintImg.Width / 2.0);
                            pivotY = Math.Abs(translateYPixel) + (scalePatternPrintImg.Height / 2.0);
                        }
                        else
                        {
                            // 如果启用了平铺，则调用平铺辅助方法 (如果平铺 画布会变化 XY轴偏移量需要重算)
                            (tileImage, double cellWidth, double cellHeight) =
                                CreateTiledBackgroundImage(
                                    scalePatternPrintImg,
                                    patternPrintLayerTask,
                                    patternPieceTask);
                            // 由于平铺背景底图扩大, 所以需要重新计算新的平铺印花图-大背景图居中的偏移量 ((小图尺寸 - 大图尺寸)/2) 除以2是因为大图和小图都是居中, 那么小图与大图宽度的差值 的一半才是实际偏移量
                            double tileBackgroundImgTranslateX = (ImageHelper.ConvertMmToPixels(
                                patternPieceTask.PatternPieceTargetWidthMm,
                                patternPieceTask.TargetDpi
                            ) - tileImage.Width) / 2.0;

                            double tileBackgroundImgTranslateY = (ImageHelper.ConvertMmToPixels(
                                patternPieceTask.PatternPieceTargetHeightMm,
                                patternPieceTask.TargetDpi
                            ) - tileImage.Width) / 2.0;
                            // netvips 平铺无法以印花图为中心进行平铺, 智能从左上角开始平铺, 所以平铺后 印花图不会在原来用户定位的位置上, 需要重新定位, 定位方式就是 [X轴偏移量(这个偏移量为原本印花图的偏移量绝对值+平铺印花图偏移量绝对值)/印花框宽度 取余 然后向X轴减少取余部分] [Y轴偏移量/印花框高度 取余 向Y轴减少取余部分]

                            /*double offsetX = (Math.Abs(tileBackgroundImgTranslateX) + Math.Abs(translateXPixel)) %
                                             cellWidth + cellWidth / 2;*/
                            double offsetX = (Math.Abs(tileBackgroundImgTranslateX) + Math.Abs(translateXPixel)) %
                                             cellWidth;
                            if (patternPrintLayerTask.TileTool.TileType.Equals(TileType.横向错位平铺))
                            {
                                // 如果是横向平铺 则 偶数行的时候 平铺开头只有半张图 位移参数需要补偿半张图(一个cell 印花图+间距)的宽度
                                offsetX +=
                                    (Math.Floor(
                                        (Math.Abs(tileBackgroundImgTranslateY) + Math.Abs(translateYPixel)) / cellHeight
                                    ) % 2 == 0
                                        ? 0
                                        : cellWidth / 2);
                            }

                            double offsetY = (Math.Abs(tileBackgroundImgTranslateY) + Math.Abs(translateYPixel)) %
                                             cellHeight;
                            if (patternPrintLayerTask.TileTool.TileType.Equals(TileType.纵向错位平铺))
                            {
                                // 如果是纵向平铺 则 偶数行的时候 平铺开头只有半张图 位移参数需要补偿半张图(一个cell 印花图+间距)的高度
                                offsetY +=
                                    (Math.Floor(
                                        (Math.Abs(tileBackgroundImgTranslateX) + Math.Abs(translateXPixel)) / cellWidth) % 2 == 0
                                        ? 0
                                        : cellHeight / 2);
                            }

                            tileTranslateXPixel = tileBackgroundImgTranslateX + offsetX;
                            tileTranslateYPixel = tileBackgroundImgTranslateY + offsetY;
                            finalX = Convert.ToInt32(tileTranslateXPixel);
                            finalY = Convert.ToInt32(tileTranslateYPixel);
                            // 旋转轴心 = |印花图平铺背景底图偏移量| + |印花图偏移量| + 印花图的中心 (宽高的一半)   这个计算出来的坐标是以平铺印花图为基准, 需要换算成以裁片底图为基准
                            // pivotX = Math.Abs(tileTranslateXPixel) + Math.Abs(translateXPixel) + (scalePatternPrintImg.Width / 2.0);
                            // pivotY = Math.Abs(tileTranslateYPixel) + Math.Abs(translateYPixel) + (scalePatternPrintImg.Height / 2.0);

                            pivotX = Math.Abs(translateXPixel) + (scalePatternPrintImg.Width / 2.0);
                            pivotY = Math.Abs(translateYPixel) + (scalePatternPrintImg.Height / 2.0);
                        }


                        Image rotatedImage = null;
                        Image imageToProcess = tileImage;
                        try
                        {
                            if (!patternPrintLayerTask.Rotation.Equals(decimal.Zero))
                            {
                                // 3. 旋转 (Rotate)
                                double rotationAngle = decimal.ToDouble(patternPrintLayerTask.Rotation);
                                rotatedImage = tileImage.Rotate(rotationAngle);
                                imageToProcess = rotatedImage;

                                // BOF 因为旋转 需要重新计算偏移量
                                // 4. 计算最终位移 (Translate) 目前前端传的XY轴偏移量 是offset_x和y, 但是经过旋转 这个值又是存在transform和gtransform, 未能一致, 所以后端自己计算了 ；以后可以统一前端计算; 计算值跟印花图的宽高 旋转角度 原来的XY轴偏移量有关
                                // a. 获取API提供的、基于“未旋转”图像的左上角目标位置
                                // b. 计算“未旋转”图像的目标中心点


                                /*double targetCenterX = translateXPixel + (tileImage.Width / 2.0);
        
                                double targetCenterY = translateYPixel + (tileImage.Height / 2.0);
        
                                // c. 旋转后的图像，其内容是居中的，所以我们获取它的尺寸
                                // d. 根据目标中心点，反向推算出旋转后图像的左上角应该放置的位置，以实现中心对齐
                                finalX = (int)Math.Round(targetCenterX - (rotatePatternPrintImg.Width / 2.0));
                                finalY = (int)Math.Round(targetCenterY - (rotatePatternPrintImg.Height / 2.0));*/


                                if (!patternPrintLayerTask.TileTool.TileType.Equals(TileType.无平铺))
                                {
                                    (double NewX, double NewY) = CalcRotateOffsetByPivot(tileImage.Width, tileImage.Height,
                                        finalX, finalY, pivotX, pivotY, rotationAngle);
                                    finalX = Convert.ToInt32(NewX);
                                    finalY = Convert.ToInt32(NewY);
                                }
                            }

                            if (patternPrintLayerTask.TileTool.TileType.Equals(TileType.无平铺))
                            {
                                double targetCenterX = translateXPixel + (tileImage.Width / 2.0);

                                double targetCenterY = translateYPixel + (tileImage.Height / 2.0);

                                // c. 旋转后的图像，其内容是居中的，所以我们获取它的尺寸
                                // d. 根据目标中心点，反向推算出旋转后图像的左上角应该放置的位置，以实现中心对齐
                                finalX = (int)Math.Round(targetCenterX - (imageToProcess.Width / 2.0));
                                finalY = (int)Math.Round(targetCenterY - (imageToProcess.Height / 2.0));
                            }

                            // EOF 因为旋转 需要重新计算偏移量
                            if (tempCanvas == null)
                            {
                                //画布为空的 直接打印出印花图即可
                                tempCanvas = imageToProcess.Copy();
                            }
                            else
                            {
                                // 5. 叠加合成
                                Image newCanvas = tempCanvas.Composite(
                                    imageToProcess,
                                    blendMode,
                                    x: finalX,
                                    y: finalY
                                );
                                if (tempCanvas != newCanvas)
                                {
                                    tempCanvas.Dispose();
                                }

                                tempCanvas = newCanvas;
                            }
                        }
                        finally
                        {
                            rotatedImage?.Dispose();
                            tileImage.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"印花裁片合成出错 {ex}");
                    }
                }

                string localOutputPath = FileName.getOrderPatternPrintImgPath(
                    patternPieceTask.ProduceBatchNum,
                    patternPieceTask.OrderNo,
                    patternPieceTask.BatchNum,
                    patternPieceTask.FactoryId,
                    0);

                Directory.CreateDirectory(localOutputPath);

                string localOutputThumbPath = FileName.getOrderPatternPrintImgThumbPath(
                    patternPieceTask.ProduceBatchNum,
                    patternPieceTask.OrderNo,
                    patternPieceTask.BatchNum,
                    patternPieceTask.FactoryId,
                    0);

                Directory.CreateDirectory(localOutputThumbPath);
                double pixelsPerMm = patternPieceTask.TargetDpi / ImageHelper.MillimetersPerInch;

                // TODO 如果是 单件手动排版 则不需要保存到磁盘,直接在内存中排版完成 只把生产排版图写入磁盘 以节约磁盘读写
                /*if (ProduceLayout.MANUAL)
                {
    
                }*/
                // 执行印花裁剪 如果有需要 根据 PrintCropType判断

                using (var cropImg = patternPieceTask.PrintCropType.Equals(PrintCropType.裁片满幅裁切)
                       ? ImageHelper.CropFromCenter(tempCanvas,
                           ImageHelper.ConvertMmToPixels(patternPieceTask.PrintCropArea.WidthMm,
                               patternPieceTask.TargetDpi),
                           ImageHelper.ConvertMmToPixels(patternPieceTask.PrintCropArea.HeightMm,
                               patternPieceTask.TargetDpi))
                       : tempCanvas?.Copy())
                {
                    if (cropImg != null)
                    {
                        try
                        {
                            using (var imageToSave = cropImg.Copy(xres: pixelsPerMm, yres: pixelsPerMm))
                            {
                                // TODO 写死了PNG格式
                                // 裁片稿件名称使用 视图id+裁片名称 (裁片名称可能为空 不能只用裁片名称)
                                string patternPieceProduceImg = localOutputPath + patternPieceTask.ViewId + "-" +
                                                                patternPieceTask.PatternPieceTitle + ".png";
                                // 使用通用的 WriteToFile，它会根据后缀名自动选择 png 保存器
                                //imageToSave.Tiffsave(localOutputPath, xres: pixelsPerMm, yres: pixelsPerMm);
                                // TODO 图片被之前打开程序锁死无法写入
                                imageToSave.Pngsave(patternPieceProduceImg);
                                patternPieceTask.PatternPieceProduceLocalImgUrl = patternPieceProduceImg;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"裁片印花无法正常合成{ex}");
                        }
                    }
                }


                // TO DO 目前缩略图只在自动排版的时候用到 如果不是自动排版 可以不创建缩略图
                // a. 计算1/10的缩放比例
                /*const double thumbnailScale = 0.1; // 1/10
                // 这样的写法 需要多一次磁盘读取 但是直接从内存裁片生产图再保存成缩略图 不知道为什么报错 无法解决
                using Image PatternPieceImg = Image.NewFromFile(PatternPieceProduceImg,
                    access: Enums.Access.Sequential);
                // b. 对最终要保存的大图（已经带DPI信息）进行高质量缩小
                using (var thumbnailImage = PatternPieceImg.Resize(thumbnailScale, kernel: Enums.Kernel.Lanczos3))
                {
                    try
                    {
                        // d. 保存缩略图
                        //    可以为缩略图设置较低的压缩质量以减小文件体积
                        thumbnailImage.Pngsave(localOutputThumbPath + patternPieceTask.PatternPieceTitle + ".png", compression: 9, interlace: true);
                        Console.WriteLine($"缩略图已保存至: {localOutputThumbPath}");
                    }
                    catch (Exception ex)
                    {
                        int X = 1;
                    }
                }*/
            }
            finally
            {
                tempCanvas?.Dispose();
            }
        }

        SaveLocalInfo saveLocalInfo = new SaveLocalInfo();
        saveLocalInfo.LocalPath = LocalAppConfig.AppSetting.GetPrintedPatternFilePathAndClassifyFolder(
            uniqueBatchItem.ProductName, uniqueBatchItem.DesignProductId, uniqueBatchItem.ProduceBatchNum,
            uniqueBatchItem.OrderNo);
        //saveLocalInfo.Name = "";
        saveLocalInfo.SetNameByFormat(LocalAppConfig.AppSetting.ProduceImgNameFormatList, uniqueBatchItem.Size,
            uniqueBatchItem.Color, uniqueBatchItem.ProductName, uniqueBatchItem.OrderDetailId);
        saveLocalInfo.ImgFormat = ImgSupportFormat.Png;
        if (isNeedLayout)
        {
            Layout(uniqueBatchItem, uniqueBatchItem.ProductionTasks, saveLocalInfo);
            // 执行排版 人工排版 自动排版 不排版 ; 如果要排版 之前可以不用保存印花裁片到磁盘
        }
        else
        {
            // 不需要排版 还需要区分生产图是单张还是多张 多张的话需要单独存放在一个文件夹 然后使用不同的命名规则 (尺寸-颜色-产品名-项批号)

            // 不是需要排版的产品
            //TODO 且不是可印刷区域裁剪的产品 直接打印印花图出来(热转印)
            int produceImgCount =
                uniqueBatchItem.ProductionTasks.Count(item => item.PatternPieceProduceLocalImgUrl is not null);
            if (produceImgCount == 1)
            {
                // 单张生产图 直接命名文件
                foreach (ProductionTask productionTask in uniqueBatchItem.ProductionTasks)
                {
                    FileHelper.CopyFileAsync(productionTask.PatternPieceProduceLocalImgUrl,
                        saveLocalInfo.LocalPath + saveLocalInfo.Name +
                        ImgFormat2Extend.GetExtend(saveLocalInfo.ImgFormat));
                }
            }
            else if (produceImgCount > 1)
            {
                // 多生产图 先建立文件夹 再 集中放置所有生产图
                saveLocalInfo.LocalPath = saveLocalInfo.LocalPath + saveLocalInfo.Name + Path.DirectorySeparatorChar;
                foreach (ProductionTask productionTask in uniqueBatchItem.ProductionTasks)
                {
                    saveLocalInfo.Name = $"{productionTask.ViewId}-{productionTask.ViewName}";
                    FileHelper.CopyFileAsync(productionTask.PatternPieceProduceLocalImgUrl,
                        saveLocalInfo.LocalPath + saveLocalInfo.Name +
                        ImgFormat2Extend.GetExtend(saveLocalInfo.ImgFormat));
                }
            }
        }

        return new ProduceBatchTaskResult()
        {
            isNeedLayout = isNeedLayout,
            saveLocalInfo = saveLocalInfo,
            ProductionTasks = uniqueBatchItem.ProductionTasks
        };
    }

    public async void Layout(UniqueBatchItem uniqueBatchItem, List<ProductionTask> productionTasks,
        SaveLocalInfo saveLocalInfo)
    {
        foreach (ProductionTask productionTask in productionTasks)
        {
            if (productionTask.DesignProductId != uniqueBatchItem.DesignProductId)
            {
                //TODO 异常
                Console.WriteLine($"同一个生产任务内出现不同产品裁片,产品ID{uniqueBatchItem.DesignProductId}");
            }
        }

        int targetDpi = uniqueBatchItem.TargetDpi;
        string token = _loginInfoService.getToken();
        // 加载裁片排版信息
        FactoryApiResponse<Object> layoutResponse = await _layoutApi.GetLayoutInfo(
            new LayoutRequest() { DesignProductId = uniqueBatchItem.DesignProductId, SizeId = uniqueBatchItem.SizeId, },
            token);
        if (layoutResponse.Data is null)
        {
            Console.WriteLine($"产品无法排版生产,产品ID{uniqueBatchItem.DesignProductId},尺码{uniqueBatchItem.SizeId}");
            return;
        }

        Layout layout = JsonSerializer.Deserialize<Layout>(layoutResponse.Data.ToString());
        if (layout is null)
        {
            Console.WriteLine($"项批号{uniqueBatchItem.BatchNum}排版信息为空");
            return;
        }

        layout.QrCode.Content = uniqueBatchItem.OrderCode;
        MachineConfig machineConfig = new MachineConfig();
        machineConfig.Dpi = targetDpi;
        machineConfig.PrintWidthMm = layout.LayoutArea.WidthMm; // 2米宽度料卷

        // TODO 当前没用到的数据 直接写死值
        RollOfFabric rollOfFabric = new RollOfFabric();
        rollOfFabric.WidthMm = 550;
        rollOfFabric.CurrentMaxLengthMm = 30 * 1000; //当前剩余30米


        LayoutClothInfo layoutClothInfo = new LayoutClothInfo();
        layoutClothInfo.WidthMm = layout.LayoutArea.WidthMm;
        layoutClothInfo.HeightMm = layout.LayoutArea.HeightMm;

        List<PatternPieceLayout> patternPieceLayouts = new List<PatternPieceLayout>();
        foreach (PatternPiecePosition patternPiecePosition in layout.PatternPiecePositionList)
        {
            try
            {
                PatternPieceLayout patternPiece = new PatternPieceLayout();
                patternPiece.Rotation = patternPiecePosition.Rotate;
                patternPiece.ViewId = patternPiecePosition.ViewId;
                patternPiece.TranslateX = patternPiecePosition.OffsetX;
                patternPiece.TranslateY = patternPiecePosition.OffsetY;
                ProductionTask productionTask = productionTasks.Count(item => item.ViewId.Equals(patternPiecePosition.ViewId)) > 1 ?
                        productionTasks.FirstOrDefault(item => item.ViewId.Equals(patternPiecePosition.ViewId) && item.PatternPieceTitle.Equals(patternPiecePosition.PatternPieceTitle))
                     : productionTasks.FirstOrDefault(item => item.ViewId.Equals(patternPiecePosition.ViewId));
                if (productionTask is null)
                {
                    //TODO 排版缺少裁片信息
                    Console.WriteLine($"产品{uniqueBatchItem.DesignProductId}缺少裁片排版信息");
                }

                patternPiece.PatternPieceProduceLocalImgUrl = productionTask.PatternPieceProduceLocalImgUrl;
                patternPieceLayouts.Add(patternPiece);
            }
            catch (Exception ex)
            {
                // TODO 回滚任务? 抛错?
                Console.WriteLine("排版出错", ex);
            }
        }

        ProduceImgInfo produceImgInfo = new ProduceImgInfo();
        produceImgInfo.MachineConfig = machineConfig;
        produceImgInfo.RollOfFabric = rollOfFabric;
        produceImgInfo.PatternPieceLayoutList = patternPieceLayouts;
        produceImgInfo.Layout = ProduceLayout.MANUAL;
        produceImgInfo.SaveLocalInfo = saveLocalInfo;
        produceImgInfo.LayoutClothInfo = layoutClothInfo;
        produceImgInfo.QrCode = layout.QrCode;
        CreateProduceLayoutImg(produceImgInfo);
    }

    public static Image TransformWithAffine(
        Image sourceImage,
        double angleDegrees)
    {
        // 1. 将角度转换为弧度
        double angleRad = angleDegrees * Math.PI / 180.0;
        double cos = Math.Cos(angleRad);
        double sin = Math.Sin(angleRad);
        double scale = 1;
        // 2. 计算变换矩阵的 旋转 和 缩放 部分
        double a = cos * scale;
        double b = -sin * scale;
        double c = sin * scale;
        double d = cos * scale;
        var matrix = new double[] { a, b, c, d };

        // 5. 执行一次性的、高效的仿射变换
        return sourceImage.Affine(
            matrix,
            premultiplied: true // 使用预乘Alpha，效果更好
        );
    }

    /// <summary>
    /// 根据旋转轴心和原始偏移量算出旋转后的偏移量
    /// </summary>
    /// <param name="tileWidth">平铺图宽度</param>
    /// <param name="tileHeight">平铺图高度</param>
    /// <param name="tileOffsetX">平铺图X轴偏移量</param>
    /// <param name="tileOffsetY">平铺图Y轴偏移量</param>
    /// <param name="pivotX">旋转轴心点X轴坐标</param>
    /// <param name="pivotY">旋转轴心点Y轴坐标<</param>
    /// <param name="rotationDegrees">旋转角</param>
    /// <returns>新的NewX X轴偏移量 NewY Y轴偏移量</returns>
    public static (double X, double Y) CalcRotateOffsetByPivot(
        double tileWidth,
        double tileHeight,
        double tileOffsetX,
        double tileOffsetY,
        double pivotX,
        double pivotY,
        double rotationDegrees)
    {
        // 1. 将旋转角度从度转换为弧度
        double angleRad = rotationDegrees * Math.PI / 180.0;
        double cosAngle = Math.Cos(angleRad);
        double sinAngle = Math.Sin(angleRad);

        // 2. 定义平铺图的四个顶点坐标 (旋转前)
        var pivot = new Vector2((float)pivotX, (float)pivotY);
        var vertices = new List<Vector2>
        {
            new Vector2((float)tileOffsetX, (float)tileOffsetY),
            new Vector2((float)(tileOffsetX + tileWidth), (float)tileOffsetY),
            new Vector2((float)tileOffsetX, (float)(tileOffsetY + tileHeight)),
            new Vector2((float)(tileOffsetX + tileWidth), (float)(tileOffsetY + tileHeight))
        };

        // 3. 旋转每一个顶点
        var rotatedVertices = new List<Vector2>();
        foreach (var vertex in vertices)
        {
            // 将顶点坐标平移到以旋转轴心为原点的坐标系
            Vector2 translatedVertex = vertex - pivot;

            // 应用旋转矩阵
            float rotatedX = translatedVertex.X * (float)cosAngle - translatedVertex.Y * (float)sinAngle;
            float rotatedY = translatedVertex.X * (float)sinAngle + translatedVertex.Y * (float)cosAngle;

            // 将旋转后的坐标平移回原始坐标系
            Vector2 rotatedVertex = new Vector2(rotatedX, rotatedY) + pivot;
            rotatedVertices.Add(rotatedVertex);
        }

        // 4. 确定外接矩形的左上角 (即所有旋转后顶点的最小X和最小Y)
        double finalOffsetX = rotatedVertices.Min(v => v.X);
        double finalOffsetY = rotatedVertices.Min(v => v.Y);

        return (finalOffsetX, finalOffsetY);
    }

    private (Image tileBackgroundImg, double cellWidth, double cellHeight) CreateTiledBackgroundImage(
        Image sourceTile,
        PrintLayerInfo patternPrintLayerTask,
        ProductionTask patternPieceTask)
    {
        // 1. 确保源瓦片有Alpha通道和sRGB身份
        using Image sourceTileWithAlpha =
            (sourceTile.HasAlpha() ? sourceTile.Copy() : sourceTile.AddAlpha()).Colourspace(Enums.Interpretation.Srgb);


        // 2. 计算带间隙的“单元格”尺寸
        //int spacingX = ImageHelper.ConvertMmToPixels(patternPrintLayerTask.TileTool.TileSpacingXMm, patternPieceTask.TargetDpi);
        //int spacingY = ImageHelper.ConvertMmToPixels(patternPrintLayerTask.TileTool.TileSpacingYMm, patternPieceTask.TargetDpi);
        // 当前前端显示 "横向间距 100" 的含义是 图片的左右两侧各留出 100px的空白, 单位本该是毫米又变成px
        int spacingX = decimal.ToInt32(ImageHelper.ConvertMmToPixels(patternPrintLayerTask.TileTool.TileSpacingXMm,
            patternPieceTask.TargetDpi));
        int spacingY = decimal.ToInt32(ImageHelper.ConvertMmToPixels(patternPrintLayerTask.TileTool.TileSpacingYMm,
            patternPieceTask.TargetDpi));
        int cellWidth = sourceTileWithAlpha.Width + spacingX;
        int cellHeight = sourceTileWithAlpha.Height + spacingY;
        // 安全底图的尺寸
        (double backgroundWidth, double backgroundHeight) = ImageHelper.getTileSafeBackgroundSize(
            ImageHelper.ConvertMmToPixels(
                patternPieceTask.PatternPieceTargetWidthMm,
                patternPieceTask.TargetDpi),
            ImageHelper.ConvertMmToPixels(
                patternPieceTask.PatternPieceTargetHeightMm,
                patternPieceTask.TargetDpi),
            sourceTile.Width,
            sourceTile.Height);
        if (patternPrintLayerTask.TileTool.TileType.Equals(TileType.基础平铺))
        {
            // --- [核心的、全新的、正确的实现] ---
            // 3. 使用 .Gravity() 来创建带边距的单元格
            //    这个方法会创建一个更大的画布，并将源图像放置在指定位置
            // 不要用这个方法 会导致alpha通道丢失 srgb信息丢失变成multibands
            //using var cell = Image.Black(cellWidth, cellHeight, bands: 4).Insert(sourceTileWithAlpha, 0, 0);
            Image cell = sourceTileWithAlpha.Gravity(
                Enums.CompassDirection.NorthWest, // 将源图像放在新画布的左上角
                cellWidth,
                cellHeight
                // (可选) background: new double[] { R, G, B, A } // 可以指定背景色
            );
            // 5. 执行平铺操作
            using (cell)
            {
                // 计算平铺次数
                int across =
                    (int)Math.Ceiling(backgroundWidth / cellWidth);
                int down = (int)Math.Ceiling(
                    backgroundHeight / cellHeight);
                // Replicate 会正确地继承 cell 的 sRGB + Alpha 身份
                return (cell.Replicate(across, down), cellWidth, cellHeight);
            }
        }
        else if (patternPrintLayerTask.TileTool.TileType.Equals(TileType.镜像平铺))
        {
            // 计算平铺次数
            int across =
                (int)Math.Ceiling(backgroundWidth / (cellWidth * 2));
            int down = (int)Math.Ceiling(
                backgroundHeight / (cellHeight * 2));

            // 镜像平铺原理 : 先构造一个2*2 每个位置互相镜像的印花图区块 然后将这个块进行平铺 即可得到全幅镜像平铺
            using var baseCell = sourceTileWithAlpha.Gravity(
                Enums.CompassDirection.NorthWest, // 将源图像放在新画布的左上角
                cellWidth,
                cellHeight
                // (可选) background: new double[] { R, G, B, A } // 可以指定背景色
            );
            // 2. 创建水平翻转的单元
            using var positionRightCell = sourceTileWithAlpha.Flip(Enums.Direction.Horizontal);
            using Image positionRightCellWithSpacing = positionRightCell.Gravity(
                Enums.CompassDirection.NorthWest, // 将源图像放在新画布的左上角
                cellWidth,
                cellHeight
                // (可选) background: new double[] { R, G, B, A } // 可以指定背景色
            );
            // 3. 拼接成一个横向镜像的超级瓦片
            using var lineOneTile = baseCell.Join(positionRightCellWithSpacing, Enums.Direction.Horizontal);

            using var positionBottomCell = sourceTileWithAlpha.Flip(Enums.Direction.Vertical);
            using Image positionBottomCellWithSpacing = positionBottomCell.Gravity(
                Enums.CompassDirection.NorthWest, // 将源图像放在新画布的左上角
                cellWidth,
                cellHeight
                // (可选) background: new double[] { R, G, B, A } // 可以指定背景色
            );

            using var positionBottomRightCell = positionBottomCell.Flip(Enums.Direction.Horizontal);
            using Image positionBottomRightCellWithSpacing = positionBottomRightCell.Gravity(
                Enums.CompassDirection.NorthWest, // 将源图像放在新画布的左上角
                cellWidth,
                cellHeight
                // (可选) background: new double[] { R, G, B, A } // 可以指定背景色
            );
            using var lineTwoTile = baseCell.Join(positionRightCellWithSpacing, Enums.Direction.Horizontal);

            // 5. 拼接成 2x2 的四方连续单元
            using var quadTile = lineOneTile.Join(lineTwoTile, Enums.Direction.Vertical);
            // 6. 平铺这个 2x2 的单元
            return (quadTile.Replicate(across, down), cellWidth * 2, cellHeight * 2);
        }
        else if (patternPrintLayerTask.TileTool.TileType.Equals(TileType.横向错位平铺))
        {
            Image cell = sourceTileWithAlpha.Gravity(
                Enums.CompassDirection.NorthWest, // 将源图像放在新画布的左上角
                cellWidth,
                cellHeight
                // (可选) background: new double[] { R, G, B, A } // 可以指定背景色
            );
            // 5. 执行平铺操作
            using (cell)
            {
                // 计算平铺次数
                int across =
                    (int)Math.Ceiling(backgroundWidth / cellWidth);
                int down = (int)Math.Ceiling(
                    backgroundHeight / cellHeight);
                
                Image horizontalTileLineOne = cell.Replicate(across, 1);

                using var doubleCell = cell.Join(cell, Enums.Direction.Horizontal);
                // Crop 参数: left, top, width, height
                // 起始点 left = cell.Width / 2，就实现了向左偏移
                using var shiftedCell = doubleCell.Crop(cell.Width / 2, 0, cell.Width, cell.Height);

                // 使用这个错位的瓦片来创建第二行
                using var horizontalTileLineTwo = shiftedCell.Replicate(across, 1);

                // --- 步骤 3: 将正常行和错位行拼接成一个“双行模块” ---
                using var tileDoubleMod = horizontalTileLineOne.Join(horizontalTileLineTwo, Enums.Direction.Vertical);
                // --- 步骤 4: 平铺这个“双行模块” ---
                // 计算需要多少个“双行模块”才能覆盖整个高度
                // 因为 tileDoubleMod 的高度是 2 * cellHeight
                var modDown = (int)Math.Ceiling(down / 2.0);

                using var finalTiledImage = tileDoubleMod.Replicate(1, modDown);

                // --- 步骤 5: 裁剪到最终需要的精确尺寸 ---
                var finalImage = finalTiledImage.Crop(0, 0, (int)backgroundWidth, (int)backgroundHeight);
                // 返回最终图像和单元格尺寸
                finalImage.WriteToFile("zzzzzzbackground.png");
                return (finalImage, cellWidth, cellHeight);
            }
        }
        else if (patternPrintLayerTask.TileTool.TileType.Equals(TileType.纵向错位平铺))
        {
            Image cell = sourceTileWithAlpha.Gravity(
                Enums.CompassDirection.NorthWest, // 将源图像放在新画布的左上角
                cellWidth,
                cellHeight
                // (可选) background: new double[] { R, G, B, A } // 可以指定背景色
            );
            // 5. 执行平铺操作
            using (cell)
            {
                // 计算平铺次数
                int across =
                    (int)Math.Ceiling(backgroundWidth / cellWidth);
                int down = (int)Math.Ceiling(
                    backgroundHeight / cellHeight);
                
                Image verticalTileLineOne = cell.Replicate(1, down);

                using var doubleCell = cell.Join(cell, Enums.Direction.Vertical);
                // Crop 参数: left, top, width, height
                // 起始点 left = cell.Height / 2，就实现了向上偏移
                using var shiftedCell = doubleCell.Crop(0, cell.Height / 2, cell.Width, cell.Height);

                // 使用这个错位的瓦片来创建第二行
                using var verticalTileLineTwo = shiftedCell.Replicate(1, down);

                // --- 步骤 3: 将正常行和错位行拼接成一个“双行模块” ---
                using var tileDoubleMod = verticalTileLineOne.Join(verticalTileLineTwo, Enums.Direction.Horizontal);
                // --- 步骤 4: 平铺这个“双行模块” ---
                // 计算需要多少个“双行模块”才能覆盖整个高度
                // 因为 tileDoubleMod 的宽度是 2 * cellWidth
                var modDown = (int)Math.Ceiling(across / 2.0);

                using var finalTiledImage = tileDoubleMod.Replicate(modDown,1);

                // --- 步骤 5: 裁剪到最终需要的精确尺寸 ---
                var finalImage = finalTiledImage.Crop(0, 0, (int)backgroundWidth, (int)backgroundHeight);
                
                
                // 返回最终图像和单元格尺寸
                return (finalImage, cellWidth, cellHeight);
            }
        }
        else
        {
            throw new Exception($"暂不支持的平铺方式[{patternPrintLayerTask.TileTool.TileType}]");
        }
    }

    // 旋转
    private void rotate(LocalImgInfo localImg, string localOutputPath, decimal rotation)
    {
        using (var image = Image.NewFromFile(localImg.LocalUrl))
        {
            // Rotate 方法接收一个角度值
            using (var rotatedImage = image.Rotate(decimal.ToDouble(rotation)))
            {
                string localOutputUrl = localOutputPath + localImg.FileName + '.' + localImg.Extenion;
                Directory.CreateDirectory(localOutputPath);
                rotatedImage.WriteToFile(localOutputUrl);
                Console.WriteLine($"图片已旋转 {rotation} 度，并保存至: {localOutputUrl}");
                Console.WriteLine($"旋转后画布尺寸: {rotatedImage.Width}x{rotatedImage.Height}");
            }
        }
    }

    // 位移
    private void move()
    {
    }

    // 缩放
    private void resize()
    {
    }

    // 叠加合成
    private void composite()
    {
    }

    // 创建生产排版图 
    public void CreateProduceLayoutImg(ProduceImgInfo produceImgInfo)
    {
        using (var canvas = _imageCreator.CreateImageFromPhysicalSize(
                   decimal.ToDouble(produceImgInfo.LayoutClothInfo.WidthMm),
                   decimal.ToDouble(produceImgInfo.LayoutClothInfo.HeightMm),
                   produceImgInfo.MachineConfig.Dpi,
                   ImgSupportFormat.Png,
                   backgroundColor: new double[] { 255, 255, 255, 0 })) // 透明 RGBA
        {
            Image currentResult = canvas; // 使用一个变量来持有流水线的当前结果
            try
            {
                // --- 步骤 2: 按顺序处理和叠加每个裁片 ---
                foreach (PatternPieceLayout layout in produceImgInfo.PatternPieceLayoutList)
                {
                    try
                    {
                        // 将每个裁片的加载和变换都包裹在 using 块中
                        using (Image pieceRaw = Image.NewFromFile(layout.PatternPieceProduceLocalImgUrl,
                                   access: Enums.Access.Random))
                            //裁片的色彩信息可能丢失 手动指定色彩空间为SRGB    
                            //using (Image piece = pieceRaw.Colourspace(Enums.Interpretation.Scrgb))

                        using (Image rotatedPiece = TransformWithAffine(pieceRaw, decimal.ToDouble(layout.Rotation)))
                        {
                            // a. Composite 创建一个全新的 Image 结果
                            Image newResult = currentResult.Composite(
                                rotatedPiece,
                                Enums.BlendMode.Over, // Over 是标准的Alpha叠加，Atop可能不是您想要的
                                x: ImageHelper.ConvertMmToPixels(layout.TranslateX, produceImgInfo.MachineConfig.Dpi) -
                                   decimal.ToInt32((new decimal(rotatedPiece.Width) - new decimal(pieceRaw.Width)) / 2),
                                y: ImageHelper.ConvertMmToPixels(layout.TranslateY, produceImgInfo.MachineConfig.Dpi) -
                                   decimal.ToInt32(
                                       (new decimal(rotatedPiece.Height) - new decimal(pieceRaw.Height)) / 2)
                            );

                            // b. 释放上一个中间结果 (如果它不是最初的画布)
                            if (currentResult != canvas)
                            {
                                currentResult.Dispose();
                            }

                            // c. 将引用指向新结果
                            currentResult = newResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"创建排版出错: {ex}");
                    }
                }

                // 叠加二维码

                Image qrCode = GenerateQrCode2Image(produceImgInfo.QrCode.Content,
                    ImageHelper.ConvertMmToPixels(produceImgInfo.QrCode.Width, produceImgInfo.MachineConfig.Dpi),
                    ImageHelper.ConvertMmToPixels(produceImgInfo.QrCode.Height, produceImgInfo.MachineConfig.Dpi));
                Image whiteQrBackground = _imageCreator.CreateImageFromPhysicalSize(
                    decimal.ToDouble(produceImgInfo.QrCode.Width),
                    decimal.ToDouble(produceImgInfo.QrCode.Height),
                    produceImgInfo.MachineConfig.Dpi,
                    ImgSupportFormat.Png,
                    backgroundColor: new double[] { 255, 255, 255, 255 });
                qrCode = whiteQrBackground.Composite(qrCode, Enums.BlendMode.Over);
                currentResult = currentResult.Composite(
                    qrCode,
                    Enums.BlendMode.Over, // Over 是标准的Alpha叠加，Atop可能不是您想要的
                    x: ImageHelper.ConvertMmToPixels(produceImgInfo.QrCode.OffsetX, produceImgInfo.MachineConfig.Dpi),
                    y: ImageHelper.ConvertMmToPixels(produceImgInfo.QrCode.OffsetY, produceImgInfo.MachineConfig.Dpi)
                );

                // --- 步骤 3: 保存最终结果 ---
                // 此时，currentResult 就是包含了所有叠加裁片的最终图像
                _imageCreator.SaveImageForProduction(
                    currentResult,
                    produceImgInfo.SaveLocalInfo.LocalPath + produceImgInfo.SaveLocalInfo.Name, // 您的完整路径
                    produceImgInfo.SaveLocalInfo.ImgFormat // 保存设定的图片格式
                );
            }
            finally
            {
                // --- 步骤 4: 确保最后一个中间结果被释放 ---
                // 如果至少进行了一次叠加，currentResult将不再是canvas
                if (currentResult != null && currentResult != canvas)
                {
                    currentResult.Dispose();
                }
            }
        }
    }

    public static Image? GenerateQrCode2Image(string content, int width, int height, int margin = 1)
    {
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        try
        {
            // 1. 配置二维码生成器
            var qrCodeWriter = new BarcodeWriter<SvgRenderer.SvgImage>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = margin,
                    ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.M, // 中等容错
                },
                Renderer = new SvgRenderer()
            };
            var svgImage = qrCodeWriter.Write(content);
            string svgContent = svgImage.Content;

            // --- [核心修复：从字符串到内存缓冲区的转换] ---

            // 2. 将SVG字符串，使用UTF-8编码，转换为字节数组 (byte[])
            byte[] svgBytes = Encoding.UTF8.GetBytes(svgContent);

            // --- 3. [NetVips] 直接从内存缓冲区加载SVG ---
            //    使用 Image.NewFromBuffer()
            using (Image initialSvgLoad = Image.NewFromBuffer(svgBytes))
            {
                // 4. 使用 .ThumbnailImage() 精确地缩放到目标尺寸 (不变)
                return initialSvgLoad.ThumbnailImage(width: width, height: height);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成二维码失败: {ex.Message}");
            return null;
        }
    }
}