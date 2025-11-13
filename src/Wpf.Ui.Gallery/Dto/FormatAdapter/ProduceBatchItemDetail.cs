// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Converters;
using Wpf.Ui.Gallery.Dto.FormatAdapter.Converts;
using Wpf.Ui.Gallery.Utils;


namespace Wpf.Ui.Gallery.Dto.FormatAdapter;

// 订单打印批次的核心信息
public class ProduceBatchItemDetail
{
    [JsonPropertyName("factory_id")]
    [JsonConverter(typeof(StringOrNumberToIntConverter))]
    public int FactoryId { get; set; }

    // 公版id
    [JsonPropertyName("design_product_id")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long DesignProductId { get; set; }

    [JsonPropertyName("order_no")] public string OrderNo { get; set; }
    
    [JsonPropertyName("order_code")] public string OrderCode { get; set; }
    
    [JsonPropertyName("order_detail_id")]
    [JsonConverter(typeof(StringOrNumberToIntConverter))]
    public int OrderDetailId { get; set; }

    [JsonPropertyName("item_id")] public string ItemId { get; set; }

    [JsonPropertyName("produce_batch_number")]
    public string ProduceBatchNumber { get; set; }

    [JsonPropertyName("batch_no")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long BatchNum { get; set; }

    [JsonPropertyName("size")] public string Size { get; set; }

    [JsonPropertyName("design_name")] public string DesignName { get; set; }
    
    [JsonPropertyName("product_id")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long ProductId { get; set; }
    
    [JsonPropertyName("buy_index")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long BuyIndex { get; set; }
    
    // 购买件数
    [JsonPropertyName("buy_number")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long BuyNumber { get; set; }
    
    [JsonPropertyName("view_id")]
    [JsonConverter(typeof(StringOrNumberToIntConverter))]
    public int ViewId { get; set; }
    
    [JsonPropertyName("goods_sku_id")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long SkuId { get; set; }
    
    [JsonPropertyName("sku")] public string Sku { get; set; }
    [JsonPropertyName("attrs")] public OrderAttributes Attributes { get; set; }

    // 设计器配置，Key是ViewId (string)
    [JsonPropertyName("product_config")] public Dictionary<string, List<ProductConfigItem>> ProductConfig { get; set; }

    // 文字转印花图列表
    [JsonPropertyName("logo_image_list")] public Dictionary<string, string> WordImgMap { get; set; }
    // SVG兼容性不太好 没有合适的库实现 有点问题 先不用 (扭曲旋转的文字无法渲染)
    // [JsonPropertyName("logo_svg_list")] public Dictionary<string, string> WordImgMap { get; set; }

    // 这是一个混合格式 有的返回数组 有的返回json
    // 生产打印参数，Key是ViewId (string)
    [JsonPropertyName("produce_print_info")]
    [JsonConverter(typeof(PrintInfoConverter))]
    public Dictionary<string, PrintInfo?>? ProducePrintInfo { get; set; }
    // 以上后端是一个多格式得数据 统一转换成 数组嵌json
    //public List<PrintInfo> ProducePrintInfo { get; set; }


    [JsonPropertyName("design_piece_rule")]
    [JsonConverter(typeof(StringOrNumberToIntConverter))]
    public int PrintCropType { get; set; }

    public ProduceBatchStatus ProduceBatchStatus { get; set; } = ProduceBatchStatus.等待生产数据;

    [JsonPropertyName("face_alias")] public Dictionary<string, string> ViewNameMap { get; set; }

    [JsonPropertyName("is_3d")]
    [JsonConverter(typeof(Is3d2MultiPieceConvert))]
    public bool IsMultiPiece { get; set; }

    public static ProduceBatchItemDetail ConstructByJson(JsonNode jsonNode)
    {
        // if (itemsNode is JsonArray itemsArray)
        // if (itemsNode is JsonObject itemsObject)
        // if (itemsNode is JsonValue itemsValue)
        // if (itemsNode == null)
        try
        {
            ProduceBatchItemDetail produceBatchItemDetail =
                JsonSerializer.Deserialize<ProduceBatchItemDetail>(jsonNode.ToString());
            if (produceBatchItemDetail == null)
            {
                return null;
            }
            else
            {
                string sizeId = produceBatchItemDetail.Attributes.SizeId.ToString();
                foreach (string key in produceBatchItemDetail.ProductConfig.Keys)
                {
                    if (!produceBatchItemDetail.ProducePrintInfo.ContainsKey(key))
                    {
                        PrintInfo samePrintInfo = produceBatchItemDetail.ProducePrintInfo["0"];
                        PrintInfo printInfo = new PrintInfo();
                        printInfo.TargetDpi = samePrintInfo.TargetDpi;
                        printInfo.RealSizeWidthMm = samePrintInfo.RealSizeWidthMm;
                        printInfo.PatternPieces = samePrintInfo.PatternPieces;
                        printInfo.ViewId = produceBatchItemDetail.ProductConfig[key][0].ViewId;
                        produceBatchItemDetail.ProducePrintInfo.Add(key, printInfo);
                    }
                }

                produceBatchItemDetail.ProducePrintInfo.Remove("0");
                // 根据比例计算出真实 宽高/位移

                foreach (string key in produceBatchItemDetail.ProducePrintInfo.Keys)
                {
                    try
                    {
                        PrintInfo printInfo = produceBatchItemDetail.ProducePrintInfo[key];
                        printInfo.RealSizeWidthMm = printInfo.RealSizeWidthMm * printInfo.SizePrintRatio[sizeId] / 100;
                        printInfo.HeightPx = printInfo.HeightPx * printInfo.SizePrintRatio[sizeId] / 100;
                        printInfo.WidthPx = printInfo.WidthPx * printInfo.SizePrintRatio[sizeId] / 100;

                        if (produceBatchItemDetail.ProductConfig.ContainsKey(key))
                        {
                            List<ProductConfigItem> productConfigItemList = produceBatchItemDetail.ProductConfig[key];
                            foreach (ProductConfigItem productConfigItem in productConfigItemList)
                            {
                                productConfigItem.Image.DimensionsMm.Width = productConfigItem.Image.DimensionsMm.Width *
                                    printInfo.SizePrintRatio[sizeId] / 100;
                                productConfigItem.Image.DimensionsMm.Height = productConfigItem.Image.DimensionsMm.Height *
                                    printInfo.SizePrintRatio[sizeId] / 100;
                                productConfigItem.Image.OffsetX = productConfigItem.Image.OffsetX *
                                    printInfo.SizePrintRatio[sizeId] / 100;
                                productConfigItem.Image.OffsetY = productConfigItem.Image.OffsetY *
                                    printInfo.SizePrintRatio[sizeId] / 100;
                                //TODO 平铺效果也需要放大平铺间距???
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    
                }

                return produceBatchItemDetail;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("格式化ProduceBatchItemDetail出错", e);
            ProduceBatchItemDetail produceBatchItemDetail = new ProduceBatchItemDetail();
            //orderPrintBatch.OrderNo = jsonNode.path("order_no").ToString();
            //orderPrintBatch.BatchNum = jsonNode["batch_no"].ToString();
            produceBatchItemDetail.OrderNo = (string?)jsonNode["order_no"] ?? string.Empty;
            produceBatchItemDetail.OrderCode = (string?)jsonNode["order_code"] ?? string.Empty;
            // --- 健壮地获取 batch_no (可能是数字或字符串，目标是long) ---
            JsonNode? batchNoNode = jsonNode["batch_no"];
            if (batchNoNode != null)
            {
                // 尝试将其作为 long 读取
                if (batchNoNode.GetValue<JsonElement>().TryGetInt64(out long batchNum))
                {
                    produceBatchItemDetail.BatchNum = batchNum;
                }
                // 如果失败，再尝试将其作为 string 读取并转换
                else if (long.TryParse((string?)batchNoNode, out batchNum))
                {
                    produceBatchItemDetail.BatchNum = batchNum;
                }
                else
                {
                    // 所有尝试都失败了，赋予一个安全的默认值
                    produceBatchItemDetail.BatchNum = 0L;
                }
            }

            return produceBatchItemDetail;
        }
    }

    public static List<ProduceBatchItemDetail> ConstructByArrayJson(List<JsonNode?> jsonObjectListString)
    {
        List<ProduceBatchItemDetail> orderPrintBatchList = new List<ProduceBatchItemDetail>();
        foreach (JsonNode obj in jsonObjectListString)
        {
            orderPrintBatchList.Add(ConstructByJson(obj));
        }

        return orderPrintBatchList;
    }
}

public class OrderAttributes
{
    [JsonPropertyName("colour_id")]
    [JsonConverter(typeof(StringOrNumberToIntConverter))]
    public int ColorId { get; set; }
    
    [JsonPropertyName("colour_alias")] public string ColorAlias { get; set; }

    [JsonPropertyName("model_alias")] public string SizeAlias { get; set; }

    [JsonPropertyName("model_id")]
    [JsonConverter(typeof(StringOrNumberToIntConverter))]
    public int SizeId { get; set; }
}

// product_config 中每个视图的配置项 (代表一个印花图层)
public class ProductConfigItem
{
    [JsonPropertyName("image")] public DesignImageInfo Image { get; set; }

    [JsonPropertyName("view_id")]
    [JsonConverter(typeof(StringOrNumberToIntConverter))]
    public int ViewId { get; set; }
}

// 印花图的详细变换和源信息
public class DesignImageInfo
{
    [JsonPropertyName("gallery_id")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long GalleryId { get; set; } // 印花图对应图库id

    [JsonPropertyName("url_origin")] public string DesignImageUrl { get; set; } // 印花图URL


    // matrix(1.7412, 0, 0, 1.7412, -112.8034, -112.8034)
    /*scaleX = 1.7412: 印花图在水平方向被放大了 1.7412 倍 (即 174.12%)。
    skewY = 0: 垂直方向没有倾斜。
    skewX = 0: 水平方向没有倾斜。
    scaleY = 1.7412: 印花图在垂直方向也被放大了 1.7412 倍。
    重要结论：因为 scaleX 和 scaleY 的值相等，所以这是一个等比缩放 (Uniform Scaling)。印花图没有被压扁或拉伸。
    translateX = -112.8034: 印花图的原点（通常是其左上角）在水平方向向左移动了 112.8034 个单位。
    这个“单位”通常是在设计器画布的坐标系下的像素。
    translateY = -112.8034: 印花图的原点在垂直方向向上移动了 112.8034 个单位。*/

    // 此变换实际由前端保存的两个变换参数进行矩阵运算合成的
    // 前端参数 transform(当图片放大的时候 需要变化一次 以保证图片一直居中) gTransform(用户实际拖动图片 位移变换的参数) 两个矩阵进行合并运算 就是最终矩阵
    // matrix(a, b, c, d, e, f)  运算方式 a b c d 进行相乘 e f 进行相加
    // TODO 没用到此参数
    [JsonPropertyName("transform")] public string CenterTransform { get; set; } // CSS变换矩阵

    // TODO 没用到此参数
    [JsonPropertyName("gTransform")] public string UserTransform { get; set; } // CSS变换矩阵

    [JsonPropertyName("offset_x")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal OffsetX { get; set; } // CSS变换矩阵


    [JsonPropertyName("offset_y")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal OffsetY { get; set; } // CSS变换矩阵

    [JsonPropertyName("rotate")] public decimal Rotate { get; set; } // 旋转角度

    [JsonPropertyName("realSize")]
    [JsonConverter(typeof(RealSizeJsonConverter))] // 使用自定义转换器
    public RealSize DimensionsMm { get; set; } // 印花图在画布上的物理尺寸(毫米)

    [JsonPropertyName("tileType")] public string TileType { get; set; } // 平铺类型

    [JsonConverter(typeof(StringToDecimalConverter))]
    [JsonPropertyName("hspacing")]
    public decimal TileSpacingXMm { get; set; } // 水平间隙

    [JsonConverter(typeof(StringToDecimalConverter))]
    [JsonPropertyName("vspacing")]
    public decimal TileSpacingYMm { get; set; } // 垂直间隙

    [JsonPropertyName("xFlip")] public bool XFlip { get; set; } // 水平翻转

    [JsonPropertyName("yFlip")] public bool YFlip { get; set; } // 垂直翻转

    [JsonPropertyName("size")] public PrintCropArea PrintCropArea { get; set; }
}

// 裁片和打印的工程参数
public class PrintInfo
{
    [JsonPropertyName("dpi")]
    [JsonConverter(typeof(StringOrNumberToIntConverter))]
    public int TargetDpi { get; set; }

    [JsonPropertyName("view_id")]
    [JsonConverter(typeof(StringOrNumberToIntConverter))]
    public int ViewId { get; set; }

    [JsonPropertyName("actual_width")]
    [JsonConverter(typeof(StringToDecimalConverter))] // <-- 应用转换器 小数字符串转Dedimal类型
    public decimal RealSizeWidthMm { get; set; } // 裁片物理宽度 (毫米)

    // 重写get太过复杂 调用的地方全部都需要改一遍 直接再set的时候按照尺码重新写入值
    /*public decimal GetRealSizeWidthBySizeRatio(string sizeId)
    {
        return RealSizeWidthMm * SizePrintRatio[sizeId] / 100;
    }*/

    [JsonPropertyName("width")] public decimal WidthPx { get; set; }

    public decimal GetWidthMm()
    {
        return RealSizeWidthMm;
    }

    [JsonPropertyName("height")] public decimal HeightPx { get; set; }

    public decimal GetHeightMm()
    {
        return HeightPx > 0 ? (HeightPx / WidthPx) * RealSizeWidthMm : 0;
    }


    // 裁片视图 目前是等宽高的, 并且接口没有返回 实际高度 ；所以只能把实际宽度当作实际高度
    // [JsonPropertyName("actual_width")]
    // [JsonConverter(typeof(StringToDecimalConverter))] // <-- 应用转换器 小数字符串转Dedimal类型
    // public decimal realSizeHeightMm { get; set; } // 裁片物理高度 (毫米)

    //[JsonPropertyName("height")]
    //[JsonConverter(typeof(StringToDecimalConverter))] // <-- 应用转换器 小数字符串转Dedimal类型
    //public decimal realSizeHeightMm { get; set; } // 裁片物理高度 (毫米)

    // 该视图下的所有裁片，Key是裁片名 (string)
    [JsonPropertyName("qp_data")] public Dictionary<string, PatternPieceInfo> PatternPieces { get; set; }

    [JsonPropertyName("size_print_ratio")] public Dictionary<string, decimal> SizePrintRatio { get; set; }
}

// 单个裁片的信息
public class PatternPieceInfo
{
    [JsonPropertyName("title")] public string Title { get; set; } // 裁片名

    // 返回值不存在裁片序号
    // [JsonPropertyName("view_id")]
    // public int ViewId { get; set; } // 裁片序号


    [JsonPropertyName("qp_img")] public string PatternPieceImageUrl { get; set; } // 裁片模板图URL

    [JsonPropertyName("width")]
    [JsonConverter(typeof(StringToDecimalConverter))] // <-- 应用转换器 小数字符串转Dedimal类型
    public decimal Width { get; set; }

    [JsonPropertyName("height")]
    [JsonConverter(typeof(StringToDecimalConverter))] // <-- 应用转换器 小数字符串转Dedimal类型
    public decimal Height { get; set; }
}

// 用于解析内嵌JSON字符串 "realSize" 的辅助类和转换器
public class RealSize
{
    [JsonPropertyName("width")]
    [JsonConverter(typeof(StringToDecimalConverter))] // <-- 应用转换器 小数字符串转Dedimal类型
    public decimal Width { get; set; }

    // 重写get太过复杂 调用的地方全部都需要改一遍 直接再set的时候按照尺码重新写入值
    /*public decimal GetWidth(Dictionary<string, decimal> SizePrintRatio , string sizeId)
    {
        return Width * SizePrintRatio[sizeId] / 100;
    }*/

    [JsonPropertyName("height")]
    [JsonConverter(typeof(StringToDecimalConverter))] // <-- 应用转换器 小数字符串转Dedimal类型
    public decimal Height { get; set; }

    // 重写get太过复杂 调用的地方全部都需要改一遍 直接再set的时候按照尺码重新写入值
    /*public decimal GetHeight(Dictionary<string, decimal> SizePrintRatio , string sizeId)
    {
        return Height * SizePrintRatio[sizeId] / 100;
    }*/
}

// 印花裁剪区域 2025.9.9 目前只对居中裁剪的产品生效
public class PrintCropArea
{
    // TODO 不知道为什么 这个接口数据的宽高映射相反 将错就错 先能用; 后续再处理
    [JsonPropertyName("height")]
    [JsonConverter(typeof(StringToDecimalConverter))] // <-- 应用转换器 小数字符串转Dedimal类型
    public decimal WidthMm { get; set; }

    [JsonPropertyName("width")]
    [JsonConverter(typeof(StringToDecimalConverter))] // <-- 应用转换器 小数字符串转Dedimal类型
    public decimal HeightMm { get; set; }
}

public class RealSizeJsonConverter : JsonConverter<RealSize>
{
    public override RealSize Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string value = reader.GetString();
            return JsonSerializer.Deserialize<RealSize>(value);
        }

        return JsonSerializer.Deserialize<RealSize>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, RealSize value, JsonSerializerOptions options)
    {
        var json = JsonSerializer.Serialize(value);
        writer.WriteStringValue(json);
    }
}