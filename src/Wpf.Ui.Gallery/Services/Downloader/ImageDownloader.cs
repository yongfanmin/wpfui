// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Net.Http;
using FileTypeChecker;
using FileTypeChecker.Abstracts;
using FileTypeChecker.Types;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Utils;

namespace Wpf.Ui.Gallery.Services.Downloader;

public class ImageDownloader : IImageDownloader
{
    private readonly HttpClient _httpClient;

    public ImageDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LocalImgInfo?> DownloadImageAsync(
        string imageUrl,
        string directoryPath,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
        {
            return null;
        }

        try
            {
                LocalImgInfo localImgInfo = new LocalImgInfo();
                // --- 步骤 1: 将整个网络流下载到内存流中 ---
                await using var memoryStream = new MemoryStream();
                
                await using (var networkStream = await _httpClient.GetStreamAsync(imageUrl, cancellationToken))
                {
                    // 将网络流的所有内容异步复制到内存流
                    await networkStream.CopyToAsync(memoryStream, cancellationToken);
                }

                // 如果流为空（下载了0字节的文件），则返回失败
                if (memoryStream.Length == 0)
                {
                    return null;
                }
                
                // 将内存流的指针重置到开头，以便进行读取操作
                memoryStream.Position = 0;

                // --- 步骤 2: 使用 FileTypeChecker 从内存流中检测格式 ---
                IFileType fileType = FileTypeValidator.GetFileType(memoryStream);

                if (fileType == null || !IsSupportedImageType(fileType))
                {
                    // 可以加入日志: "格式不支持或无法识别"
                    return null;
                }

                
                string extension = "." + fileType.Extension;

                // --- 步骤 3: 准备本地文件路径并写入 ---
                string localFileUrl = Path.Combine(directoryPath, fileName + extension);
                localImgInfo.LocalUrl = localFileUrl;
                localImgInfo.Extenion = fileType.Extension;
                localImgInfo.FileName = fileName;
                // 文件存在 则直接返回本地地址 不再下载
                if (IsExistFile(localFileUrl))
                {
                    return localImgInfo;
                }

                // 确保目标目录存在
                Directory.CreateDirectory(directoryPath);

                // --- [您需要的完整代码在这里] ---
                // 再次将内存流的指针重置到开头，因为FileTypeValidator也读取过它
                memoryStream.Position = 0;

                // 使用一个 using 语句来创建文件流，确保它在使用后被正确关闭
                // FileStream 构造函数参数:
                // - path: 完整的文件路径
                // - mode: FileMode.Create 表示如果文件已存在则覆盖，不存在则创建
                // - access: FileAccess.Write 表示我们只需要写入权限
                // - share: FileShare.None 表示在写入期间不允许其他进程访问该文件，保证独占性
                // - bufferSize: 缓冲区大小，8192 (8KB) 是一个很好的通用值
                // - useAsync: true 启用异步I/O，提升性能
                await using (var fileStream = new FileStream(localFileUrl, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true))
                {
                    // 将内存流的所有内容异步复制到文件流
                    await memoryStream.CopyToAsync(fileStream, cancellationToken);
                }
                // --- [代码结束] ---

                return localImgInfo;
            }
            catch (Exception ex)
            {
                // 统一处理所有可能的异常
                Console.WriteLine($"下载图片 {imageUrl} 时发生错误: {ex.Message}");
                return null;
            }
    }
    
    private bool IsSupportedImageType(IFileType fileType)
    {
        // FileTypeChecker 提供了具体的类型供我们判断
        return fileType is JointPhotographicExpertsGroup ||
               fileType is PortableNetworkGraphic ||
               fileType is GraphicsInterchangeFormat87 ||
               fileType is GraphicsInterchangeFormat89 ||
               fileType is Bitmap ||
               fileType is TaggedImageFileFormat ||
               fileType is Webp;
    }

    // 判断文件是否已经存在本地磁盘 TODO 是否重复的判断过于简单 无法应对 图片名称没修改 但是 图片内容已经修改的情况 (存储文件hash用于判断? 或者判断文件创建日期是否一致?)
    private bool IsExistFile(string pathFileName)
    {
        // 1. 处理 null 或空白字符串输入，直接返回 false
        if (string.IsNullOrWhiteSpace(pathFileName))
        {
            return false;
        }

        try
        {
            // 2. 使用 File.Exists()，这是最直接、最高效的方式
            // 它在内部已经处理了很多路径有效性的问题
            return File.Exists(pathFileName);
        }
        catch (Exception)
        {
            // 3. 尽管 File.Exists() 很少抛出异常，但为了绝对的健壮性，
            // 捕获所有可能的意外情况 (例如，极长的路径、权限问题等)
            // 在任何异常情况下，我们都安全地认为“文件不存在”。
            return false;
        }
    }
    
    
}
