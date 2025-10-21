// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Utils;

public class NetworkHelper
{
    // URL兼容解析写法: 浏览器端支持  https://  http://  // 三种开头 [其中 // 的意思是跟随浏览器当前页面是https://还是 http:// 这种在服务端是不支持的 需要强行转换成其他协议 当前默认转换成 http://]
    // 历史遗留, 本就不应该保存 //开头的URL到数据库
    public static Uri ParseUrl(string imageUrl, string defaultScheme = "http")
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentNullException(nameof(imageUrl));
        }

        // 如果URL以"//"开头，为其添加默认协议
        if (imageUrl.StartsWith("//"))
        {
            imageUrl = $"{defaultScheme}:{imageUrl}";
        }

        // 尝试创建Uri对象，构造函数会验证并解析URL
        // UriKind.Absolute 确保我们得到的是一个完整的、可以用于网络请求的URL
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? absoluteUri))
        {
            // 确保协议是 http 或 https，因为 HttpClient 默认只支持这两种
            if (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps)
            {
                return absoluteUri;
            }
            else
            {
                throw new UriFormatException($"URL 协议 '{absoluteUri.Scheme}' 不受支持。仅支持 http 和 https。");
            }
        }
        else
        {
            throw new UriFormatException($"无法将 '{imageUrl}' 解析为有效的绝对URL。");
        }
    }
    
    public static string? GetFileExtensionFromUrl(string url)
    {
        // 步骤 1: 基础验证，处理 null 或空白输入
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // 步骤 2: 使用 .NET 内置的 Uri 类进行专业解析
        // Uri.TryCreate 是最可靠的方式，它可以正确识别协议、主机、路径、查询和片段等部分
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            // 如果字符串不是一个有效的绝对 URI，则无法安全解析
            return null;
        }

        // 步骤 3: 获取 URI 的路径部分
        // 对于 URL: https://.../68ce0af6597af.png?x-oss-process=...
        // uri.AbsolutePath 会返回: "/productPhoto/20250920/68ce0af6597af.png"
        // 它已经智能地将查询参数（?x-oss-process=...）剥离了
        string path = uri.AbsolutePath;

        // 步骤 4: 使用 System.IO.Path.GetExtension 方法
        // 这是从文件路径字符串中提取扩展名的标准、高效且安全的方法
        string extension = Path.GetExtension(path);

        // 步骤 5: 返回结果
        // 如果 Path.GetExtension 找不到扩展名，它会返回一个空字符串 ""
        // 我们将其转换成 null，使得方法签名更清晰地表达“未找到”的语义
        return string.IsNullOrEmpty(extension) ? null : extension;
    }
}