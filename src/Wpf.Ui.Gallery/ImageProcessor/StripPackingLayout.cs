// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using NetVips;
using RectpackSharp;
using Wpf.Ui.Gallery.Dto.PrintTask;
using Wpf.Ui.Gallery.LocalConfig;
using Wpf.Ui.Gallery.Utils;

namespace Wpf.Ui.Gallery.ImageProcessor;

// 条带打包/条带排版 卷轴排版算法 : 在一个宽度固定长度不固定的布料上进行印花排版的算法
// 算法如何实现? 使用DeepNest 直接所有裁片进行最省布料排版的问题: 1.运算量太大 穷举算出最优解 2.没见衣服的裁片/印花 不相邻 对于工人操作很麻烦
// 但是deepnest不支持天际线算法 而且 天际线算法只支持矩形
// 所以改造成: 单件使用deepnest最优算法排出长度最节约的排版, 然后给出排版后的 "多边形天际线" ; 然后在这个天际线之后继续排版下一件， 这样可以做到: 单件衣服不同裁片/印花连在一起方便工人裁剪和制作 2.用料也少 计算速度快
// 此算法问题: 1.不是全局布料最节约(但是问题不大 除了一些极端排版的情况 多数情况下很节约布料了)  2.不适合单印花生产多件(这种情况可能需要排版几件 然后进行连续打印 方便批量裁剪)
public class StripPackingLayout
{
    /*private Frontier currentFrontier; // 您的“轮廓线”或“天际线”数据结构

    public PackingResult Pack(List<ClothingGroup> groups)
    {
        // 1. 初始化天际线 (一条Y=0的直线)
        this.currentFrontier = new Frontier();

        // 2. 循环处理每一个组 (这就是您的贪心/序列化部分)
        foreach (var group in groups)
        {
            // 3. 基于当前天际线，动态创建一个“容器”多边形
            var containerForThisStep = this.currentFrontier.CreatePlacementContainer();

            // 4. 调用DeepNestSharp作为“放置引擎”！
            // 在这一小步里，DeepNestSharp会为当前组找到一个局部最优解
            var nestEngine = new DeepNestSharp.Nest();
            var placement = nestEngine.FindBestPlacementForGroup(group.Pieces, containerForThisStep);

            // 5. 将结果应用到画布，并更新您的天际线
            this.ApplyPlacement(placement);
            this.currentFrontier.Update(placement.Outline);
        }

        return this.GetFinalResult();
    }*/

    /*
    public Nfp ConvertOpenCvToDeepNestPolygon(Point[] opencvPoints)
    {
        if (opencvPoints == null || opencvPoints.Length < 3)
        {
            // 一个有效的多边形至少需要3个顶点
            return null;
        }

        // 1. 将 OpenCvSharp.Point (int) 转换为 DeepNestSharp.Placement.PointF (double)
        List<PointF> deepNestPoints = opencvPoints
            .Select(p => new PointF(p.X, p.Y)) // 这里的转换是核心
            .ToList();

        // 2. 使用转换后的点列表创建 Nfp 对象
        var polygon = new Nfp(deepNestPoints);

        // （可选）可以给这个多边形设置一个ID，便于后续识别
        // polygon.Source = ...;

        return polygon;
    }*/

