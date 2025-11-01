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
using DataJuggler.RealESRGAN;
using DataJuggler.RealESRGAN.Enumerations;
using ImageMagick;
using ImageMagick.Formats;
using Microsoft.Win32;
using NetVips;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Component;
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
    
    private readonly PhotoshopService _photoshopService;

    // 构造函数：声明自己需要一个 IImageProcessor
    public ProduceImageProcessor(
        IImageCreator imageCreator,
        ILayoutApi layoutApi,
        LoginInfoService loginInfoService,
        PhotoshopService photoshopService
    )
    {
        _imageCreator = imageCreator;
        _layoutApi = layoutApi; // DI容器会自动提供实例
        _loginInfoService = loginInfoService;
        _photoshopService = photoshopService;
    }

    // TODO 现在同产品多印花面 会打印N次 导致磁盘多写入N次 但生产图会覆盖 所以结果没问题 因为获取一个生产项信息的数据包含多个面信息 直接一次生成了全部生产图
    // TODO 使用Affine 一次性完整矩阵变换(缩放/旋转/位移) 似乎性能更高?
    public ProduceBatchTaskResult ProcessProductionTask(UniqueBatchItem uniqueBatchItem)
    {
        if (uniqueBatchItem.ProductionTasks.All(item => item.PrintLayers.Count == 0))
        {
            if (!uniqueBatchItem.IsMultiPiece)
            {
                Console.WriteLine(
                    $"批次号[{uniqueBatchItem.ProduceBatchNum}]项批号[{uniqueBatchItem.BatchNum}]没有任何印花图层 已跳过生产");
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
                                        (Math.Abs(tileBackgroundImgTranslateX) + Math.Abs(translateXPixel)) /
                                        cellWidth) % 2 == 0
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
                                    (double NewX, double NewY) = CalcRotateOffsetByPivot(tileImage.Width,
                                        tileImage.Height,
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
                        throw new Exception($"印花裁片合成出错 {ex}");
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
                ProductionTask productionTask =
                    productionTasks.Count(item => item.ViewId.Equals(patternPiecePosition.ViewId)) > 1
                        ? productionTasks.FirstOrDefault(item =>
                            item.ViewId.Equals(patternPiecePosition.ViewId) &&
                            item.PatternPieceTitle.Equals(patternPiecePosition.PatternPieceTitle))
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

                using var finalTiledImage = tileDoubleMod.Replicate(modDown, 1);

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

    /** 使用 NetVips 将常见图像（如 PNG）转换为带有白色专色通道的 CMYK TIFF 文件。
    * 此方法性能高，内存占用低。
    * </summary>
    * <param name="inputImagePath">输入图像文件（例如 a.png）的完整路径。</param>
    * <param name="outputTiffPath">输出 TIFF 文件（例如 a.tif）的保存路径。</param>
    * <param name="cmykProfilePath">（强烈推荐）用于精确颜色转换的 CMYK ICC 颜色配置文件路径。</param> **/
    public static void ConvertToCmykWithSpotColor(
        string inputImagePath,
        string outputTiffPath,
        string? cmykProfilePath = null, // 参数仍然是可选的
        double defaultDpi = 300.0)
    {
        // AddSpotChannel(inputImagePath,outputTiffPath,"W1");
        // 测试放放大四倍正常 放大两倍出错 AI图片放大
        // RealESRGANHelper.UpscaleImage(inputImagePath, outputTiffPath,UpscaleModelEnum.UltraSharp,ScaleEnum.Four_X);

        if (!File.Exists(inputImagePath))
        {
            throw new FileNotFoundException("输入图像文件未找到。", inputImagePath);
        }

        var iccProfileToUse = cmykProfilePath;
        if (string.IsNullOrEmpty(iccProfileToUse) || !File.Exists(iccProfileToUse))
        {
            iccProfileToUse = FindDefaultCmykProfile();
        }

        string finalCorrectPath = Path.ChangeExtension(outputTiffPath, ".tif");

        using var image = Image.NewFromFile(inputImagePath);

        // --- DPI 准备 ---
        double dpiX, dpiY;
        var xresInPpm = image.Xres;
        var yresInPpm = image.Yres;
        if (xresInPpm <= 0 || yresInPpm <= 0)
        {
            dpiX = defaultDpi;
            dpiY = defaultDpi;
        }
        else
        {
            dpiX = xresInPpm * ImageHelper.MillimetersPerInch;
            dpiY = yresInPpm * ImageHelper.MillimetersPerInch;
        }

        // --- [最终核心修复：重构整个图像处理流程] ---

        // 步骤 1: 确保我们有一个带 Alpha 通道的源图像。
        // Alpha 通道将作为我们的专色通道。
        using var imageWithAlpha = image.HasAlpha()
            ? image.Copy() // 如果已经有 alpha，直接使用
            : image.Bandjoin(255); // 如果没有，添加一个全白（不透明）的 alpha 通道

        // 步骤 2: 将这个 4 通道的 RGBA 图像转换到目标 CMYK 色彩空间。
        // libvips 会智能地将 RGB -> CMYK (4 个通道)，并将原始的 Alpha 通道作为第 5 个通道附加。
        // 这一步的结果是一个 5 通道的图像，其 interpretation 为 Multiband。
        //using var cmykWithSpot = imageWithAlpha.Colourspace(Enums.Interpretation.Cmyk, sourceSpace: Enums.Interpretation.Srgb);
        
        // --- 专色层和 CMYK 图像准备 ---
        using Image spotPlate = image.HasAlpha()
            ? image.ExtractBand(image.Bands - 1).Invert()
            : Image.Black(image.Width, image.Height).Invert();
        // 创建一个 5x5 的方形结构元素 (核)，用于一次性腐蚀2个像素。 (n-1)/2  n为矩阵长度
        // 在 NetVips 中，结构元素本身就是一个 Image 对象。
        using var mask = Image.NewFromArray(new byte[,]
        {
            { 255, 255, 255, 255, 255},
            { 255, 255, 255, 255, 255},
            { 255, 255, 255, 255, 255},
            { 255, 255, 255, 255, 255},
            { 255, 255, 255, 255, 255}
        });
        // 使用创建的 5x5 核，对专色蒙版执行一次腐蚀操作。
        //using var spotPlateShrunk = spotPlate.Erode(mask);
        // 先对透明通道取反->外扩 = 非透明区域内缩
        using var spotPlateShrunk = spotPlate.Dilate(mask);
        
        using var imageWithoutAlpha = image.HasAlpha() ? image.Copy() : image;

        using Image cmykImage = imageWithoutAlpha.IccTransform(iccProfileToUse, inputProfile: "srgb");

        using var cmykWithSpot = cmykImage.Bandjoin(spotPlateShrunk);
        // --- 通道合并 ---
        /*
        
        
        byte[] cmykBytes = cmykWithSpot.TiffsaveBuffer(compression: Enums.ForeignTiffCompression.Lzw, profile: iccProfileToUse);

        using (var imageToProcess = new MagickImage(cmykBytes))
        {
            var cmykChannels = imageToProcess.Separate(Channels.CMYK);
            var alphaChannel = imageToProcess.Separate(Channels.Alpha);

            // Create a collection of channels to combine
            using (var channels = new MagickImageCollection())
            {
                channels.AddRange(cmykChannels);
                channels.AddRange(alphaChannel);

                // Combine the channels
                using (var combinedImage = channels.Combine(ColorSpace.CMYK))
                {
                    // Set the photometric interpretation to "Separated". This is crucial for spot colors.
                    combinedImage.Settings.SetDefine(MagickFormat.Tiff, "photometric", "separated");

                    // result.Settings.SetDefine(MagickFormat.Tiff, "ink-names", "Cyan,Magenta,Yellow,Black,W1");
                    // Tag 333: Provide the names for all inks. The number of names must match the number of channels.
                    combinedImage.Settings.SetDefine(MagickFormat.Tiff, "ink-names", "Cyan\0Magenta\0Yellow\0Black\0W1");

                    // Tag 338: Describe the extra sample. '2' stands for 'Unspecified data',
                    // which is typically used for spot colors.
                    combinedImage.Settings.SetDefine(MagickFormat.Tiff, "extra-samples", "2");
                    // Set the colorspace to CMYK explicitly.
                    combinedImage.ColorSpace = ColorSpace.CMYK;

                    // Write the final TIFF image
                    combinedImage.Write(finalCorrectPath);
                }
            }
        }*/
        /*byte[] spotChannelBytes = spotPlateShrunk.TiffsaveBuffer(compression: Enums.ForeignTiffCompression.Lzw);
        byte[] cmykBytes = cmykImage.TiffsaveBuffer(compression: Enums.ForeignTiffCompression.Lzw, profile: iccProfileToUse);

        using (var imageToProcess = new MagickImage(cmykBytes))
        {
            var cmykChannels = imageToProcess.Separate(Channels.CMYK);
            var alphaChannel = imageToProcess.Separate(Channels.Alpha);

            // Create a collection of channels to combine
            using (var channels = new MagickImageCollection())
            {
                channels.AddRange(cmykChannels);
                channels.AddRange(alphaChannel);

                // Combine the channels
                using (var result = channels.Combine(ColorSpace.CMYK))
                {
                    // Set TIFF defines for spot color
                    result.Settings.SetDefine(MagickFormat.Tiff, "photometric", "separated");
                    result.Settings.SetDefine(MagickFormat.Tiff, "ink-names", "Cyan,Magenta,Yellow,Black,W1");

                    // Write the output file
                    result.Write(finalCorrectPath);
                }
            }


            // 1. 检查文件是否确实有Alpha通道
            if (!imageToProcess.HasAlpha)
            {
                throw new InvalidOperationException("临时文件未能正确生成Alpha通道。");
            }

            // 2. 将Alpha通道分离出来，成为一个独立的灰度图像
            // DeactivateAlpha() 会移除Alpha通道，并将其作为单独的图像返回
            using (IMagickImage spotChannel = imageToProcess.Separate(Channels.Alpha)[0])
            {
                // 此时, imageToProcess 变回了4通道的CMYK图像
                // spotChannel 是一个只包含原Alpha数据的灰度图

                // 3. 为这个分离出的通道附加专色元数据
                spotChannel.SetArtifact("channel:type", "spot");
                spotChannel.SetArtifact("channel:alias", "W1");

                imageToProcess.SetWriteMask(spotChannel);
            }

            imageToProcess.Settings.SetDefine(MagickFormat.Tiff, "write-spot-channels", "true");

            // 6. 保存最终文件
            imageToProcess.Write(finalCorrectPath);
            Console.WriteLine($"转换成功！最终文件已保存到: {finalCorrectPath}");
        }*/
        
        /*using (var cmykMagickImage = new MagickImage(cmykBytes)) // 直接从内存TIFF读取
        using (var spotMagickImage = new MagickImage(spotChannelBytes)) // 直接从内存TIFF读取
        {
            // ... (所有 SetArtifact, SetChannel, Write 的逻辑与方案一完全相同) ...
            spotMagickImage.SetArtifact("channel:type", "spot");
            spotMagickImage.SetArtifact("channel:alias", "W1");
            // cmykMagickImage.SetChannel(PixelChannel.Meta0, spotMagickImage);
            cmykMagickImage.SetWriteMask(spotMagickImage);
            //var tiffDefines = new TiffWriteDefines { WriteSpotChannels = true };
            cmykMagickImage.Settings.SetDefine(MagickFormat.Tiff, "write-spot-channels", "true");
            cmykMagickImage.Write(finalCorrectPath);

            // 保存带有专色通道的新TIFF文件
            // 注意：这里不再需要 TiffWriteDefines 对象
            //image.Write(outputTiffPath+".tif");
        }*/
        
        
        // 人工检查专色通道是否正藏
        // cmykWithSpot.ExtractBand(4).Pngsave("spotcolorcheck.png");
        
        // 步骤 3: 保存这个 5 通道的图像。
        // 在保存时，我们通过 `profile` 参数提供 CMYK ICC 配置文件。
        // 这个组合会让 Tiffsave 正确地写入所有 5 个通道，并将第 5 个标记为 Extra Sample。
        cmykWithSpot.Tiffsave(finalCorrectPath,
            compression: Enums.ForeignTiffCompression.Lzw,
            profile: iccProfileToUse, // 在这里提供配置文件是成功的关键
            tile: true,
            pyramid: false,
            resunit: Enums.ForeignTiffResunit.Inch,
            xres: xresInPpm,
            yres: yresInPpm
        );
        ExecutePhotoshopJsxAnyChannel2SpotColor(new List<string>(){finalCorrectPath});
        Console.WriteLine($"转换成功！文件已保存到: {finalCorrectPath}");
    }

    // 运行ps脚本转换任意通道成专色通道
    private static async void ExecutePhotoshopJsxAnyChannel2SpotColor(List<string> inputImagePathList)
    {
        /*OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.tif;*.tiff;*.psd",
            Title = "请选择一张需要处理的图片"
        };

        if (openFileDialog.ShowDialog() != true) return;

        string selectedImagePath = openFileDialog.FileName;*/


        try
        {
            // 异步调用处理方法，并解构返回的元组
            var (isSuccess, message) = await PhotoshopService.ProcessImageAsync(inputImagePathList);

            if (isSuccess)
            {
                //StatusTextBlock.Text = $"处理成功！\n文件已保存到: {_outputFolderPath}";
            }
            else
            {
                // 显示来自服务层的详细错误信息
                //StatusTextBlock.Text = $"处理失败！\n详细信息: {message}";
            }
        }
        catch (Exception ex)
        {
            // 捕获异步任务中未处理的异常
            //StatusTextBlock.Text = $"程序发生意外错误: {ex.Message}";
        }
        finally
        {
            //ProcessButton.IsEnabled = true;
        }
    }

    private static string FindDefaultCmykProfile()
    {
        // 定义常见的 ICC 配置文件存放目录
        var profilePaths = new[]
        {
            // Windows
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "spool", "drivers", "color"),
            AppContext.BaseDirectory + "Assets" + Path.DirectorySeparatorChar + "Icc" +
            Path.DirectorySeparatorChar + "CMYK"
        };

        // 定义我们优先想要的通用配置文件
        var preferredProfiles = new[] { "USWebCoatedSWOPv2.icc", "USWebCoatedSWOP.icc", "CoatedFOGRA39.icc" };

        foreach (var path in profilePaths)
        {
            if (Directory.Exists(path))
            {
                // 优先查找我们想要的配置文件
                foreach (var preferred in preferredProfiles)
                {
                    var fullPath = Path.Combine(path, preferred);
                    if (File.Exists(fullPath))
                    {
                        Console.WriteLine($"找到首选的 CMYK 配置文件: {fullPath}");
                        return fullPath;
                    }
                }

                // 如果没找到首选的，就找目录下任何一个 .icc 文件作为备用
                try
                {
                    var fallbackProfile = Directory.GetFiles(path, "*.icc").FirstOrDefault();
                    if (fallbackProfile != null)
                    {
                        Console.WriteLine($"警告: 未找到首选的 CMYK 配置文件，使用备用文件: {fallbackProfile}");
                        return fallbackProfile;
                    }
                }
                catch (IOException)
                {
                    /* 忽略权限等问题 */
                }
            }
        }

        throw new FileNotFoundException(
            "在系统中找不到默认的 CMYK ICC 配置文件。请在调用方法时手动提供一个有效的 cmykProfilePath。");
    }
    
    public static void AddSpotChannel(string inputTiffPath, string outputTiffPath, string spotColorName)
    {
        // 确保输出目录存在
        string directoryName = Path.GetDirectoryName(outputTiffPath);
        if (!string.IsNullOrEmpty(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        try
        {
            // 使用using语句确保资源被正确释放
            using (MagickImage image = new MagickImage(inputTiffPath))
            {
                // 1. 确保图像是CMYK模式，这是添加专色的常见基础
                if (image.ColorSpace != ColorSpace.CMYK)
                {
                    Console.WriteLine("警告: 图像不是CMYK格式，正在尝试转换为CMYK。");
                    // 实际应用中，你可能需要根据具体情况加载合适的色彩配置文件(ICC profile)
                    image.ColorSpace = ColorSpace.CMYK;
                }

                // 2. 创建一个新的图层作为专色通道
                // 尺寸与原图相同，背景为白色。
                // 在专色通道中，白色通常代表100%的油墨浓度，黑色代表0%。
                using (MagickImage spotChannel = new MagickImage(MagickColors.White, image.Width, image.Height))
                {
                    // 3. 将这个新图层设置为灰度图，这是作为通道的必要步骤
                    spotChannel.Grayscale(PixelIntensityMethod.Average);

                    // 4. 将此通道设置为非主要通道（读/写通道），并指定为专色
                    spotChannel.SetArtifact("channel:type", "spot");
                    spotChannel.SetArtifact("channel:alias", spotColorName); // 设置专色名称
                    // Magick.NET 7 (旧版) 的写法
                    image.SetWriteMask(spotChannel); 
                    // image.Settings.SetDefine(MagickFormat.Tiff, "write-spot-channels", "true");

                    // Magick.NET 8+ (推荐) 的写法
                    // 将专色通道合并到原图像中
                    // image.SetChannel(PixelChannel.Meta0, spotChannel);
                }

                // TiffWriteDefines defines = new TiffWriteDefines { WriteSpotChannels = true };
                image.Settings.SetDefine(MagickFormat.Tiff, "write-spot-channels", "true");

                // 保存带有专色通道的新TIFF文件
                // 注意：这里不再需要 TiffWriteDefines 对象
                image.Write(outputTiffPath+".tif");

                Console.WriteLine($"成功添加专色通道 '{spotColorName}' 并保存至: {outputTiffPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理TIFF文件时发生错误: {ex.Message}");
        }
    }
    
    public static Dictionary<string, Image> ExtractSpotChannels(string inputTiffPath)
    {
        var extractedChannels = new Dictionary<string, Image>();

        if (!File.Exists(inputTiffPath))
        {
            Console.WriteLine($"错误: 文件未找到 '{inputTiffPath}'");
            return extractedChannels;
        }

        try
        {
            using (Image image = Image.NewFromFile(inputTiffPath))
            {
                Console.WriteLine($"已加载图像: {inputTiffPath}");
                Console.WriteLine($" - 尺寸: {image.Width}x{image.Height}");
                Console.WriteLine($" - 通道数: {image.Bands}");
                Console.WriteLine($" - 色彩空间: {image.Interpretation}");

                // 1. 检查是否有额外通道
                // 假设基础是CMYK，有4个通道
                if (image.Interpretation != Enums.Interpretation.Cmyk || image.Bands <= 4)
                {
                    Console.WriteLine("该图像不是带有额外通道的CMYK图像。");
                    return extractedChannels;
                }

                int numSpotChannels = image.Bands - 4;
                Console.WriteLine($"发现 {numSpotChannels} 个额外的通道。");

                // 2. 尝试读取专色通道名称 (InkNames Tag)
                List<string> spotChannelNames = new List<string>();
                try
                {
                    // InkNames TIFF tag is 333. libvips exposes it this way.
                    string inkNamesRaw = image.Get("tiff-tag-333-ink-names") as string;
                    if (!string.IsNullOrEmpty(inkNamesRaw))
                    {
                        // The InkNames tag is a list of names, each separated by a null character.
                        spotChannelNames.AddRange(inkNamesRaw.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries));
                        Console.WriteLine("成功读取到以下专色名称: " + string.Join(", ", spotChannelNames));
                    }
                }
                catch (VipsException)
                {
                    Console.WriteLine("未找到 'InkNames' (TIFF Tag 333) 元数据。将使用默认名称。");
                }

                // 3. 提取每个专色通道的数据
                for (int i = 0; i < numSpotChannels; i++)
                {
                    // 通道索引从0开始，CMYK是0,1,2,3。第一个专色通道是索引4。
                    int channelIndex = 4 + i;

                    // 确定通道名称
                    string channelName;
                    if (i < spotChannelNames.Count)
                    {
                        channelName = spotChannelNames[i];
                    }
                    else
                    {
                        // 如果元数据中的名称不够，则提供一个备用名称
                        channelName = $"Unknown Name!";
                    }

                    Console.WriteLine($"正在提取通道索引 {channelIndex}，命名为 '{channelName}'...");

                    // 提取通道数据
                    // 注意：返回的 Image 对象需要由调用者管理其生命周期 (dispose)
                    Image spotChannelImage = image.ExtractBand(channelIndex);
                    // 由于ps无法预览 输入图片 人工检查专色通道是否正确
                    // spotChannelImage.Pngsave("xxx.png");
                    extractedChannels.Add(channelName, spotChannelImage);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"使用 NetVips 提取专色通道时发生错误: {ex.Message}");
        }

        return extractedChannels;
    }
    
    public static void ReadSpotChannelNamesWithMagick(string inputTiffPath)
    {
        Console.WriteLine($"--- 使用 Magick.NET 读取文件: {inputTiffPath} ---");
        if (!File.Exists(inputTiffPath))
        {
            Console.WriteLine("错误: 文件未找到！");
            return;
        }

        try
        {
            using (var image = new MagickImage(inputTiffPath))
            {
                // 1. 检查是否有额外的通道
                // Magick.NET 会自动将专色通道识别为额外的通道
                // image.ChannelCount 会包含所有通道 (CMYK + Spots)
                if (image.ColorSpace != ColorSpace.CMYK || image.ChannelCount <= 4)
                {
                    Console.WriteLine("图像不是带有额外通道的CMYK文件。");
                    return;
                }

                Console.WriteLine($"文件总通道数: {image.ChannelCount}");

                // 2. (关键) 使用 GetAttribute 来获取专色名称
                // 这个方法会智能地查找 Tag 333 和 Adobe 私有数据块
                string inkNamesAttribute = image.GetAttribute("tiff:ink-names");

                if (!string.IsNullOrEmpty(inkNamesAttribute))
                {
                    // Photoshop 返回的 ink-names 属性通常是用换行符 \n 分隔的
                    string[] names = inkNamesAttribute.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    Console.WriteLine($"成功! 读取到以下专色通道名称: '{string.Join(", ", names)}'");

                    // 3. (可选但推荐) 提取每个专色通道的图像数据
                    // 你可以通过通道索引来分离它们
                    uint spotChannelCount = image.ChannelCount - 4;
                    for (int i = 0; i < spotChannelCount; i++)
                    {
                        // CMYK = 0,1,2,3。第一个专色是通道 4
                        int channelIndex = 4 + i;
                        Console.WriteLine($"正在分离通道 {channelIndex} ({names[i]}) ...");

                        using (IMagickImage spotChannelImage = image.Separate(GetChannelByIndex(channelIndex))[0])
                        {
                            // 现在 spotChannelImage 就是一个只包含该专色数据的灰度图
                            // 你可以保存它或进行进一步处理
                             spotChannelImage.Write($@"C:\temp\Extracted_{names[i]}.tif");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("失败! 未能从文件中读取到 'tiff:ink-names' 属性。");
                    Console.WriteLine("这可能意味着文件没有专色通道，或者元数据格式非常特殊。");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取文件时发生错误: {ex.Message}");
        }
    }
    
    // 辅助函数，根据索引获取 PixelChannel 枚举
    private static Channels GetChannelByIndex(int index)
    {
        // ImageMagick 7+ 使用 Channels 枚举
        switch (index)
        {
            case 0: return Channels.Cyan;
            case 1: return Channels.Magenta;
            case 2: return Channels.Yellow;
            case 3: return Channels.Black;
            case 4: return Channels.Gray; // 第一个额外通道通常映射到Alpha/Gray
            // Magick.NET 可能会将后续通道视为 Meta 通道
            default:
                 // 对于第五个以上通道，需要更复杂的处理
                 // 但对于提取第一个专色，Channels.Gray 或 Channels.Alpha 通常有效
                 return Channels.Gray;
        }
    }
}