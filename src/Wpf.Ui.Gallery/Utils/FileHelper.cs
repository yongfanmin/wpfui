// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.Machine;
using Wpf.Ui.Gallery.LocalConfig;
using System.IO;

namespace Wpf.Ui.Gallery.Utils;

public static class FileHelper
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

    private static SemaphoreSlim GetLock(string filePath)
    {
        return _fileLocks.GetOrAdd(Path.GetFullPath(filePath).ToUpperInvariant(), _ => new SemaphoreSlim(1, 1));
    }
    
    // <summary>
    /// 配置JSON序列化器的选项，使其生成的JSON字符串带缩进，更易于阅读。
    /// </summary>
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        // 2. 设置编码器，以防止中文字符被转义成unicode
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
    };

    #region 同步方法 (Synchronous Methods)

    /// <summary>
    /// 将一个对象序列化为JSON格式，并同步写入到指定的文件路径。
    /// 如果目录不存在，会自动创建。
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型。</typeparam>
    /// <param name="filePath">目标文件的完整路径。</param>
    /// <param name="objectToWrite">要写入文件的对象实例。</param>
    /// <exception cref="ArgumentNullException">当 objectToWrite 为 null 时抛出。</exception>
    /// <exception cref="Exception">封装了文件写入或序列化过程中可能出现的其他异常。</exception>
    public static void WriteToJsonFile<T>(T objectToWrite)
    {
        
        if (!LocalAppConfig._filePathRegistry.TryGetValue(typeof(T), out var filePath))
        {
            throw new KeyNotFoundException($"The type '{typeof(T).FullName}' has not been registered in the AppDataManager.[此泛型未保存在本地磁盘]");
        }
        
        if (objectToWrite == null)
        {
            throw new ArgumentNullException(nameof(objectToWrite));
        }
        
        var fileLock = GetLock(filePath);
        fileLock.Wait();
        try
        {
            // 确保目标目录存在
            string directoryName = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            // 序列化对象为JSON字符串
            string jsonString = JsonSerializer.Serialize(objectToWrite, _options);

            // 将JSON字符串写入文件
            File.WriteAllText(filePath, jsonString);
        }
        catch (Exception ex)
        {
            // 抛出带有更多上下文信息的新异常，便于调试
            throw new Exception($"Failed to write to file '{filePath}'.", ex);
        }
        finally
        {
            fileLock.Release();
        }
    }

    /// <summary>
    /// 同步读取一个JSON文件，并将其反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">期望反序列化成的目标类型。</typeparam>
    /// <param name="filePath">源文件的完整路径。</param>
    /// <returns>反序列化后的对象实例。</returns>
    /// <exception cref="FileNotFoundException">当指定的文件不存在时抛出。</exception>
    /// <exception cref="JsonException">当文件内容不是有效的JSON或无法转换为目标类型时抛出。</exception>
    /// <exception cref="Exception">封装了文件读取或反序列化过程中可能出现的其他异常。</exception>
    public static T ReadFromJsonFile<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }
        
        var fileLock = GetLock(filePath);
        fileLock.Wait();
        try
        {
            // 从文件读取JSON字符串
            string jsonString = File.ReadAllText(filePath);

            // 反序列化JSON字符串为对象
            T deserializedObject = JsonSerializer.Deserialize<T>(jsonString);

            return deserializedObject;
        }
        catch (JsonException ex)
        {
            // 专门处理JSON解析错误
            throw new JsonException(
                $"Failed to deserialize file '{filePath}' as type '{typeof(T).Name}'. Check for malformed JSON.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to read from file '{filePath}'.", ex);
        }
        finally
        {
            fileLock.Release();
        }
    }

    #endregion

    #region 异步方法 (Asynchronous Methods)

    /// <summary>
    /// 将一个对象序列化为JSON格式，并异步写入到指定的文件路径。
    /// 如果目录不存在，会自动创建。
    /// </summary>
    public static async Task WriteToJsonFileAsync<T>(string filePath, T objectToWrite)
    {
        if (objectToWrite == null)
        {
            throw new ArgumentNullException(nameof(objectToWrite));
        }

        var fileLock = GetLock(filePath);
        await fileLock.WaitAsync();
        try
        {
            string directoryName = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            string jsonString = JsonSerializer.Serialize(objectToWrite, _options);

            await File.WriteAllTextAsync(filePath, jsonString);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to write to file '{filePath}' asynchronously.", ex);
        }
        finally
        {
            fileLock.Release();
        }
    }

    /// <summary>
    /// 异步读取一个JSON文件，并将其反序列化为指定类型的对象。
    /// </summary>
    public static async Task<T> ReadFromJsonFileAsync<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        var fileLock = GetLock(filePath);
        await fileLock.WaitAsync();
        try
        {
            string jsonString = await File.ReadAllTextAsync(filePath);

            T deserializedObject = JsonSerializer.Deserialize<T>(jsonString);

            return deserializedObject;
        }
        catch (JsonException ex)
        {
            throw new JsonException(
                $"Failed to deserialize file '{filePath}' as type '{typeof(T).Name}'. Check for malformed JSON.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to read from file '{filePath}' asynchronously.", ex);
        }
        finally
        {
            fileLock.Release();
        }
    }

    #endregion
    
    
    public static T ReadFromJsonFileAuto<T>()
    {
        if (!LocalAppConfig._filePathRegistry.TryGetValue(typeof(T), out var filePath))
        {
            throw new KeyNotFoundException($"The type '{typeof(T).FullName}' has not been registered in the AppDataManager.[此泛型未保存在本地磁盘]");
        }
        
        if (!File.Exists(filePath))
        {
            //throw new FileNotFoundException("File not found.", filePath);
            return default;
        }
        
        var fileLock = GetLock(filePath);
        fileLock.Wait();
        try
        {
            // 从文件读取JSON字符串
            string jsonString = File.ReadAllText(filePath);

            // 反序列化JSON字符串为对象
            T deserializedObject = JsonSerializer.Deserialize<T>(jsonString);

            return deserializedObject;
        }
        catch (JsonException ex)
        {
            // 专门处理JSON解析错误
            throw new JsonException(
                $"Failed to deserialize file '{filePath}' as type '{typeof(T).Name}'. Check for malformed JSON.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to read from file '{filePath}'.", ex);
        }
        finally
        {
            fileLock.Release();
        }
    }
    
     public static void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            // --- 1. 参数校验 ---
            if (string.IsNullOrWhiteSpace(sourceFilePath))
            {
                throw new ArgumentException("Source file path cannot be null or empty.", nameof(sourceFilePath));
            }
            if (string.IsNullOrWhiteSpace(destinationFilePath))
            {
                throw new ArgumentException("Destination file path cannot be null or empty.", nameof(destinationFilePath));
            }
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("The source file was not found.", sourceFilePath);
            }

            var destinationLock = GetLock(destinationFilePath);
            destinationLock.Wait();
            try
            {
                // --- 2. [核心] 确保目标目录存在 ---
                string? destinationDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                // --- 3. [核心] 执行文件复制 ---
                // File.Copy 的第三个参数 `overwrite` 设置为 true，
                // 即可实现“如果目标文件存在则覆盖”的功能。
                File.Copy(sourceFilePath, destinationFilePath, true);
            }
            catch (Exception ex)
            {
                // 封装异常，提供更多上下文信息
                throw new Exception($"Failed to copy file from '{sourceFilePath}' to '{destinationFilePath}'.", ex);
            }
            finally
            {
                destinationLock.Release();
            }
        }

        /// <summary>
        /// Asynchronously copies an existing file to a new file, overwriting the destination file if it already exists.
        /// </summary>
        public static async Task CopyFileAsync(string sourceFilePath, string destinationFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
            {
                throw new ArgumentException("Source file path cannot be null or empty.", nameof(sourceFilePath));
            }
            if (string.IsNullOrWhiteSpace(destinationFilePath))
            {
                throw new ArgumentException("Destination file path cannot be null or empty.", nameof(destinationFilePath));
            }
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("The source file was not found.", sourceFilePath);
            }

            var destinationLock = GetLock(destinationFilePath);
            await destinationLock.WaitAsync();
            try
            {
                string? destinationDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                await using (FileStream sourceStream = File.Open(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await using (FileStream destinationStream = File.Create(destinationFilePath))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to copy file from '{sourceFilePath}' to '{destinationFilePath}' asynchronously.", ex);
            }
            finally
            {
                destinationLock.Release();
            }
        }
        
    /// <summary>
    /// Deletes files in a specified directory that are older than a given number of days.
    /// </summary>
    /// <param name="directoryPath">The path to the directory.</param>
    /// <param name="days">The age of files (in days) to be deleted.</param>
    public static void DeleteFilesOlderThan(string directoryPath, int days)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            var cutoffDate = DateTime.Now.AddDays(-days);

            // Step 1: Get original timestamps of all subdirectories before any modification.
            var subdirectories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
            var originalDirectoryTimes = subdirectories.ToDictionary(
                dir => dir,
                dir => new DirectoryInfo(dir).LastWriteTime
            );

            // Step 2: Recursively get all files and delete the old ones.
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime < cutoffDate)
                {
                    var fileLock = GetLock(file);
                    fileLock.Wait();
                    try
                    {
                        fileInfo.Delete();
                    }
                    finally
                    {
                        fileLock.Release();
                    }
                }
            }

            // Step 3: Delete directories that are now empty AND were originally old.
            foreach (var dir in subdirectories.OrderByDescending(d => d.Length))
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    if (originalDirectoryTimes.TryGetValue(dir, out var originalTime) && originalTime < cutoffDate)
                    {
                        try
                        {
                            Directory.Delete(dir);
                        }
                        catch (IOException) { /* Ignore errors like directory in use */ }
                        catch (UnauthorizedAccessException) { /* Ignore permission errors */ }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Optional: Add logging here to record the exception.
            throw new Exception($"Failed to delete files in directory '{directoryPath}'.", ex);
        }
    }

    /// <summary>
    /// Cleans up old pattern print images from the cache directories.
    /// </summary>
    /// <param name="days">The age of files (in days) to be deleted.</param>
    public static void CleanupOldPatternPrintImages(int days)
    {
        string cachePath = Path.Combine(AppContext.BaseDirectory, "Cache");
        if (!Directory.Exists(cachePath))
        {
            return;
        }

        var cutoffDate = DateTime.Now.AddDays(-days);
        var factoryDirs = Directory.GetDirectories(cachePath, "Factory-*");

        foreach (var factoryDir in factoryDirs)
        {
            var orderBatchDirs = Directory.GetDirectories(factoryDir, "Order-batch-*");
            foreach (var orderBatchDir in orderBatchDirs)
            {
                var dirInfo = new DirectoryInfo(orderBatchDir);
                if (dirInfo.LastWriteTime < cutoffDate)
                {
                    try
                    {
                        Directory.Delete(orderBatchDir, true); // Recursive delete
                    }
                    catch (IOException) { /* Optional: Log error */ }
                    catch (UnauthorizedAccessException) { /* Optional: Log error */ }
                }
            }
        }
    }
}