// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using NetVips;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Converters;
using Wpf.Ui.Gallery.Dto.PrintTask;
using Wpf.Ui.Gallery.Utils;

namespace Wpf.Ui.Gallery.Services.Creator;

public class ImageCreator : IImageCreator
{
    public Image CreateImageFromPhysicalSize(
            double widthMm,
            double heightMm,
            int dpi,
            ImgSupportFormat format,
            double[]? backgroundColor = null)
        {
            // --- 1. 参数校验 ---
            if (widthMm <= 0) throw new ArgumentException("Width must be positive.", nameof(widthMm));
            if (heightMm <= 0) throw new ArgumentException("Height must be positive.", nameof(heightMm));
            if (dpi <= 0) throw new ArgumentException("DPI must be positive.", nameof(dpi));

            // --- 2. 决定通道数和默认背景色 ---
            int bands;
            bool supportsAlpha = format == ImgSupportFormat.Png || format == ImgSupportFormat.Tiff || format == ImgSupportFormat.Webp;

            if (backgroundColor != null)
            {
                bands = backgroundColor.Length;
                if (bands < 1 || bands > 4) throw new ArgumentException("Background color must have 1 to 4 bands.", nameof(backgroundColor));
                if (!supportsAlpha && bands % 2 == 0 && bands > 0) // e.g., RGBA for Jpeg
                {
                    // 如果格式不支持透明，但提供了带Alpha的颜色，可以记录一个警告或自动移除Alpha
                }
            }
            else
            {
                // 如果未提供背景色，根据格式选择默认值
                if (supportsAlpha)
                {
                    bands = 4; // Png/Tiff/Webp支持透明，默认为RGBA
                    backgroundColor = new double[] { 0, 0, 0, 0 }; // 透明黑
                }
                else
                {
                    bands = 3; // Jpeg不支持透明，默认为RGB
                    backgroundColor = new double[] { 255, 255, 255 }; // 白色
                }
            }

            // --- 3. 计算像素尺寸 (核心单位换算) ---
            int widthInPixels = (int)Math.Round((widthMm / ImageHelper.MillimetersPerInch) * dpi);
            int heightInPixels = (int)Math.Round((heightMm / ImageHelper.MillimetersPerInch) * dpi);
            if (widthInPixels == 0 || heightInPixels == 0) throw new ArgumentException("Calculated pixel dimensions are zero.");

            // --- 4. 创建图像 ---
            // 1. 直接使用数组，而不是 Memory<T>
            //double[] pixelData = backgroundColor;

            //using var pixel = Image.NewFromArray(pixelData);
    
            // 创建一个 1000x1000 像素的黑色图像，并包含一个 alpha 通道（4个通道：RGBA）
            // 通过将第四个通道（alpha）设置为0，我们使其完全透明。
            using Image image = Image.Black(widthInPixels, heightInPixels, bands: 4) + backgroundColor;

            // 将图像的色彩空间解释为 SRGB
            using Image newImage = image.Copy(interpretation: Enums.Interpretation.Srgb);
            
            // b. 使用 Embed 将这个单像素颜色“平铺”到一个指定尺寸的巨大画布上。
            //    extend: Enums.Extend.Copy 确保了颜色被复制填充。
            //    这里要使用正确的 heightInPixels 变量！
            //Image newImage = pixel.Embed(0, 0, widthInPixels, heightInPixels, extend: Enums.Extend.Copy);

            // --- 5. 设置DPI元数据 ---
            double pixelsPerInch = dpi / ImageHelper.MillimetersPerInch;
            // Copy 会创建一个新的 Image 实例，这是最终要返回的对象
            var finalImage = newImage.Copy(xres: pixelsPerInch, yres: pixelsPerInch);
            return finalImage;
        }
    

    /// <summary>
    /// Saves a NetVips image to a file with format-specific, high-quality options suitable for production.
    /// </summary>
    /// <param name="image">The image to save.</param>
    /// <param name="filePathWithoutExtension">The base path for the output file (e.g., "D:/output/my_image").</param>
    /// <param name="format">The format to save the image in, using your specific enum.</param>
    public void SaveImageForProduction(Image image, string filePathWithoutExtension, ImgSupportFormat format)
    {
        string fullPath;
        Directory.CreateDirectory(Path.GetDirectoryName(filePathWithoutExtension));
        switch (format)
        {
            case ImgSupportFormat.Jpeg:
                fullPath = filePathWithoutExtension + ImgFormat2Extend.GetExtend(format);
                // strip 移除EXIF信息 无法这样写
                //image.Jpegsave(fullPath, q: 95, strip: true, optimizeCoding: true);
                image.Jpegsave(fullPath, q: 95, optimizeCoding: true);
                break;

            case ImgSupportFormat.Png:
                fullPath = filePathWithoutExtension + ImgFormat2Extend.GetExtend(format);
                // image.Pngsave(fullPath, compression: 6, interlace: false, filter: Enums.PngFilter.All);
                image.Pngsave(fullPath, compression: 6, interlace: false, filter: Enums.ForeignPngFilter.All);
                break;

            case ImgSupportFormat.Tiff:
                fullPath = filePathWithoutExtension + ImgFormat2Extend.GetExtend(format);
                image.Tiffsave(fullPath, compression: Enums.ForeignTiffCompression.Lzw, tile: true, pyramid: true);
                break;

            case ImgSupportFormat.Webp:
                fullPath = filePathWithoutExtension + ImgFormat2Extend.GetExtend(format);
                // lossless: 无损压缩, q: 质量 (有损时)
                image.Webpsave(fullPath, lossless: true);
                break;

            default:
                throw new NotSupportedException($"Format {format} is not supported.");
        }

        Console.WriteLine($"图片已成功保存至: {fullPath}");
    }
}