    // 使用skyline算法进行矩形图片排版 支持旋转正负90度排版  自动排版
    public static LayoutResult SkylineLayout(List<LayoutImg> printImgList, uint machinePrintWidthPx )
    {
        
        
        

        var rectanglesToPack = new PackingRectangle[printImgList.Count];
        for (int i = 0; i < rectanglesToPack.Length; i++)
        {
            rectanglesToPack[i] = new PackingRectangle(0, 0, printImgList[i].WidthPx, printImgList[i].HeightPx,
                printImgList[i].Id);
        }

        // Store original dimensions
        var originalRectangles = new PackingRectangle[rectanglesToPack.Length];
        rectanglesToPack.CopyTo(originalRectangles, 0);

        /*枚举成员	含义解释	排序规则	适用场景
        TryByArea	按面积尝试	将所有矩形从大到小按面积排序后，再进行打包。	通用，最常用的策略之一。 先放大块的、难放的，有助于尽早确定布局骨架。
        TryByPerimeter	按周长尝试	将所有矩形从大到小按周长排序后，再进行打包。	与按面积类似，但更倾向于那些“细长”或“扁平”的矩形，因为它们的周长相对面积更大。
        TryByBiggerSide	按更长边尝试	对每个矩形，取其max(宽, 高)作为排序依据，从大到小排序。	通用，效果非常好。 优先处理那些尺寸最大的矩形，无论它们是高还是宽。
        TryByWidth	按宽度尝试	将所有矩形从大到小按宽度排序后，再进行打包。	在水平空间有限时，先放入最宽的矩形可以避免最后它们无处可放。
        TryByHeight	按高度尝试	将所有矩形从大到小按高度排序后，再进行打包。	条带打包的“黄金策略”。 在固定宽度、追求最短长度（高度）的场景下，先放入最高的矩形通常能得到最优或次优解。
        TryByPathologicalMultiplier	按病态乘数尝试	这是一个更高级的启发式规则，排序依据是 max(w,h) / min(w,h) * w * h。	专门用于优化那些宽高比差异极大的矩形（例如 10x800 和 800x10）。它会优先处理那些最“极端”的形状。*/
        // 设置打包提示，启用旋转
        var packingHint = PackingHints.TryByHeight | PackingHints.TryByWidth | PackingHints.TryByBiggerSide |
                          PackingHints.TryByArea;
        RectanglePacker.Pack(
            rectanglesToPack,
            out var bounds,
            packingHint,
            maxBoundsWidth: machinePrintWidthPx // 固定宽度 但是长度不定
        );

        /*Console.WriteLine($"在最大宽度为 {fabricWidth} 的画布上，成功打包了 {rectanglesToPack.Length} 个矩形。");
        Console.WriteLine($"最终占用的边界框为: {bounds.Width}x{bounds.Height}。");
        Console.WriteLine($"占用的最短长度为: {bounds.Height}。");

        Console.WriteLine("\n--- 各矩形位置 ---");*/

        List<LayoutImg> layoutImgList = new List<LayoutImg>();
        for (int i = 0; i < rectanglesToPack.Length; i++)
        {
            var packed = rectanglesToPack[i];
            var original = originalRectangles[i];
            var isRotated = packed.Width != original.Width;
            LayoutImg layoutImg = printImgList.Where(item => item.Id == packed.Id).FirstOrDefault();
            layoutImgList.Add(new LayoutImg()
            {
                WidthPx = packed.Width,
                HeightPx = packed.Height,
                Id = packed.Id,
                PositionX = packed.X,
                PositionY = packed.Y,
                ImgPath = layoutImg.ImgPath,
                LayoutCropImg = layoutImg.LayoutCropImg
            });
            /*Console.WriteLine(
                $"矩形: 原始尺寸({original.Width}x{original.Height}), " +
                $"放置位置: X={packed.X}, Y={packed.Y}, " +
                $"放置尺寸: {packed.Width}x{packed.Height} " +
                $"{(isRotated ? "(已旋转)" : "")}"
            );*/
        }

        return new LayoutResult()
        {
            LayoutWidthPx = bounds.Width, LayoutHeightPx = bounds.Height, LayoutImgList = layoutImgList,
        };
    }

    // 一件多印花图 必须相邻排版
    // 1.计算出单件衣服内 面积最大的印花图 的 短边 能被打印机出料宽度整除几次(n次), 然后循环排版n件到1件 ,算出 1->n 和 2n 每种排版方式 单件衣服占用面积|长度最小的情况, 再算出 1->和2n 能否被m件衣服整除, 能整除 则按照平均单件占用面积最小执行重复排版, 如果不整除 则再算出取余部分占用空间平摊到单件面积占用
    /*public static LayoutResult SkylineLayoutByOneProduct(List<string> printImgPathList, uint machinePrintWidthPx,
        int printImgPaddingPx)
    {
        
    }*/
}