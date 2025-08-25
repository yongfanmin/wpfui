// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using NetVips;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Utils;

namespace Wpf.Ui.Gallery.ImageProcessor;

// 生产图处理器
// 传入 裁片信息 印花图信息 打印信息(旋转 位移 缩放 ..)
// 对印花图执行打印信息操作(旋转 位移 缩放 ..)
// 按照生产尺寸对 裁片与印花图 进行等比放大
// 位移信息可能需要按照放大信息进行等比放大处理
// 放大后的 裁片与印花图 进行叠加合成
public class ProduceImageProcessor : IProduceImageProcessor
{
    public async Task<List<ProductionTask>> processProductionTask(List<ProductionTask> productionTasks)
    {
        foreach (ProductionTask patternPieceTask in productionTasks)
        {
            // 公版裁片为基底作业流水线
            // Enums.Access.Sequential 顺序读取不能用 因为要保存缩略图 如果用了顺序读取 保存完大图 指针会在图片末尾, 无法从头读取像素去制造缩略图
            using Image patternPieceImg = Image.NewFromFile(patternPieceTask.PatternPieceImageLocalImg.LocalUrl,
                access: Enums.Access.Sequential);
            Image tempCanvas = patternPieceImg.Resize(ImageHelper.pixelSizeToPhysicalSizeNeedScale(patternPieceImg.Width,
                patternPieceTask.PatternPieceTargetWidthMm, patternPieceTask.TargetDpi));
            foreach (PrintLayerInfo patternPrintLayerTask in patternPieceTask.PrintLayers)
            {
                using Image patternPrintImg = Image.NewFromFile(patternPrintLayerTask.DesignImageLocalImg.LocalUrl,
                    access: Enums.Access.Sequential);
                // 横向缩放 水平倾斜 垂直缩放 垂直倾斜
                // var transformMatrix = new double[] { decimal.ToDouble(patternPrintLayerTask.ScaleX), 0, 0, decimal.ToDouble(patternPrintLayerTask.ScaleY) };
                using Image scalePatternPrintImg = patternPrintImg.Resize(ImageHelper.pixelSizeToPhysicalSizeNeedScale(
                    patternPrintImg.Width,
                    patternPrintLayerTask.DesignImageSizeMm.Width, patternPieceTask.TargetDpi));
                using Image rotatePatternPrintImg =
                    scalePatternPrintImg.Rotate(decimal.ToDouble(patternPrintLayerTask.Rotation));
                // Atop模式: 叠加裁片图和印花图
                Image newCanvas = tempCanvas.Composite(rotatePatternPrintImg, Enums.BlendMode.Atop,
                    ImageHelper.ConvertMmToPixels(patternPrintLayerTask.TranslateX, patternPieceTask.TargetDpi),
                    ImageHelper.ConvertMmToPixels(patternPrintLayerTask.TranslateY, patternPieceTask.TargetDpi));
                tempCanvas.Dispose();
                tempCanvas = newCanvas;
            }

            string localOutputPath = FileName.getOrderPatternPrintImgPath(patternPieceTask.OrderNo,
                patternPieceTask.FactoryId,
                0);
            
            Directory.CreateDirectory(localOutputPath);
            
            string localOutputThumbPath = FileName.getOrderPatternPrintImgThumbPath(patternPieceTask.OrderNo,
                patternPieceTask.FactoryId,
                0);
            
            Directory.CreateDirectory(localOutputThumbPath);
            //测试写死
            //patternPieceImg.WriteToFile(localOutputPath + patternPieceTask.PatternPieceTitle+".png");
            
            // 使用通用的 WriteToFile，它会根据后缀名自动选择 png 保存器
            //imageToSave.Tiffsave(localOutputPath, xres: pixelsPerMm, yres: pixelsPerMm);
            
            // --- 这是设置DPI并保存的最终、正确的方法 ---
            // 1. 将DPI转换为libvips需要的单位: 像素/毫米
            /*double xres = patternPieceTask.TargetDpi/ImageHelper.MillimetersPerInch;
            double yres = patternPieceTask.TargetDpi/ImageHelper.MillimetersPerInch;
            VOption voption = new VOption();
            voption.AddIfPresent<double>(nameof(xres), xres);
            voption.AddIfPresent<double>(nameof(yres), yres);
            var saveOptions = new VOption
            {
                { "xres", xres },
                { "yres", yres },
                { "resolution-unit", "in" }
            };*/
            double pixelsPerMm = patternPieceTask.TargetDpi/ImageHelper.MillimetersPerInch;
            // 此写法无效 无法改变dpi
            /*patternPieceImg = patternPieceImg.Mutate(mutableImage =>
            {
                mutableImage.Set(GValue.GDoubleType, "xres", pixelsPerMm);
                mutableImage.Set(GValue.GDoubleType, "yres", pixelsPerMm);
            });*/
            string PatternPieceProduceImg = localOutputPath + patternPieceTask.PatternPieceTitle + ".png";
            using (var imageToSave = tempCanvas.Copy(xres: pixelsPerMm, yres: pixelsPerMm))
            {
                // 使用通用的 WriteToFile，它会根据后缀名自动选择 png 保存器
                //imageToSave.Tiffsave(localOutputPath, xres: pixelsPerMm, yres: pixelsPerMm);
                
                imageToSave.Pngsave(PatternPieceProduceImg);
            }

            patternPieceTask.PatternPieceProduceLocalImgUrl = PatternPieceProduceImg;

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

        return productionTasks;
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
}