// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Dto.CreateImg;

namespace Wpf.Ui.Gallery.Dto.FormatAdapter;

using System.Globalization;
using System.Text.RegularExpressions;

public class ProductionTaskBuilder
{
    /// <summary>
    /// 从API返回的订单信息中，构建出所有需要执行的生产图合成任务列表。
    /// </summary>
    /// <param name="batchItem">从API反序列化得到的订单对象。</param>
    /// <returns>一个包含所有待处理任务的列表。</returns>
    public List<ProductionTask> BuildTasksFromItem(ProduceBatchItemDetail batchItem)
    {
        var tasks = new List<ProductionTask>();
        PrintCropType printCropType = PrintCropTypeBuilder.BuildPrintCropTypeFromOriginString(batchItem.PrintCropType);
        // 遍历所有视图 (后片、前片、袖子等)
        foreach (var viewIdKey in batchItem.ProducePrintInfo.Keys)
        {
            PrintInfo printInfo = batchItem.ProducePrintInfo[viewIdKey];
            if (printCropType == PrintCropType.裁片指定印花区域裁切)
            {
                // 只打印印花图 与裁片无关 (如 烫画)
                var task = new ProductionTask
                {
                    RenderType = RenderTypeBuilder.getRenderType(batchItem.IsMultiPiece),
                    PatternPieceTitle = viewIdKey,
                    FactoryId = batchItem.FactoryId,
                    TaskId = $"{batchItem.ProduceBatchNumber}-{viewIdKey}",
                    DesignProductId = batchItem.DesignProductId,
                    OrderAttributes = batchItem.Attributes,
                    ProductName = batchItem.DesignName,
                    ProduceBatchNum = batchItem.ProduceBatchNumber,
                    BatchNum = batchItem.BatchNum,
                    OrderNo = batchItem.OrderNo,
                    OrderDetailId = batchItem.OrderDetailId,
                    ViewId = printInfo.ViewId,
                    ViewName = batchItem.ViewNameMap?.ContainsKey(viewIdKey) ?? false ? batchItem.ViewNameMap[viewIdKey] : string.Empty,
                    PatternPieceImageUrl = null,
                    // CuttingPieceTargetWidthCm = decimal.Parse(cuttingPiece.WidthCm, CultureInfo.InvariantCulture),
                    // CuttingPieceTargetHeightCm = decimal.Parse(cuttingPiece.HeightCm, CultureInfo.InvariantCulture),
                    PatternPieceTargetWidthMm = printInfo.RealSizeWidthMm,
                    //裁片视图 目前是等宽高的, 并且接口没有返回 实际高度 ；所以只能把实际宽度当作实际高度
                    PatternPieceTargetHeightMm = printInfo.RealSizeWidthMm,
                    TargetDpi = printInfo.TargetDpi,
                    PrintLayers = new List<PrintLayerInfo>(),
                    PrintCropType = printCropType,
                    // 全印的情况: 印花裁剪区域应该在裁片上才对 但是目前数据是存放在印花图层上 强行校正数据存放位置在此 (下面的代码进行实际赋值)
                    // 局部印的情况: 只给了其中一面的印花区域 已修复后端代码, 给处所有面打印区域数据
                    PrintCropArea = new PrintCropArea()
                    {
                        WidthMm = printInfo.GetWidthMm(),
                        HeightMm = printInfo.GetHeightMm(),
                    }
                };
                // 检查此视图上是否有对应的印花图配置
                if (batchItem.ProductConfig.TryGetValue(viewIdKey, out var configItems))
                {
                    // 一个裁片上可能叠加了多个印花图
                    foreach (var configItem in configItems)
                    {
                        var designImage = configItem.Image;

                        // 解析复杂的CSS变换矩阵 然后把居中变换和用户手动变换的矩阵信息合并
                        var centerTransform = ParseTransformMatrix(designImage.CenterTransform);
                        var userTransform = ParseTransformMatrix(designImage.UserTransform);
                        var transform = MergeMatrix(centerTransform, userTransform);
                        TileTool tileTool = new TileTool();
                        tileTool.TileType = TileTypeBuilder.BuildTileTypeFromOriginString(designImage.TileType);
                        tileTool.TileSpacingXMm = designImage.TileSpacingXMm;
                        tileTool.TileSpacingYMm = designImage.TileSpacingYMm;
                        var layer = new PrintLayerInfo
                        {
                            GalleryId = designImage.GalleryId,
                            DesignImageUrl = designImage.DesignImageUrl,
                            DesignImageSizeMm = designImage.DimensionsMm,
                            //ScaleX = transform.ScaleX,
                            //ScaleY = transform.ScaleY,
                            //TranslateX = transform.TranslateX,
                            //TranslateY = transform.TranslateY,
                            TranslateX = designImage.OffsetX,
                            TranslateY = designImage.OffsetY,
                            Rotation = designImage.Rotate,
                            XFlip = designImage.XFlip,
                            YFlip = designImage.YFlip,
                            //目前 裁片多印花图的层级索引等于裁片印花图的序号
                            ZIndex = configItem.ViewId,
                            TileTool = tileTool,
                        };
                        task.PrintLayers.Add(layer);
                        // task.PrintCropArea = designImage.PrintCropArea;
                    }
                }else
                {
                    // 目前文字印花使用后端生成的文字印花图层(或者svg图层)进行生产图制作 ； 而不是 文字+位移+旋转+字体+文字大小+颜色 等数据进行渲染
                    foreach (string viewId in batchItem.WordImgMap.Keys)
                    {
                        string wordImgUrl = batchItem.WordImgMap[viewId];
                            
                        // 文字印花不支持特效
                        TileTool tileTool = new TileTool();
                        tileTool.TileType = TileType.无平铺;
                        var layer = new PrintLayerInfo
                        {
                            GalleryId = -1,
                            DesignImageUrl = wordImgUrl,
                            // 印花图需要缩放到跟裁片一样的目标尺寸
                            DesignImageSizeMm = new RealSize()
                            {
                                Width = printInfo.GetWidthMm(),
                                Height = printInfo.GetHeightMm(),
                            },
                            //ScaleX = transform.ScaleX,
                            //ScaleY = transform.ScaleY,
                            //TranslateX = transform.TranslateX,
                            //TranslateY = transform.TranslateY,
                            TranslateX = 0,
                            TranslateY = 0,
                            Rotation = 0,
                            XFlip = false,
                            YFlip = false,
                            //目前 裁片多印花图的层级索引等于裁片印花图的序号
                            ZIndex = Convert.ToInt32(viewId),
                            TileTool = tileTool,
                        };
                        task.PrintLayers.Add(layer);
                    }
                }

                tasks.Add(task);
            }
            else if ((printCropType == PrintCropType.裁片底图全印裁切) || (printCropType == PrintCropType.裁片满幅裁切))
            {
                // 一个视图可能对应多个裁片
                foreach (var piecePair in printInfo.PatternPieces)
                {
                    PatternPieceInfo patternPiece = piecePair.Value;

                    // 创建一个针对此裁片的新生产任务
                    var task = new ProductionTask
                    {
                        RenderType = RenderTypeBuilder.getRenderType(batchItem.IsMultiPiece),
                        PatternPieceTitle = patternPiece.Title,
                        FactoryId = batchItem.FactoryId,
                        TaskId = $"{batchItem.ProduceBatchNumber}-{patternPiece.Title}",
                        DesignProductId = batchItem.DesignProductId,
                        OrderAttributes = batchItem.Attributes,
                        ProductName = batchItem.DesignName,
                        ProduceBatchNum = batchItem.ProduceBatchNumber,
                        BatchNum = batchItem.BatchNum,
                        OrderNo = batchItem.OrderNo,
                        OrderDetailId = batchItem.OrderDetailId,
                        ViewId = printInfo.ViewId,
                        PatternPieceImageUrl = patternPiece.PatternPieceImageUrl,
                        // CuttingPieceTargetWidthCm = decimal.Parse(cuttingPiece.WidthCm, CultureInfo.InvariantCulture),
                        // CuttingPieceTargetHeightCm = decimal.Parse(cuttingPiece.HeightCm, CultureInfo.InvariantCulture),
                        PatternPieceTargetWidthMm = printInfo.RealSizeWidthMm,
                        //裁片视图 目前是等宽高的, 并且接口没有返回 实际高度 ；所以只能把实际宽度当作实际高度
                        PatternPieceTargetHeightMm = printInfo.RealSizeWidthMm,
                        TargetDpi = printInfo.TargetDpi,
                        PrintLayers = new List<PrintLayerInfo>(),
                        PrintCropType = printCropType,
                        // 印花裁剪区域应该再裁片上才对 但是目前数据是存放在印花图层上 强行校正数据存放位置在此 (下面的代码进行实际赋值)
                        PrintCropArea = null
                    };
                    // 由于无字段可以识别此项是否是纯印花图 或者是 印花图+文字印花  或者纯文字印花  所以使用此判断依据: 如果 product_config数量与logo_svg_list数量不相等 则存在文字印花 执行文字印花流程
                    if (batchItem.ProductConfig.Count == batchItem.WordImgMap.Count)
                    {
                        // 图片印花
                        // 检查此视图上是否有对应的印花图配置
                        if (batchItem.ProductConfig.TryGetValue(viewIdKey, out var configItems))
                        {
                            // 一个裁片上可能叠加了多个印花图
                            foreach (var configItem in configItems)
                            {
                                var designImage = configItem.Image;

                                // 解析复杂的CSS变换矩阵 然后把居中变换和用户手动变换的矩阵信息合并
                                var centerTransform = ParseTransformMatrix(designImage.CenterTransform);
                                var userTransform = ParseTransformMatrix(designImage.UserTransform);
                                var transform = MergeMatrix(centerTransform, userTransform);
                                TileTool tileTool = new TileTool();
                                tileTool.TileType = TileTypeBuilder.BuildTileTypeFromOriginString(designImage.TileType);
                                tileTool.TileSpacingXMm = designImage.TileSpacingXMm;
                                tileTool.TileSpacingYMm = designImage.TileSpacingYMm;
                                var layer = new PrintLayerInfo
                                {
                                    GalleryId = designImage.GalleryId,
                                    DesignImageUrl = designImage.DesignImageUrl,
                                    DesignImageSizeMm = designImage.DimensionsMm,
                                    //ScaleX = transform.ScaleX,
                                    //ScaleY = transform.ScaleY,
                                    //TranslateX = transform.TranslateX,
                                    //TranslateY = transform.TranslateY,
                                    TranslateX = designImage.OffsetX,
                                    TranslateY = designImage.OffsetY,
                                    Rotation = designImage.Rotate,
                                    XFlip = designImage.XFlip,
                                    YFlip = designImage.YFlip,
                                    //目前 裁片多印花图的层级索引等于裁片印花图的序号
                                    ZIndex = configItem.ViewId,
                                    TileTool = tileTool,
                                };
                                task.PrintLayers.Add(layer);
                                task.PrintCropArea = designImage.PrintCropArea;
                            }
                        }
                    }
                    else
                    {
                        // 目前文字印花使用后端生成的文字印花图层(或者svg图层)进行生产图制作 ； 而不是 文字+位移+旋转+字体+文字大小+颜色 等数据进行渲染
                        if (batchItem.WordImgMap.ContainsKey(viewIdKey))
                        {
                            string wordImgUrl = batchItem.WordImgMap[viewIdKey];
                            // 文字印花不支持特效
                            TileTool tileTool = new TileTool();
                            tileTool.TileType = TileType.无平铺;
                            var layer = new PrintLayerInfo
                            {
                                GalleryId = -1,
                                DesignImageUrl = wordImgUrl,
                                DesignImageSizeMm = new RealSize()
                                {
                                    Width = printInfo.GetWidthMm(),
                                    Height = printInfo.GetHeightMm(),
                                },
                                //ScaleX = transform.ScaleX,
                                //ScaleY = transform.ScaleY,
                                //TranslateX = transform.TranslateX,
                                //TranslateY = transform.TranslateY,
                                TranslateX = 0,
                                TranslateY = 0,
                                Rotation = 0,
                                XFlip = false,
                                YFlip = false,
                                //目前 裁片多印花图的层级索引等于裁片印花图的序号
                                ZIndex = Convert.ToInt32(viewIdKey),
                                TileTool = tileTool,
                            };
                            task.PrintLayers.Add(layer);
                        }
                    }

                    tasks.Add(task);
                }
            }
            else
            {
                // TODO 需要抛错
                Console.WriteLine("未知印花裁剪方式,无法创建生产任务:" + printCropType);
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