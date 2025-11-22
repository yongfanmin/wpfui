// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace RectpackSharp;

public class RectanglePacker2
{
    /// <summary>
    /// 对矩形集合进行排版（装箱）。
    /// </summary>
    /// <param name="rectangles">需要排版的矩形列表（Span）。排版结果会直接修改此Span中元素的X, Y, Width, Height属性。</param>
    /// <param name="bounds">输出排版后的总边界框。</param>
    /// <param name="maxBoundsWidth">最大允许宽度（必须指定）。</param>
    /// <param name="maxBoundsHeight">最大允许高度。如果为null，则视为高度无限（Strip Packing）。</param>
    /// <param name="isRot90">是否允许矩形旋转90度以寻找更优解。</param>
    public static void Pack(Span<PackingRectangle> rectangles, out PackingRectangle bounds,
        uint? maxBoundsWidth = null, uint? maxBoundsHeight = null, bool isRot90 = false)
    {
        if (maxBoundsWidth == null)
        {
            throw new ArgumentNullException(nameof(maxBoundsWidth), "Must specify a maximum width for packing.");
        }

        uint binWidth = maxBoundsWidth.Value;
        uint binHeight = maxBoundsHeight ?? uint.MaxValue;

        // 1. 准备工作：计算SortKey并排序
        // 通常面积越大或边长越长越难排，所以优先排大的。
        // PackingRectangle.CompareTo 默认是降序（基于 -SortKey）。
        for (int i = 0; i < rectangles.Length; i++)
        {
            // 使用面积作为排序键值
            rectangles[i].SortKey = rectangles[i].Area;
        }

        // 注意：Span.Sort 使用的是结构体的 CompareTo
        rectangles.Sort();

        // 2. 初始化空闲矩形列表
        // 初始状态下，整个布料就是一个大的空闲矩形
        List<PackingRectangle> freeRectangles = new List<PackingRectangle>();
        freeRectangles.Add(new PackingRectangle(0, 0, binWidth, binHeight));

        uint finalWidth = 0;
        uint finalHeight = 0;

        // 3. 遍历所有矩形进行放置
        for (int i = 0; i < rectangles.Length; i++)
        {
            // 寻找最佳位置
            var bestNode = FindPositionForNewNode(
                rectangles[i].Width,
                rectangles[i].Height,
                freeRectangles,
                isRot90,
                out bool bestRotated);

            // 如果找到了位置（Height不为0说明找到了，因为我们初始化SortKey不影响IsPlaced判断，但这里最好用特定值判断）
            // 在MaxRects中，如果放不下，bestNode的高度通常表现为极大值或无法匹配
            // 这里我们假设只要maxBoundsHeight够大或者无限，总能放下。
            // 如果是固定大小且放不下，位置将保持默认（可能重叠或在0,0），实际应用需处理放不下的情况。

            if (bestNode.Height == 0 && bestNode.Width == 0 && bestNode.X == 0 && bestNode.Y == 0 &&
                rectangles[i].Area > 0)
            {
                // 无法放入（可能是固定大小限制了）
                // 简单的策略：跳过或标记。这里保留原坐标，但在实际生产中应标记为未打包。
                continue;
            }

            // 应用旋转
            if (bestRotated)
            {
                rectangles[i].Rotate();
            }

            // 更新矩形坐标
            rectangles[i].X = bestNode.X;
            rectangles[i].Y = bestNode.Y;

            // 4. 更新空闲矩形列表（核心：MaxRects Split）
            // 放置新矩形后，它会与现有的空闲矩形重叠，需要将重叠的空闲矩形切割
            int numRectanglesToProcess = freeRectangles.Count;
            for (int j = 0; j < numRectanglesToProcess; ++j)
            {
                if (SplitFreeNode(freeRectangles, j, rectangles[i]))
                {
                    freeRectangles.RemoveAt(j);
                    --j;
                    --numRectanglesToProcess;
                }
            }

            // 5. 清理被包含的空闲矩形（优化步骤）
            PruneFreeList(freeRectangles);

            // 6. 记录边界
            if (rectangles[i].Right > finalWidth) finalWidth = rectangles[i].Right;
            if (rectangles[i].Bottom > finalHeight) finalHeight = rectangles[i].Bottom;
        }

        bounds = new PackingRectangle(0, 0, finalWidth, finalHeight);
    }

