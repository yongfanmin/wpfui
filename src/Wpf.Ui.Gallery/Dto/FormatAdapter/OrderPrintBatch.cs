// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.
using System.Text.Json;
using System.Text.Json.Serialization;
using Wpf.Ui.Gallery.Converters;
using Wpf.Ui.Gallery.Utils;


namespace Wpf.Ui.Gallery.Dto.FormatAdapter;

// 订单打印批次的核心信息
public class OrderPrintBatch
{
    [JsonPropertyName("factory_id")]
    public int FactoryId { get; set; }
    
    // 公版id
    [JsonPropertyName("design_product_id")]
    public long DesignProductId { get; set; }
    
    [JsonPropertyName("order_no")]
    public string OrderNo { get; set; }
    
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; }

    [JsonPropertyName("produce_batch_number")]
    public string ProduceBatchNumber { get; set; }

    [JsonPropertyName("size")]
    public string Size { get; set; }
    
    [JsonPropertyName("design_name")]
    public string DesignName { get; set; }

    [JsonPropertyName("attrs")]
    public OrderAttributes Attributes { get; set; }

    // 设计器配置，Key是ViewId (string)
    [JsonPropertyName("product_config")]
    public Dictionary<string, List<ProductConfigItem>> ProductConfig { get; set; }

    // 生产打印参数，Key是ViewId (string)
    [JsonPropertyName("produce_print_info")]
    public Dictionary<string, PrintInfo> ProducePrintInfo { get; set; }
}

public class OrderAttributes
{
    [JsonPropertyName("colour_alias")]
    public string ColorAlias { get; set; }

    [JsonPropertyName("model_alias")]
    public string SizeAlias { get; set; }
    
    [JsonPropertyName("model_id")]
    public int SizeId { get; set; }
}

// product_config 中每个视图的配置项 (代表一个印花图层)
public class ProductConfigItem
{
    [JsonPropertyName("image")]
    public DesignImageInfo Image { get; set; }

    [JsonPropertyName("view_id")]
    public int ViewId { get; set; }
}

// 印花图的详细变换和源信息
public class DesignImageInfo
{
    [JsonPropertyName("gallery_id")]
    public long GalleryId { get; set; } // 印花图对应图库id
    
    [JsonPropertyName("designImg")]
    public string DesignImageUrl { get; set; } // 印花图URL

    
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
    [JsonPropertyName("transform")]
    public string CenterTransform { get; set; } // CSS变换矩阵
    
    [JsonPropertyName("gTransform")]
    public string UserTransform { get; set; } // CSS变换矩阵

    [JsonPropertyName("rotate")]
    public decimal Rotate { get; set; } // 旋转角度

    [JsonPropertyName("realSize")]
    [JsonConverter(typeof(RealSizeJsonConverter))] // 使用自定义转换器
    public RealSize DimensionsMm { get; set; } // 印花图在画布上的物理尺寸(毫米)
}

// 裁片和打印的工程参数
public class PrintInfo
{
    [JsonPropertyName("dpi")]
    public int TargetDpi { get; set; }

    [JsonPropertyName("view_id")]
    public int ViewId { get; set; }

    [JsonPropertyName("actual_width")]
    [JsonConverter(typeof(StringToDecimalConverter))] // <-- 应用转换器 小数字符串转Dedimal类型
    public decimal realSizeWidthMm { get; set; } // 裁片物理宽度 (毫米)
    
    // 该视图下的所有裁片，Key是裁片名 (string)
    [JsonPropertyName("qp_data")]
    public Dictionary<string, PatternPieceInfo> PatternPieces { get; set; }
}

// 单个裁片的信息
public class PatternPieceInfo
{
    [JsonPropertyName("title")]
    public string Title { get; set; } // 裁片名
    
    // 返回值不存在裁片序号
    // [JsonPropertyName("view_id")]
    // public int ViewId { get; set; } // 裁片序号


    [JsonPropertyName("qp_img")]
    public string PatternPieceImageUrl { get; set; } // 裁片模板图URL

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

    [JsonPropertyName("height")]
    [JsonConverter(typeof(StringToDecimalConverter))] // <-- 应用转换器 小数字符串转Dedimal类型
    public decimal Height { get; set; }
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