// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Config;

public class FileName
{
    // 公版裁片图 本地保存的路径
    public static string getPatternPieceImgPath(int factoryId, long designProductId)
    {
        // Cache/[工厂]/Pattern-piece/公版id/ [裁片图]
        return AppContext.BaseDirectory + "Cache" +
               Path.DirectorySeparatorChar + "Factory-" + factoryId +
               Path.DirectorySeparatorChar + "Pattern-piece" +
               Path.DirectorySeparatorChar + "Paper-pattern-" + designProductId +
               Path.DirectorySeparatorChar;
    }

    // 公版裁片图对应的印花图 本地保存的路径
    public static string getPatternPrintImgPath(int factoryId, long galleryId)
    {
        // Cache/[工厂]/Pattern-piece/图库id/ [印花图]
        return AppContext.BaseDirectory + "Cache" +
               Path.DirectorySeparatorChar + "Factory-" + factoryId +
               Path.DirectorySeparatorChar + "Pattern-print" +
               //Path.DirectorySeparatorChar + "Print-img-" + galleryId +
               Path.DirectorySeparatorChar;
    }

    // 订单的公版裁片图 本地保存的路径
    public static string getOrderPatternPieceImgPath(string orderNo, int factoryId, long designProductId)
    {
        // Cache/[工厂]/Order/Pattern-piece/公版id/ [裁片图]
        return AppContext.BaseDirectory + "Cache" +
               Path.DirectorySeparatorChar + "Factory-" + factoryId +
               Path.DirectorySeparatorChar + "Order-" + orderNo +
               Path.DirectorySeparatorChar + "Pattern-piece" +
               Path.DirectorySeparatorChar + "Paper-pattern-" + designProductId +
               Path.DirectorySeparatorChar;
    }

    // 订单的公版裁片图对应的印花图 本地保存的路径 (子项才是唯一的, 一个商户单有多个子订单, 商户单单号不是唯一值, 子订单对应的子项号才是唯一的)
    public static string getOrderPatternPrintImgPath(string produceBatchNum, string orderNo, long batchNo, int factoryId, long galleryId)
    {
        // Cache/[工厂]/Order/Pattern-piece/图库id/ [印花图]
        return AppContext.BaseDirectory + "Cache" +
               Path.DirectorySeparatorChar + "Factory-" + factoryId +
               Path.DirectorySeparatorChar + "Order-batch-" + batchNo +
               Path.DirectorySeparatorChar + "Pattern-print" +
               //Path.DirectorySeparatorChar + "Print-img-" + galleryId +
               Path.DirectorySeparatorChar;
    }

    // 订单的公版裁片图对应的印花图  缩略图 本地保存的路径
    public static string getOrderPatternPrintImgThumbPath(string produceBatchNum, string orderNo, long batchNo, int factoryId, long galleryId)
    {
        // Cache/[工厂]/Order/Pattern-piece/图库id/ [印花图]
        return AppContext.BaseDirectory + "Cache" +
               Path.DirectorySeparatorChar + "Factory-" + factoryId +
               Path.DirectorySeparatorChar + "Order-batch-" + batchNo +
               Path.DirectorySeparatorChar + "Pattern-print-thumb" +
               //Path.DirectorySeparatorChar + "Print-img-" + galleryId +
               Path.DirectorySeparatorChar;
    }

    // 获取运单文件夹
    public static string getOrderExpressWaybillPath(int factoryId)
    {
        // Cache/[工厂]/Order/Waybill/图库id/ [印花图]
        return AppContext.BaseDirectory + "Cache" +
               Path.DirectorySeparatorChar + "Factory-" + factoryId +
               Path.DirectorySeparatorChar + "Waybill" +
               Path.DirectorySeparatorChar;
    }

    public static string getPhotoshopJsxScriptPath()
    {
        return AppContext.BaseDirectory + "Assets"+Path.DirectorySeparatorChar+"Script"+Path.DirectorySeparatorChar+"AnyChannel2spot.jsx";
    }

    public static string getLayoutTargetName(ObservableCollection<string> produceBatchNumberList)
    {
        return produceBatchNumberList.Count > 1 ? $"{string.Join("--",produceBatchNumberList)}_生产图排版.tif" : "生产图排版.tif";
    }
    
    public static readonly string ProduceImgLayoutName = "ProduceImgLayout";


    public static readonly string DefaultPrintedPatternFilePath =
        AppContext.BaseDirectory + "Cache" + Path.DirectorySeparatorChar + ProduceImgLayoutName + Path.DirectorySeparatorChar;
    
    public static readonly string DatabaseFilePath = AppContext.BaseDirectory + "Database" + Path.DirectorySeparatorChar;
    public static readonly string DatabaseFileName = "Local.db";
    
    public static readonly string LogFilePath = AppContext.BaseDirectory + "Logs" + Path.DirectorySeparatorChar;
    public static readonly string LogFileFullPath = LogFilePath + "log-.txt";
}