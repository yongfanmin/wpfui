// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

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
        }

        /// <summary>
        /// Asynchronously copies an existing file to a new file, overwriting the destination file if it already exists.
        /// </summary>
        public static async Task CopyFileAsync(string sourceFilePath, string destinationFilePath)
        {
            // --- 异步版本的实现 ---
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

            try
            {
                string? destinationDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                // .NET 6+ 提供了 File.CopyAsync 但是不知道为什么我使用了 .NET9 却没有这份方法 暂时不处理
                // #if NET6_0_OR_GREATER
                // await File.CopyAsync(sourceFilePath, destinationFilePath, true);
                // #else
                // 对于旧版.NET，我们可以用流来模拟异步复制
                await using (FileStream sourceStream = File.Open(sourceFilePath, FileMode.Open, FileAccess.Read))
                {
                    await using (FileStream destinationStream = File.Create(destinationFilePath))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }
                }
                // #endif
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to copy file from '{sourceFilePath}' to '{destinationFilePath}' asynchronously.", ex);
            }
        }
}