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
}