    /// <summary>
    /// 寻找最佳放置位置（Best Short Side Fit 启发式策略）。
    /// </summary>
    private static PackingRectangle FindPositionForNewNode(
        uint width, uint height,
        List<PackingRectangle> freeRectangles,
        bool isRot90,
        out bool bestRotated)
    {
        PackingRectangle bestNode = new PackingRectangle();
        bestRotated = false;

        // 记录最佳评分，用于比较（Best Short Side Fit: 剩余短边越小越好）
        uint bestShortSideFit = uint.MaxValue;
        uint bestLongSideFit = uint.MaxValue;

        // 遍历所有空闲矩形
        foreach (var freeRect in freeRectangles)
        {
            // 尝试：不旋转
            if (freeRect.Width >= width && freeRect.Height >= height)
            {
                // 计算剩余空间
                uint leftoverHoriz = (uint)Math.Abs(freeRect.Width - width);
                uint leftoverVert = (uint)Math.Abs(freeRect.Height - height);
                uint shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                uint longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                // 评分规则：
                // 1. 优先最小化剩余短边 (BSSF)
                // 2. 如果短边相同，优先最小化长边
                // 3. 如果都相同，优先选择 Y 坐标较小的（为了 Strip Packing 尽可能往上排）
                if (shortSideFit < bestShortSideFit ||
                    (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit) ||
                    (shortSideFit == bestShortSideFit && longSideFit == bestLongSideFit && freeRect.Y < bestNode.Y))
                {
                    bestNode.X = freeRect.X;
                    bestNode.Y = freeRect.Y;
                    bestNode.Width = width;
                    bestNode.Height = height;
                    bestShortSideFit = shortSideFit;
                    bestLongSideFit = longSideFit;
                    bestRotated = false;
                }
            }

            // 尝试：旋转90度
            if (isRot90 && freeRect.Width >= height && freeRect.Height >= width)
            {
                uint leftoverHoriz = (uint)Math.Abs(freeRect.Width - height);
                uint leftoverVert = (uint)Math.Abs(freeRect.Height - width);
                uint shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                uint longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                if (shortSideFit < bestShortSideFit ||
                    (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit) ||
                    (shortSideFit == bestShortSideFit && longSideFit == bestLongSideFit && freeRect.Y < bestNode.Y))
                {
                    bestNode.X = freeRect.X;
                    bestNode.Y = freeRect.Y;
                    bestNode.Width = height; // 注意这里宽高互换
                    bestNode.Height = width;
                    bestShortSideFit = shortSideFit;
                    bestLongSideFit = longSideFit;
                    bestRotated = true;
                }
            }
        }

        return bestNode;
    }

    /// <summary>
    /// 切割空闲矩形。如果使用的矩形(usedNode)与空闲矩形(freeRect)相交，
    /// 则将空闲矩形切割成最多4个新的小空闲矩形，并添加到列表中。
    /// </summary>
    /// <returns>如果发生了相交（即原freeRect需要被移除）则返回true。</returns>
    private static bool SplitFreeNode(List<PackingRectangle> freeRectangles, int freeNodeIndex,
        PackingRectangle usedNode)
    {
        PackingRectangle freeNode = freeRectangles[freeNodeIndex];

        // 检查是否相交（利用 struct 中现有的 Intersects 逻辑，或者手动判断）
        // 这里手动判断更精确，因为我们要知道怎么切
        if (usedNode.X >= freeNode.Right || usedNode.Right <= freeNode.X ||
            usedNode.Y >= freeNode.Bottom || usedNode.Bottom <= freeNode.Y)
        {
            return false; // 不相交
        }

        // 开始切割，生成新的空闲矩形

        // 1. 顶部剩余
        if (usedNode.Y > freeNode.Y && usedNode.Y < freeNode.Bottom)
        {
            var newNode = freeNode;
            newNode.Height = usedNode.Y - newNode.Y;
            freeRectangles.Add(newNode);
        }

        // 2. 底部剩余
        if (usedNode.Bottom < freeNode.Bottom)
        {
            var newNode = freeNode;
            newNode.Y = usedNode.Bottom;
            newNode.Height = freeNode.Bottom - usedNode.Bottom;
            freeRectangles.Add(newNode);
        }

        // 3. 左侧剩余
        if (usedNode.X > freeNode.X && usedNode.X < freeNode.Right)
        {
            var newNode = freeNode;
            newNode.Width = usedNode.X - newNode.X;
            freeRectangles.Add(newNode);
        }

        // 4. 右侧剩余
        if (usedNode.Right < freeNode.Right)
        {
            var newNode = freeNode;
            newNode.X = usedNode.Right;
            newNode.Width = freeNode.Right - usedNode.Right;
            freeRectangles.Add(newNode);
        }

        return true;
    }

    /// <summary>
    /// 清理空闲列表：移除那些完全被另一个空闲矩形包含的小矩形，减少计算量。
    /// </summary>
    private static void PruneFreeList(List<PackingRectangle> freeRectangles)
    {
        for (int i = 0; i < freeRectangles.Count; ++i)
        {
            for (int j = i + 1; j < freeRectangles.Count; ++j)
            {
                if (freeRectangles[j].Contains(freeRectangles[i]))
                {
                    freeRectangles.RemoveAt(i);
                    --i;
                    break;
                }

                if (freeRectangles[i].Contains(freeRectangles[j]))
                {
                    freeRectangles.RemoveAt(j);
                    --j;
                }
            }
        }
    }
}