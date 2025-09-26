// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.


using NetVips;
using System;
using System.Globalization;
using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Utils;

public static class ImageHelper
{
    /// <summary>
    /// 常量：1英寸等于多少毫米。
    /// </summary>
    public const double MillimetersPerInch = 25.4;

    /// <summary>
    /// 根据当前像素宽度、目标物理宽度(毫米)和目标DPI，计算新的像素宽度和缩放比例。
    /// </summary>
    /// <param name="currentPixelWidth">当前图片的宽度（单位：像素）。</param>
    /// <param name="targetMillimeterWidth">目标图片的期望物理宽度（单位：毫米）。</param>
    /// <param name="targetDpi">目标图片的期望分辨率（单位：DPI）。</param>
    /// <returns>返回一个 ImageResizeInfo 对象，包含新的像素宽度和缩放比例。</returns>
    /// <exception cref="ArgumentException">如果任何输入参数小于或等于零，则抛出异常。</exception>
    public static double pixelSizeToPhysicalSizeNeedScale(
        int currentPixelWidth,
        decimal targetMillimeterWidth,
        int targetDpi)
    {
        // --- 参数校验 ---
        if (currentPixelWidth <= 0)
            throw new ArgumentException("当前像素宽度必须为正数。", nameof(currentPixelWidth));
        if (targetMillimeterWidth <= 0)
            throw new ArgumentException("目标毫米宽度必须为正数。", nameof(targetMillimeterWidth));
        if (targetDpi <= 0)
            throw new ArgumentException("目标DPI必须为正数。", nameof(targetDpi));

        // --- Step 1: 计算目标物理尺寸在目标DPI下需要多少像素 ---

        // 1a. 将目标的毫米宽度转换为英寸
        double targetWidthInches = decimal.ToDouble(targetMillimeterWidth) / MillimetersPerInch;

        // 1b. 将英寸乘以目标DPI，得到最终需要的像素数
        double requiredPixelsDouble = targetWidthInches * targetDpi;
        int requiredPixelsInt = (int)Math.Round(requiredPixelsDouble);

        // --- Step 2: 计算从当前像素到目标像素的缩放比例 ---
        double scaleFactor = requiredPixelsDouble / currentPixelWidth;
        return scaleFactor;
    }

    /// <summary>
    /// 将给定的物理长度（毫米）和分辨率（DPI）转换为像素数量。
    /// </summary>
    /// <param name="millimeters">要转换的物理长度，单位为毫米。</param>
    /// <param name="dpi">目标分辨率，单位为每英寸点数 (Dots Per Inch)。</param>
    /// <returns>计算出的像素数量，四舍五入到最接近的整数。</returns>
    /// <exception cref="ArgumentException">如果毫米或DPI的值不是正数，则抛出异常。</exception>
    public static int ConvertMmToPixels(decimal millimeters, int dpi)
    {
        double millimeter = decimal.ToDouble(millimeters);
        // 1. 参数校验，确保输入有效
        /*if (millimeters < 0)
        {
            // 有时是用来位移多少厘米 可能为负数  后续需要加强判断
            throw new ArgumentException("毫米值不能为负数。", nameof(millimeters));
        }*/
        if (dpi <= 0)
        {
            throw new ArgumentException("DPI值必须为正数。", nameof(dpi));
        }

        // 2. 核心计算公式：
        //    a. 先将毫米转换为英寸 (毫米 / 25.4)
        //    b. 再将英寸乘以DPI得到总像素数
        double pixels = (millimeter / MillimetersPerInch) * dpi;

        // 3. 将结果四舍五入为最接近的整数并返回
        //    因为像素不能是小数
        return (int)Math.Round(pixels);
    }

    // 计算矩形的外接圆的外接矩形的宽高 (用于平铺渲染 预先渲染一个最大图 避免平铺图进行旋转的时候部分底版缺少印花图虚渲染)
    public static (double Width, double Height) getTileSafeBackgroundSize(
        double backgroundWidth,
        double backgroundHeight,
        double imgWidth,
        double imgHeight
    )
    {
        // 因为图片需要进行偏移量位移对位, 偏移量最大校正值等于图片的宽高, 所以需要加上图片的宽高两倍(移动到裁片四个角顶点上 印花图外溢的情况)才是安全值
        backgroundWidth = (backgroundWidth + (imgWidth * 2));
        backgroundHeight = (backgroundHeight + (imgHeight * 2));
        // 参数校验，确保输入的宽高是有效的
        if (backgroundWidth < 0 || backgroundHeight < 0)
        {
            throw new ArgumentException("Width and height must be non-negative.");
        }

        // 1. 使用勾股定理计算对角线的平方
        //    为了避免不必要的中间变量，我们可以直接计算
        double diagonalSquared = (backgroundWidth * backgroundWidth) + (backgroundHeight * backgroundHeight);

        // 2. 计算平方根，得到对角线长度
        double diagonalLength = Math.Sqrt(diagonalSquared);

        // 3. 对角线的长度就是最终正方形的边长
        //    返回一个元组(tuple)，清晰地表示宽度和高度
        return (diagonalLength, diagonalLength);
    }
    
    
    // 图片居中裁剪保留
    public static Image CropFromCenter(Image sourceImage, int cropWidth, int cropHeight)
    {
        // --- 1. 参数校验 ---
        if (sourceImage == null)
        {
            throw new ArgumentNullException(nameof(sourceImage));
        }

        if (cropWidth <= 0 || cropHeight <= 0)
        {
            throw new ArgumentException("Crop dimensions must be positive.");
        }
            
        // 确保裁剪尺寸不超过原始图像尺寸
        if (cropWidth > sourceImage.Width || cropHeight > sourceImage.Height)
        {
            throw new ArgumentException("Crop dimensions cannot be larger than the source image.");
        }

        // --- 2. 计算居中位置的左上角坐标 ---
        int left = (sourceImage.Width - cropWidth) / 2;
        int top = (sourceImage.Height - cropHeight) / 2;

        // --- 3. 执行裁剪操作 ---
        // .Crop() 方法会返回一个新的 Image 对象
        Image croppedImage = sourceImage.Crop(left, top, cropWidth, cropHeight);

        return croppedImage;
    }
}