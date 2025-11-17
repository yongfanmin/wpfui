// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.


using NetVips;
using System;
using System.Globalization;
using OpenCvSharp;
using OpenCvSharp.XImgProc;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Converters;
using Wpf.Ui.Gallery.LocalConfig;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.Rendering;
using Size = OpenCvSharp.Size;

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
    
    /// <summary>
    /// 将像素 (pixels) 转换为毫米 (mm)。
    /// </summary>
    /// <param name="pixels">要转换的像素值。</param>
    /// <param name="dpi">每英寸点数 (DPI)，定义了转换的分辨率。</param>
    /// <returns>对应的毫米值 (使用decimal以保持精度)。</returns>
    public static int ConvertPixelsToMm(int pixels, int dpi)
    {
        // 1. 参数校验
        if (dpi <= 0)
        {
            throw new ArgumentException("DPI值必须为正数。", nameof(dpi));
        }
        // 像素值可以为负数，表示相对位移，所以不校验pixels的正负

        // 2. 核心计算公式（逆向）：
        //    a. (像素 / DPI) -> 转换为英寸
        //    b. (英寸 * 25.4) -> 转换为毫米
        double millimeters = ((double)pixels / dpi) * MillimetersPerInch;

        // 3. 将结果转换为decimal类型并返回
        //    使用decimal可以更好地表示物理尺寸，避免浮点数精度问题
        return Convert.ToInt32(millimeters);
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

    /// <summary>
    /// 使用NetVips为图像四周添加指定宽度的透明边距。
    /// </summary>
    /// <param name="inputImage">输入的NetVips图像对象。</param>
    /// <param name="paddingCm">边距宽度（单位：厘米）。</param>
    /// <returns>一个添加了透明边距的新NetVips图像对象。</returns>
    public static Image AddTransparentPadding(Image inputImage, int paddingPx)
    {
        // 2. 计算新图像的总尺寸
        int newWidth = inputImage.Width + (2 * paddingPx);
        int newHeight = inputImage.Height + (2 * paddingPx);

        // 确保图像有Alpha通道，如果没有，则添加一个
        Image imageWithAlpha = inputImage.HasAlpha() ? inputImage : inputImage.BandjoinConst(new[] { 255d });

        // 3. 使用 Gravity 方法将原图放置在更大的画布中央
        //    - 第一个参数：对齐方式。Enums.CompassDirection.Centre 表示居中。
        //    - 第二、三个参数：新画布的宽度和高度。
        //    - "extend" 参数：定义画布空白区域的填充方式。
        //        Enums.Extend.Background 表示使用背景色填充。
        //    - "background" 参数：指定背景色。对于RGBA，[0, 0, 0, 0] 表示完全透明。
        Image paddedImage = imageWithAlpha.Gravity(
            Enums.CompassDirection.Centre,
            newWidth,
            newHeight,
            extend: Enums.Extend.Background,
            background: new double[] { 0, 0, 0, 0 }
        );
        imageWithAlpha.Dispose();
        return paddedImage;
    }

    // 获取尽可能接近两个像素的线 (会导致小于两个像素的细线可能断掉)
    // 图像抽取骨架 (为了保留细节的专色图层 ) 识别白色部分抽取骨架 所以先把透明通道 黑白翻转 抽取骨架后再 白黑翻转
    public static Image SkeletonizeWithOpenCvInvert(Image spotPlate, int targetThickness, bool invertFinalResult = true)
    {
        // ... [预检查代码] ...

        byte[] memoryBuffer = spotPlate.WriteToBuffer(ImgFormat2Extend.GetExtend(ImgSupportFormat.Png));

        Mat finalMat = null;
        Mat resultToEncode = null; // 新增一个变量来指向最终要编码的Mat

        try
        {
            using (Mat inputMat = Mat.ImDecode(memoryBuffer, ImreadModes.Grayscale))
            {
                if (inputMat == null || inputMat.Empty()) return spotPlate.Copy();

                // 预处理：颜色反转 + 阈值化
                using (Mat invertedMat = new Mat())
                using (Mat binaryMat = new Mat())
                {
                    Cv2.BitwiseNot(inputMat, invertedMat);
                    Cv2.Threshold(invertedMat, binaryMat, 127, 255, ThresholdTypes.Binary);

                    // 骨架化
                    using (Mat skeletonMat = new Mat())
                    {
                        if (binaryMat.Empty()) return spotPlate.Copy();

                        CvXImgProc.Thinning(binaryMat, skeletonMat, ThinningTypes.ZHANGSUEN);

                        // 厚度恢复
                        if (targetThickness <= 1)
                        {
                            finalMat = skeletonMat.Clone();
                        }
                        else
                        {
                            // ... [Dilate 逻辑] ...
                            int dilateAmount = (int)Math.Floor(targetThickness / 2.0);
                            if (dilateAmount > 0)
                            {
                                int kernelSize = dilateAmount * 2 + 1;
                                using (Mat dilateKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse,
                                           new Size(kernelSize, kernelSize)))
                                {
                                    finalMat = new Mat();
                                    Cv2.Dilate(skeletonMat, finalMat, dilateKernel);
                                }
                            }
                            else
                            {
                                finalMat = skeletonMat.Clone();
                            }
                        }
                    }
                }
            }

            if (finalMat == null || finalMat.Empty())
            {
                return spotPlate.Copy();
            }

            // =================================================================
            // --- 核心修正点: 在返回前，进行最终的颜色反转 ---
            // =================================================================
            if (invertFinalResult)
            {
                resultToEncode = new Mat();
                Cv2.BitwiseNot(finalMat, resultToEncode);
            }
            else
            {
                // 如果不需要反转，直接使用finalMat
                resultToEncode = finalMat;
            }
            // =================================================================

            // --- OpenCvSharp Mat -> NetVips Image ---
            byte[] outputMemory;
            Cv2.ImEncode(ImgFormat2Extend.GetExtend(ImgSupportFormat.Png), resultToEncode, out outputMemory);
            /*using (Image bwImage = Image.NewFromBuffer(outputMemory))
            {
                // 步骤 2: 使用 Copy() 方法创建一个新的图像头，
                // 并明确地将 Interpretation 设置为 sRGB。
                // 像素数据本身没有改变，只是改变了NetVips“看待”它的方式。
                Image srgbImage = bwImage.Copy(interpretation: Enums.Interpretation.Srgb);

                return srgbImage;
            }*/
            return Image.NewFromBuffer(outputMemory);
        }
        finally
        {
            // 确保我们创建的所有Mat都被释放
            finalMat?.Dispose();

            // 如果resultToEncode是一个新创建的Mat(即反转过)，也需要释放
            if (resultToEncode != null && resultToEncode != finalMat)
            {
                resultToEncode.Dispose();
            }
        }
    }

    // 尽可能的获取细线 但可能线变成不连贯的像素点
    public static Image SkeletonizeWithOpenCvInvertLinePixel(Image spotPlate, int targetThickness,
        bool invertFinalResult = true)
    {
        // ... [预检查代码] ...
        if (spotPlate.Max() < 1) return spotPlate.Copy();

        byte[] memoryBuffer = spotPlate.WriteToBuffer(ImgFormat2Extend.GetExtend(ImgSupportFormat.Png));

        Mat finalMat = null;
        Mat resultToEncode = null;

        try
        {
            using (Mat inputMat = Mat.ImDecode(memoryBuffer, ImreadModes.Grayscale))
            {
                if (inputMat == null || inputMat.Empty()) return spotPlate.Copy();

                // 预处理：颜色反转 + 阈值化 (与您原有的逻辑保持一致)
                using (Mat invertedMat = new Mat())
                using (Mat binaryMat = new Mat())
                {
                    Cv2.BitwiseNot(inputMat, invertedMat);
                    // 127 是 8位灰度 255的一半
                    // Cv2.Threshold(invertedMat, binaryMat, 127, 255, ThresholdTypes.Binary);
                    Cv2.Threshold(invertedMat, binaryMat, 5, 255, ThresholdTypes.Binary);

                    if (binaryMat.Empty()) return spotPlate.Copy();

                    // =================================================================
                    // --- 核心算法替换: 从 Thinning+Dilate 改为 形态学骨架 ---
                    // =================================================================

                    // 步骤 1: 计算距离变换图
                    using (Mat distMat = new Mat())
                    {
                        // 在二值图上计算距离
                        Cv2.DistanceTransform(binaryMat, distMat, DistanceTypes.L2, DistanceTransformMasks.Mask5);

                        // 步骤 2: 寻找局部最大值 (山脊线)，即1像素骨架
                        using (Mat dilatedDist = new Mat())
                        using (Mat skeletonMat = new Mat())
                        {
                            using (Mat kernel =
                                   Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3))) // 方形核更适合这里的比较
                            {
                                Cv2.Dilate(distMat, dilatedDist, kernel);
                            }

                            // 骨架点 = 原始距离图 与 扩张后的距离图 完全相等的地方
                            Cv2.Compare(distMat, dilatedDist, skeletonMat, CmpType.EQ);

                            // 步骤 3: 基于目标厚度，从距离图中生成最终结果
                            // 我们需要一个能同时满足“是骨架点”和“厚度达标”的区域
                            if (targetThickness <= 1)
                            {
                                finalMat = skeletonMat.Clone();
                            }
                            else
                            {
                                // 创建一个蒙版，只保留距离大于 (thickness-1)/2 的区域
                                double minDistance = (targetThickness - 1) / 2.0;
                                using (Mat thicknessMask = new Mat())
                                {
                                    // distMat > minDistance 会得到一个二值图
                                    Cv2.Compare(distMat, minDistance, thicknessMask, CmpType.GT); // GT = Greater Than

                                    // 最终结果 = 骨架点 AND 厚度蒙版 AND 原始形状
                                    finalMat = new Mat();
                                    Cv2.BitwiseAnd(skeletonMat, thicknessMask, finalMat);
                                    Cv2.BitwiseAnd(finalMat, binaryMat, finalMat); // 确保结果不出界
                                }
                            }
                        }
                    }
                    // =================================================================
                }
            }

            if (finalMat == null || finalMat.Empty())
            {
                return spotPlate.Copy();
            }

            // =================================================================
            // --- 核心修正点: 在返回前，进行最终的颜色反转 ---
            // =================================================================
            if (invertFinalResult)
            {
                resultToEncode = new Mat();
                Cv2.BitwiseNot(finalMat, resultToEncode);
            }
            else
            {
                // 如果不需要反转，直接使用finalMat
                resultToEncode = finalMat;
            }
            // =================================================================

            // --- OpenCvSharp Mat -> NetVips Image ---
            byte[] outputMemory;
            Cv2.ImEncode(ImgFormat2Extend.GetExtend(ImgSupportFormat.Png), resultToEncode, out outputMemory);
            /*using (Image bwImage = Image.NewFromBuffer(outputMemory))
            {
                // 步骤 2: 使用 Copy() 方法创建一个新的图像头，
                // 并明确地将 Interpretation 设置为 sRGB。
                // 像素数据本身没有改变，只是改变了NetVips“看待”它的方式。
                Image srgbImage = bwImage.Copy(interpretation: Enums.Interpretation.Srgb);

                return srgbImage;
            }*/
            return Image.NewFromBuffer(outputMemory);
        }
        finally
        {
            // 确保我们创建的所有Mat都被释放
            finalMat?.Dispose();

            // 如果resultToEncode是一个新创建的Mat(即反转过)，也需要释放
            if (resultToEncode != null && resultToEncode != finalMat)
            {
                resultToEncode.Dispose();
            }
        }
    }

    // 山脊算法 快速 并且山脊线比较连续(可用)  效果接近可用 细条文字过粗 63秒运算时间
    public static Image SkeletonizeWithOpenCvInvertFast(Image spotPlate, int targetThickness,
        bool invertFinalResult = true)
    {
        if (spotPlate.Max() < 1) return spotPlate.Copy();

        byte[] memoryBuffer = spotPlate.WriteToBuffer(ImgFormat2Extend.GetExtend(ImgSupportFormat.Png));

        Mat finalMat = null;
        Mat resultToEncode = null;

        try
        {
            using (Mat inputMat = Mat.ImDecode(memoryBuffer, ImreadModes.Grayscale))
            {
                if (inputMat == null || inputMat.Empty()) return spotPlate.Copy();

                using (Mat invertedMat = new Mat())
                using (Mat binaryMat = new Mat())
                {
                    Cv2.BitwiseNot(inputMat, invertedMat);
                    Cv2.Threshold(invertedMat, binaryMat, 127, 255, ThresholdTypes.Binary);
                    if (binaryMat.Empty()) return spotPlate.Copy();

                    // =================================================================
                    // --- 核心算法: 双路径处理与合并 ---
                    // =================================================================

                    // --- 路径 A: 处理粗壮主体 ---
                    Mat skeletonThick;
                    using (Mat thickParts = new Mat())
                    {
                        // A1. 使用开运算移除细线 (kernel size 3x3 会移除宽度<=2的线条)
                        using (Mat openKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                        {
                            Cv2.MorphologyEx(binaryMat, thickParts, MorphTypes.Open, openKernel);
                        }

                        // A2. 对粗壮部分进行降采样加速的Thinning
                        skeletonThick = GetAcceleratedSkeleton(thickParts);
                    }

                    // --- 路径 B: 处理纤细细节 ---
                    Mat skeletonThin;
                    using (Mat thinParts = new Mat())
                    {
                        // B1. 提取细线部分
                        Cv2.Subtract(binaryMat, skeletonThick, thinParts); // 修正：应该从binaryMat减去thickParts

                        // B2. 在全分辨率下对细线进行Thinning (速度很快)
                        skeletonThin = GetFullResSkeleton(thinParts);
                    }

                    // --- 步骤 C: 合并骨架 ---
                    using (Mat mergedSkeleton = new Mat())
                    {
                        Cv2.BitwiseOr(skeletonThick, skeletonThin, mergedSkeleton);

                        // 统一厚度恢复
                        if (targetThickness <= 1)
                        {
                            finalMat = mergedSkeleton.Clone();
                        }
                        else
                        {
                            int dilateAmount = (int)Math.Ceiling((targetThickness - 1) / 2.0);
                            if (dilateAmount > 0)
                            {
                                int kernelSize = dilateAmount * 2 + 1;
                                using (Mat dilateKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse,
                                           new Size(kernelSize, kernelSize)))
                                {
                                    finalMat = new Mat();
                                    Cv2.Dilate(mergedSkeleton, finalMat, dilateKernel);
                                }
                            }
                            else
                            {
                                finalMat = mergedSkeleton.Clone();
                            }
                        }
                    }

                    // 释放中间骨架
                    skeletonThick.Dispose();
                    skeletonThin.Dispose();
                    // =================================================================
                }
            }

            if (finalMat == null || finalMat.Empty())
            {
                return spotPlate.Copy();
            }

            // =================================================================
            // --- 核心修正点: 在返回前，进行最终的颜色反转 ---
            // =================================================================
            if (invertFinalResult)
            {
                resultToEncode = new Mat();
                Cv2.BitwiseNot(finalMat, resultToEncode);
            }
            else
            {
                // 如果不需要反转，直接使用finalMat
                resultToEncode = finalMat;
            }
            // =================================================================

            // --- OpenCvSharp Mat -> NetVips Image ---
            byte[] outputMemory;
            Cv2.ImEncode(ImgFormat2Extend.GetExtend(ImgSupportFormat.Png), resultToEncode, out outputMemory);
            /*using (Image bwImage = Image.NewFromBuffer(outputMemory))
            {
                // 步骤 2: 使用 Copy() 方法创建一个新的图像头，
                // 并明确地将 Interpretation 设置为 sRGB。
                // 像素数据本身没有改变，只是改变了NetVips“看待”它的方式。
                Image srgbImage = bwImage.Copy(interpretation: Enums.Interpretation.Srgb);

                return srgbImage;
            }*/
            using (var image = Image.NewFromBuffer(outputMemory))
            {
                return image.Copy();
            }
        }
        finally
        {
            // 确保我们创建的所有Mat都被释放
            finalMat?.Dispose();

            // 如果resultToEncode是一个新创建的Mat(即反转过)，也需要释放
            if (resultToEncode != null && resultToEncode != finalMat)
            {
                resultToEncode.Dispose();
            }
        }
    }

