// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Dto.CreateImg;

namespace Wpf.Ui.Gallery.Dto.FormatAdapter;

using System.Globalization;
using System.Text.RegularExpressions;

public class ProductionTaskBuilder
{
    /// <summary>
    /// 从API返回的订单信息中，构建出所有需要执行的生产图合成任务列表。
    /// </summary>
    /// <param name="order">从API反序列化得到的订单对象。</param>
    /// <returns>一个包含所有待处理任务的列表。</returns>
    public List<ProductionTask> BuildTasksFromOrder(OrderPrintBatch order)
    {
        var tasks = new List<ProductionTask>();

        // 遍历所有视图 (后片、前片、袖子等)
        foreach (var viewIdKey in order.ProducePrintInfo.Keys)
        {
            var printInfo = order.ProducePrintInfo[viewIdKey];

            // 一个视图可能对应多个裁片
            foreach (var piecePair in printInfo.PatternPieces)
            {
                var patternPiece = piecePair.Value;

                // 创建一个针对此裁片的新生产任务
                var task = new ProductionTask
                {
                    PatternPieceTitle = patternPiece.Title,
                    FactoryId = order.FactoryId,
                    TaskId = $"{order.ProduceBatchNumber}-{patternPiece.Title}",
                    DesignProductId = order.DesignProductId,
                    OrderNo = order.OrderNo,
                    ViewId = printInfo.ViewId,
                    PatternPieceImageUrl = patternPiece.PatternPieceImageUrl,
                    // CuttingPieceTargetWidthCm = decimal.Parse(cuttingPiece.WidthCm, CultureInfo.InvariantCulture),
                    // CuttingPieceTargetHeightCm = decimal.Parse(cuttingPiece.HeightCm, CultureInfo.InvariantCulture),
                    PatternPieceTargetWidthMm = printInfo.realSizeWidthMm,
                    // 公版设计的时候 宽高相等才成立
                    PatternPieceTargetHeightMm = printInfo.realSizeWidthMm,
                    TargetDpi = printInfo.TargetDpi,
                    PrintLayers = new List<PrintLayerInfo>()
                };

                // 检查此视图上是否有对应的印花图配置
                if (order.ProductConfig.TryGetValue(viewIdKey, out var configItems))
                {
                    // 一个裁片上可能叠加了多个印花图
                    foreach (var configItem in configItems)
                    {
                        var designImage = configItem.Image;
                        
                        // 解析复杂的CSS变换矩阵 然后把居中变换和用户手动变换的矩阵信息合并
                        var centerTransform = ParseTransformMatrix(designImage.CenterTransform);
                        var userTransform = ParseTransformMatrix(designImage.UserTransform);
                        var transform = MergeMatrix(centerTransform, userTransform);
                        var layer = new PrintLayerInfo
                        {
                            GalleryId = designImage.GalleryId,
                            DesignImageUrl = designImage.DesignImageUrl,
                            DesignImageSizeMm = designImage.DimensionsMm,
                            ScaleX = transform.ScaleX,
                            ScaleY = transform.ScaleY,
                            TranslateX = transform.TranslateX,
                            TranslateY = transform.TranslateY,
                            Rotation = designImage.Rotate,
                            //目前 裁片多印花图的层级索引等于裁片印花图的序号
                            ZIndex = configItem.ViewId
                        };
                        task.PrintLayers.Add(layer);
                    }
                }
                
                tasks.Add(task);
            }
        }

        return tasks;
    }

    /// <summary>
    /// 解析 CSS transform matrix 字符串的辅助方法。
    /// </summary>
    private DesignMatrix ParseTransformMatrix(string matrix)
    {
        DesignMatrix designMatrix = new DesignMatrix();
        var match = Regex.Match(matrix, @"matrix\(([^,]+),[^,]+,[^,]+,([^,]+),([^,]+),([^,]+)\)");
        if (match.Success)
        {
            
            designMatrix.ScaleX = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            designMatrix.ScaleY = decimal.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            designMatrix.TranslateX = decimal.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            designMatrix.TranslateY = decimal.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            return designMatrix;
        }
        // 返回默认值或抛出异常
        return designMatrix;
    }

    private DesignMatrix MergeMatrix(DesignMatrix first, DesignMatrix second)
    {
        DesignMatrix designMatrix = new DesignMatrix();
        designMatrix.ScaleX = first.ScaleX * second.ScaleX;
        designMatrix.ScaleY = first.ScaleY * second.ScaleY;
        designMatrix.TranslateX = first.TranslateX + second.TranslateX;
        designMatrix.TranslateY = first.TranslateY + second.TranslateY;
        return designMatrix;
    }
}