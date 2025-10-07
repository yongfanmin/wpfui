// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SkiaSharp;
using Svg;
using Svg.Skia;
using Svg.Skia.TypefaceProviders;
using Svg.Transforms;


namespace Wpf.Ui.Gallery.Utils;

public static class SvgPreloader
{
    
    public static bool IsSvg(Stream stream)
    {
        if (stream == null || !stream.CanRead)
        {
            return false;
        }

        long originalPosition = -1;
        if (stream.CanSeek)
        {
            originalPosition = stream.Position;
            stream.Position = 0;
        }

        try
        {
            var settings = new XmlReaderSettings
            {
                // 关键修改 1: 告诉读取器忽略 DTD，而不是禁止它。
                // Prohibit 会在遇到 <!DOCTYPE> 时抛出异常。
                // Ignore 会跳过 DTD 定义并继续解析。
                DtdProcessing = DtdProcessing.Ignore,

                // 关键修改 2: 禁用对外部实体的解析，这是一个重要的安全措施。
                // 这样可以防止读取器尝试访问网络上的 dtd 文件。
                XmlResolver = null,

                // 保持原有的安全设置
                CheckCharacters = true
            };

            using (var reader = XmlReader.Create(stream, settings))
            {
                // 循环读取，跳过所有非元素节点（如声明、注释、DTD等）
                while (reader.Read())
                {
                    // 仅当找到第一个实际的元素时才进行判断
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        // 检查根元素的名称是否为 "svg"
                        return reader.Name.Equals("svg", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }
        catch (XmlException)
        {
            // 如果文件内容根本不是 XML，或者有其他 XML 错误，则不是 SVG
            return false;
        }
        finally
        {
            // 确保流的位置被恢复，以便后续代码可以从头读取
            if (stream.CanSeek && originalPosition != -1)
            {
                stream.Position = originalPosition;
            }
        }

        // 如果文件中一个元素都找不到，那肯定不是 SVG
        return false;
    }
}