// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Config;

public class FileName
{
    //用户信息缓存文件名称
    public static readonly string UserCacheFileName = "UserInfo.cache";
    
    //用户信息缓存文件名称
    public static readonly string LocalConfigFileName = "Local.Config";

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

    // 订单的公版裁片图对应的印花图 本地保存的路径
    public static string getOrderPatternPrintImgPath(string orderNo, int factoryId, long galleryId)
    {
        // Cache/[工厂]/Order/Pattern-piece/图库id/ [印花图]
        return AppContext.BaseDirectory + "Cache" +
               Path.DirectorySeparatorChar + "Factory-" + factoryId +
               Path.DirectorySeparatorChar + "Order-" + orderNo +
               Path.DirectorySeparatorChar + "Pattern-print" +
               //Path.DirectorySeparatorChar + "Print-img-" + galleryId +
               Path.DirectorySeparatorChar;
    }
    
    // 订单的公版裁片图对应的印花图  缩略图 本地保存的路径
    public static string getOrderPatternPrintImgThumbPath(string orderNo, int factoryId, long galleryId)
    {
        // Cache/[工厂]/Order/Pattern-piece/图库id/ [印花图]
        return AppContext.BaseDirectory + "Cache" +
               Path.DirectorySeparatorChar + "Factory-" + factoryId +
               Path.DirectorySeparatorChar + "Order-" + orderNo +
               Path.DirectorySeparatorChar + "Pattern-print-thumb" +
               //Path.DirectorySeparatorChar + "Print-img-" + galleryId +
               Path.DirectorySeparatorChar;
    }
    
    
}