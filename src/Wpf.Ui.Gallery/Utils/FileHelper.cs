// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Net.Http;

namespace Wpf.Ui.Gallery.Utils;

public static class FileHelper
{
    // 使用一个静态的信号量来处理可能的跨线程文件访问冲突，特别是在高并发场景下。
    // 对于同一个文件路径，我们希望写入操作是原子的。
    // 注意：这是一个简单的全局锁，对于极高并发写入不同文件，可以考虑更精细的锁策略。
    private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Asynchronously writes the specified text to a file, creating the directory if it does not exist.
    /// This method is robust, thread-safe, and ensures the path integrity before writing.
    /// </summary>
    /// <param name="path">The full path of the file to write to.</param>
    /// <param name="content">The text content to write to the file.</param>
    /// <param name="encoding">The character encoding to use. Defaults to UTF-8.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>

    // 朝某个完整路径的文件写入数据 如果路径上包含不存在的目录 则创建目录
    public static async Task WriteTextRobustAsync(
        string path,
        string content,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(nameof(path), "File path cannot be null or empty.");
        }

        // 1. 获取目标文件所在的目录路径
        // Path.GetDirectoryName is smart enough to handle file paths at the root.
        string? directoryPath = Path.GetDirectoryName(path);

        // 如果 directoryPath 是 null 或空，说明路径有问题（例如，不是一个有效的文件路径）
        // 或者文件就在驱动器的根目录（如 "C:\myfile.txt"），这种情况下无需创建目录。
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            // For root paths like "C:\file.txt", GetDirectoryName returns null.
            // We can proceed if the root exists.
            if (!Directory.Exists(Path.GetPathRoot(path)))
            {
                throw new DirectoryNotFoundException($"The root directory for path '{path}' was not found.");
            }
        }
        else
        {
            // 2. 检查目录是否存在，如果不存在，则递归创建所有父目录
            // Directory.CreateDirectory is idempotent: it does nothing if the directory already exists.
            Directory.CreateDirectory(directoryPath);
        }

        // 3. 使用信号量来确保文件写入的线程安全
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            // 4. 执行异步文件写入
            // 使用 File.WriteAllTextAsync, 它比手动管理 FileStream 更简洁高效
            await File.WriteAllTextAsync(path, content, encoding ?? Encoding.UTF8, cancellationToken);
        }
        finally
        {
            _fileLock.Release();
        }
    }
}