// --- 辅助函数 ---

    /// <summary>
    /// 【辅助】对图像进行降采样加速的Thinning
    /// </summary>
    private static Mat GetAcceleratedSkeleton(Mat input)
    {
        if (input.Empty()) return new Mat();

        double scale = 2.0;
        using (Mat smallMat = new Mat())
        {
            Cv2.Resize(input, smallMat, new Size(input.Width / scale, input.Height / scale));
            using (Mat smallSkeleton = new Mat())
            {
                CvXImgProc.Thinning(smallMat, smallSkeleton, ThinningTypes.ZHANGSUEN);
                using (Mat largeSkeleton = new Mat())
                {
                    Cv2.Resize(smallSkeleton, largeSkeleton, input.Size());
                    using (Mat cleanSkeleton = new Mat())
                    {
                        Cv2.Threshold(largeSkeleton, cleanSkeleton, 127, 255, ThresholdTypes.Binary);
                        return cleanSkeleton.Clone();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 【辅助】在全分辨率下对稀疏图像进行Thinning
    /// </summary>
    private static Mat GetFullResSkeleton(Mat input)
    {
        if (input.Empty()) return new Mat();

        using (Mat skeleton = new Mat())
        {
            CvXImgProc.Thinning(input, skeleton, ThinningTypes.ZHANGSUEN);
            return skeleton.Clone();
        }
    }

    // 此算法 消耗大量时间 性能极差
    private static Mat GetThinningSkeleton(Mat binaryMat)
    {
        using (Mat skeleton = new Mat())
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            CvXImgProc.Thinning(binaryMat, skeleton, ThinningTypes.ZHANGSUEN);

            watch.Stop();
            Console.WriteLine($"GetThinningSkeleton耗时{watch.ElapsedMilliseconds}");
            return skeleton.Clone(); // 返回一个独立的克隆
        }
    }

    /// <summary>
    /// 【辅助函数】使用形态学方法生成1像素骨架。
    /// </summary>
    /// <returns>一个1像素的骨架Mat对象（白底黑字）。</returns>
    private static Mat GetMorphologicalSkeleton(Mat binaryMat)
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();

        using (Mat distMat = new Mat())
        using (Mat dilatedDist = new Mat())
        using (Mat skeleton = new Mat())
        {
            Cv2.DistanceTransform(binaryMat, distMat, DistanceTypes.L2, DistanceTransformMasks.Mask5);
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)))
            {
                Cv2.Dilate(distMat, dilatedDist, kernel);
            }

            Cv2.Compare(distMat, dilatedDist, skeleton, CmpType.EQ);

            // 确保骨架不出界
            Cv2.BitwiseAnd(skeleton, binaryMat, skeleton);
            watch.Stop();
            Console.WriteLine($"GetMorphologicalSkeleton耗时{watch.ElapsedMilliseconds}");
            return skeleton.Clone(); // 返回一个独立的克隆
        }
    }

    // TODO 着是将两个算法合并成一个函数, 可能可以 增加 Threshold 的色彩识别范围就行 而不用两种算法进行合并以保留细节 ， 但需要验证  效果接近完美 耗时70秒
    public static Image UnifiedSkeletonize(Image spotPlate, int targetThickness, bool invertFinalResult = true)
    {
        if (spotPlate.Max() < 1) return spotPlate.Copy();

        byte[] memoryBuffer = spotPlate.WriteToBuffer(ImgFormat2Extend.GetExtend(ImgSupportFormat.Png));

        Mat finalMat = null;
        Mat resultToEncode = null;

        try
        {
            using (Mat inputMat = Mat.ImDecode(memoryBuffer, ImreadModes.Grayscale))
            {
                if (inputMat == null || inputMat.Empty()) return spotPlate.Copy();

                // --- 统一的预处理 ---
                using (Mat invertedMat = new Mat())
                using (Mat binaryMat = new Mat())
                {
                    Cv2.BitwiseNot(inputMat, invertedMat);
                    // 127 是 8位灰度 255的一半
                    // Cv2.Threshold(invertedMat, binaryMat, 127, 255, ThresholdTypes.Binary);
                    // 第一个数字 thresh 值 越大,则只有在越清晰的像素部分才会打印白墨专色通道
                    Cv2.Threshold(invertedMat, binaryMat,
                        LocalAppConfig.AppSetting.PrintTaskConfig.WhiteInkEdgeStrength, 255, ThresholdTypes.Binary);

                    if (binaryMat.Empty()) return spotPlate.Copy();

                    // =================================================================
                    // --- 核心逻辑: 独立计算 -> 合并 -> 统一厚度控制 ---
                    // =================================================================

                    // 步骤 1: 独立计算两种1像素骨架
                    using (Mat skeletonA = GetThinningSkeleton(binaryMat))
                    using (Mat skeletonB = GetMorphologicalSkeleton(binaryMat))
                    {
                        // 步骤 2: 合并骨架，取并集
                        using (Mat mergedSkeleton = new Mat())
                        {
                            Cv2.BitwiseOr(skeletonA, skeletonB, mergedSkeleton);

                            // 步骤 3: 统一厚度控制
                            if (targetThickness <= 1)
                            {
                                finalMat = mergedSkeleton.Clone();
                            }
                            else
                            {
                                // 计算需要扩张的像素量，以达到目标厚度
                                // (targetThickness - 1) 是因为骨架本身已有1像素厚度
                                int dilateAmount = (int)Math.Ceiling((targetThickness - 1) / 2.0);

                                if (dilateAmount > 0)
                                {
                                    int kernelSize = dilateAmount * 2 + 1;
                                    using (Mat dilateKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse,
                                               new Size(kernelSize, kernelSize)))
                                    {
                                        finalMat = new Mat();
                                        Cv2.Dilate(mergedSkeleton, finalMat, dilateKernel);
                                    }
                                }
                                else
                                {
                                    finalMat = mergedSkeleton.Clone();
                                }
                            }
                        }
                    }
                }
            }

            if (finalMat == null || finalMat.Empty())
            {
                return spotPlate.Copy();
            }

            // --- 最终颜色反转 ---
            if (invertFinalResult)
            {
                resultToEncode = new Mat();
                Cv2.BitwiseNot(finalMat, resultToEncode);
            }
            else
            {
                resultToEncode = finalMat;
            }

            // --- OpenCvSharp Mat -> NetVips Image ---
            byte[] outputMemory;
            Cv2.ImEncode(ImgFormat2Extend.GetExtend(ImgSupportFormat.Png), resultToEncode, out outputMemory);

            using (var image = Image.NewFromBuffer(outputMemory))
            {
                return image.Copy();
            }
        }
        finally
        {
            finalMat?.Dispose();
            if (resultToEncode != null && resultToEncode != finalMat)
            {
                resultToEncode.Dispose();
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

    /** 生成带有自定义颜色边框和背景的二维码。
    * </summary>
    * <param name="content">二维码内容</param>
    * <param name="width">最终图像的总宽度</param>
    * <param name="height">最终图像的总高度</param>
    * <param name="margin">红色边框的宽度</param>
    * <returns>一个包含二维码的 NetVips.Image 对象，如果失败则返回 null</returns>*/
    public static Image? GenerateQrCodeWithBorder(string content, int width, int height, int margin = 1)
    {
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        if (margin < 0)
        {
            margin = 0;
        }
        
        try
        {
            // --- 步骤 1: 生成黑底透明背景的二维码核心 ---
            var qrCodeWriter = new BarcodeWriter<SvgRenderer.SvgImage>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = width, Height = height, Margin = 0, ErrorCorrection = ErrorCorrectionLevel.M,
                },
                Renderer = new SvgRenderer()
            };
            var svgImage = qrCodeWriter.Write(content);
            byte[] svgBytes = Encoding.UTF8.GetBytes(svgImage.Content);

            // 从SVG加载图像，得到一个黑底透明背景的4通道RGBA图像
            using var qrCodeTransparentBg = Image.NewFromBuffer(svgBytes)
                .ThumbnailImage(width, height: height);

            // --- 步骤 2: 将透明背景替换为白色背景 ---
            // 使用 Flatten 将透明部分替换为纯白色。
            // Flatten 会移除Alpha通道，结果是一个3通道的RGB图像。
            using var qrWithWhiteBg = qrCodeTransparentBg.Flatten(background: new double[] { 255, 255, 255 });

            // --- 步骤 3: 创建红色底图并插入二维码 ---
            // 直接创建一个3通道的红色背景
            using var redBackground = Image.Black(width + margin*2, height + margin*2, bands: 3)
                .Linear(new double[] { 0, 0, 0 }, new double[] { 255, 0, 0 });
            // 注意: Linear的更简洁写法是 (a * input + b)。要得到纯色，a=0, b=颜色值。

            // 使用 Insert 将黑白的二维码图像插入到红色背景的中心。Insert效率高于Composite。
            using var finalRgbImage = redBackground.Insert(qrWithWhiteBg, margin, margin);

            // --- 步骤 4: 为最终图像添加一个完全不透明的Alpha通道 ---
            // 这样可以确保返回的图像一定是4通道RGBA，方便后续统一处理
            return finalRgbImage.Bandjoin(255).Copy(interpretation: Enums.Interpretation.Srgb);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成二维码失败: {ex.Message}");
            return null;
        }
    }
    
     // 根据最大画布宽高自适应文字大小 创建一个纯文字画布
     // 健壮性判断 不传字体 "Arial"字体也不存在. 会尽量找到一个字体进行打印 完全没字体才会报错 
       public static Image? CreateTextImage(
        string textToPrint,
        int? maxWidthMm = null,
        int? maxHeightMm = null,
        int heightMm = 20,
        int dpi = 300,
        string fontName = "Arial",
        decimal paddingMm = 1.0m)
    {
        if (string.IsNullOrEmpty(textToPrint))
        {
            return null;
        }

        try
        {
            int paddingInPixels = paddingMm > 0 ? ConvertMmToPixels(paddingMm, dpi) : 0;
            Image? textImage = null;
            int finalWidthInPixels = ConvertMmToPixels(maxWidthMm.Value, dpi);
            int finalHeightInPixels = ConvertMmToPixels(maxHeightMm.Value, dpi);

            int textRenderMaxWidth = finalWidthInPixels - (2 * paddingInPixels);
            int textRenderMaxHeight = finalHeightInPixels - (2 * paddingInPixels);
            if (maxWidthMm.HasValue && maxWidthMm.Value > 0 && maxHeightMm.HasValue && maxHeightMm.Value > 0)
            {
                // **【自适应字体和缩放逻辑】**
               

                if (textRenderMaxWidth <= 0 || textRenderMaxHeight <= 0) return null;

                // 步骤A: 使用二分查找找到一个“接近最佳”的字体大小
                int minFontSize = 1, maxFontSize = textRenderMaxHeight, optimalFontSize = 0;
                while (minFontSize <= maxFontSize)
                {
                    int currentFontSize = minFontSize + (maxFontSize - minFontSize) / 2;
                    if (currentFontSize == 0) break;

                    using (var tempMask = TryCreateTextMask(textToPrint, fontName, currentFontSize, textRenderMaxWidth, dpi))
                    {
                        if (tempMask == null) throw new Exception("系统中未找到任何可用的字体进行渲染。");
                        
                        // 检查是否超出边界
                        if (tempMask.Width <= textRenderMaxWidth && tempMask.Height <= textRenderMaxHeight)
                        {
                            optimalFontSize = currentFontSize;
                            minFontSize = currentFontSize + 1; // 尝试更大
                        }
                        else
                        {
                            maxFontSize = currentFontSize - 1; // 太大了，尝试更小
                        }
                    }
                }

                if (optimalFontSize == 0)
                {
                    Console.WriteLine("错误：即使使用最小字体，文本也无法在指定尺寸内容纳。");
                    return null;
                }

                // 步骤B: 【修复关键】使用找到的最佳字体进行一次高质量渲染
                using (var rawTextImage = CreateBlackOnWhiteTextImage(textToPrint, fontName, optimalFontSize, textRenderMaxWidth, dpi))
                {
                    if (rawTextImage == null) return null; // 字体渲染失败
                    
                    // 步骤C: 计算精确的缩放比例以适应边界
                    double hScale = (double)textRenderMaxWidth / rawTextImage.Width;
                    double vScale = (double)textRenderMaxHeight / rawTextImage.Height;
                    double scale = Math.Min(hScale, vScale); // 取较小的比例以确保等比缩放后能完全放入

                    // 如果需要缩小，则执行Resize操作
                    if (scale < 1.0)
                    {
                        textImage = rawTextImage.Resize(scale, kernel: Enums.Kernel.Lanczos3);
                    }
                    else
                    {
                        textImage = rawTextImage.Copy();
                    }
                }

                // 步骤D: 将最终的文字图像居中放置在固定大小的画布上
                var whitePixel = new double[] { 255, 255, 255 };
                int leftOffset = (finalWidthInPixels - textImage.Width) / 2;
                int topOffset = (finalHeightInPixels - textImage.Height) / 2;

                return textImage.Embed(leftOffset, topOffset,
                                       finalWidthInPixels,
                                       finalHeightInPixels,
                                       extend: Enums.Extend.Background,
                                       background: whitePixel);
            }
            else
            {
                // **【固定高度逻辑】** (保持不变)
                int textRenderHeight = ConvertMmToPixels(heightMm, dpi);
                if (maxWidthMm.HasValue && maxWidthMm.Value > 0)
                {
                    textRenderMaxWidth = ConvertMmToPixels(maxWidthMm.Value, dpi) - (2 * paddingInPixels);
                }
                textImage = CreateBlackOnWhiteTextImage(textToPrint, fontName, textRenderHeight, textRenderMaxWidth, dpi);
                
                if (textImage == null) return null;

                var whitePixel = new double[] { 255, 255, 255 };
                return textImage.Embed(paddingInPixels, paddingInPixels,
                                       textImage.Width + (2 * paddingInPixels),
                                       textImage.Height + (2 * paddingInPixels),
                                       extend: Enums.Extend.Background,
                                       background: whitePixel);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成文字图像失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 【新】辅助方法：从文本直接创建黑字白底的RGB图像
    /// </summary>
    private static Image? CreateBlackOnWhiteTextImage(string text, string preferredFont, int fontSize, int? maxWidth, int dpi)
    {
        using (var textMask = TryCreateTextMask(text, preferredFont, fontSize, maxWidth, dpi))
        {
            if (textMask == null) return null;
            return textMask.Ifthenelse(new double[] { 0, 0, 0 }, new double[] { 255, 255, 255 });
        }
    }
    
    /// <summary>
    /// 辅助方法：尝试使用一个字体列表来创建文字遮罩，并优化了换行模式。
    /// </summary>
    private static Image? TryCreateTextMask(string text, string preferredFont, int fontSize, int? maxWidth, int dpi)
    {
        var fontFallbacks = new List<string> { "Microsoft YaHei", "PingFang SC", "WenQuanYi Micro Hei", "Arial", "Helvetica", "DejaVu Sans", "Noto Sans", "Verdana", "Calibri", "sans-serif" };
        var fontsToTry = new List<string> { preferredFont };
        fontsToTry.AddRange(fontFallbacks);
        fontsToTry = fontsToTry.Distinct().ToList();

        foreach (var font in fontsToTry)
        {
            try
            {
                return Image.Text(
                    text,
                    font: $"{font} {fontSize}px",
                    width: maxWidth,
                    // 【修复】对于混合文本，Char换行通常比Word更可靠
                    wrap: maxWidth.HasValue ? Enums.TextWrap.Char : null,
                    align: maxWidth.HasValue ? Enums.Align.Low : Enums.Align.Centre,
                    dpi: dpi
                );
            }
            catch (VipsException ex)
            {
                if (ex.Message.Contains("font", StringComparison.OrdinalIgnoreCase) && ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    // 忽略字体未找到的异常，继续尝试下一个
                }
                else { throw; }
            }
        }
        return null;
    }

    // 将图片等比放大到目标高度
    public static Image ScaleImageToHeight(
        Image sourceImage, 
        decimal targetHeightMm, 
        int dpi = 300,
        Enums.Kernel kernel = Enums.Kernel.Lanczos3)
    {
        if (sourceImage == null)
        {
            throw new ArgumentNullException(nameof(sourceImage), "源图像不能为空。");
        }
        if (targetHeightMm <= 0)
        {
            throw new ArgumentException("目标高度必须为正数。", nameof(targetHeightMm));
        }
        if (dpi <= 0)
        {
            throw new ArgumentException("DPI值必须为正数。", nameof(dpi));
        }

        try
        {
            // 步骤 1: 将目标高度从毫米转换为像素
            int targetHeightInPixels = ConvertMmToPixels(targetHeightMm, dpi);

            // 如果目标像素高度已经和当前高度相同，则无需缩放，直接返回副本
            if (targetHeightInPixels == sourceImage.Height)
            {
                return sourceImage.Copy();
            }

            // 步骤 2: 计算等比缩放因子
            // scale = 目标尺寸 / 原始尺寸
            double scaleFactor = (double)targetHeightInPixels / sourceImage.Height;

            // 步骤 3: 使用 Resize 方法执行等比缩放
            // Resize 方法接受一个统一的缩放因子，会自动应用于宽度和高度
            // kernel 参数指定了插值算法，Lanczos3 在放大时能提供高质量的结果
            return sourceImage.Resize(scaleFactor, kernel: kernel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"图像缩放失败: {ex.Message}");
            // 在失败时抛出异常，以便调用者可以处理
            throw;
        }
    }
    
    public static Image ResizeImageToHeight(
        string filePath, 
        int targetHeight, 
        Enums.Kernel kernel = Enums.Kernel.Lanczos3)
    {
        // 1. 参数校验
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("文件路径不能为空。", nameof(filePath));
        }
        if (targetHeight <= 0)
        {
            throw new ArgumentException("目标高度必须为正数。", nameof(targetHeight));
        }

        // 2. 从文件加载图像。使用 using 语句确保加载的图像在操作完成后被释放。
        using (var sourceImage = Image.NewFromFile(filePath))
        {
            // 如果原始高度与目标高度相同，则无需处理，直接返回一个副本
            if (sourceImage.Height == targetHeight)
            {
                return sourceImage.Copy();
            }

            // 3. 计算等比缩放因子
            // 必须进行浮点数除法，否则整数除法可能会得到0或1
            double scaleFactor = (double)targetHeight / sourceImage.Height;

            // 4. 执行缩放并返回新图像
            // Resize 方法会返回一个新的 Image 对象，源图像 sourceImage 会在 using 块结束时被自动释放
            return sourceImage.Resize(scaleFactor, kernel: kernel);
        }
    }
}