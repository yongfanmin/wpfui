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
                    Cv2.Threshold(invertedMat, binaryMat, 127, 255, ThresholdTypes.Binary);

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

            // --- 最终颜色反转 (与您原有的逻辑保持一致) ---
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

            return Image.NewFromBuffer(outputMemory);
        }
        finally
        {
            // ... [资源释放逻辑保持不变] ...
        }
    }

    private static Mat GetThinningSkeleton(Mat binaryMat)
    {
        using (Mat skeleton = new Mat())
        {
            CvXImgProc.Thinning(binaryMat, skeleton, ThinningTypes.ZHANGSUEN);
            return skeleton.Clone(); // 返回一个独立的克隆
        }
    }

    /// <summary>
    /// 【辅助函数】使用形态学方法生成1像素骨架。
    /// </summary>
    /// <returns>一个1像素的骨架Mat对象（白底黑字）。</returns>
    private static Mat GetMorphologicalSkeleton(Mat binaryMat)
    {
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

            return skeleton.Clone(); // 返回一个独立的克隆
        }
    }

    // TODO 着是将两个算法合并成一个函数, 可能可以 增加 Threshold 的色彩识别范围就行 而不用两种算法进行合并以保留细节 ， 但需要验证
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
                    Cv2.Threshold(invertedMat, binaryMat, 24, 255, ThresholdTypes.Binary);

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

            return Image.NewFromBuffer(outputMemory);
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
}