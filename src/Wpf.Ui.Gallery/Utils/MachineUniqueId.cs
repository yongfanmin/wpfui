// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;

// 建议将此类放在项目的工具类或帮助类命名空间下
namespace Wpf.Ui.Gallery.Utils;

/// <summary>
/// 提供一个基于硬件信息生成唯一且稳定机器ID的静态工具类。
/// This is a robust implementation for generating a unique machine identifier.
/// </summary>
public static class MachineUniqueId
{
    private static string _machineId;
    private static readonly object SyncLock = new object();

    /// <summary>
    /// 获取当前机器的唯一ID (基于主板、CPU、主硬盘序列号的SHA256哈希值)。
    /// 结果会被缓存，在程序生命周期内多次调用只会计算一次。
    /// </summary>
    /// <param name="truncateTo">可选参数，将完整的哈希ID截断为指定长度的字符数。默认为0，即不截断。</param>
    /// <returns>唯一的机器ID字符串。</returns>
    /// <exception cref="InvalidOperationException">当无法获取任何有效的硬件信息时抛出。</exception>
    public static string GetId(int truncateTo = 0)
    {
        // 使用双重检查锁定模式确保线程安全和高性能
        if (_machineId == null)
        {
            lock (SyncLock)
            {
                if (_machineId == null)
                {
                    var hardwareIds = new StringBuilder();

                    // 1. 获取主板序列号 (通常只有一个)
                    hardwareIds.Append($"MB:[{GetHardwareIdentifiers("Win32_BaseBoard", "SerialNumber")}]");

                    // 2. 获取所有CPU的ID
                    hardwareIds.Append($"-CPU:[{GetHardwareIdentifiers("Win32_Processor", "ProcessorId")}]");

                    // 3. 获取所有物理硬盘的序列号
                    hardwareIds.Append($"-DISK:[{GetHardwareIdentifiers("Win32_DiskDrive", "SerialNumber")}]");

                    string combinedId = hardwareIds.ToString();

                    // 如果所有信息都为空，则这是一个严重问题
                    if (combinedId == "MB:[]-CPU:[]-DISK:[]")
                    {
                        throw new InvalidOperationException(
                            "Could not retrieve any valid hardware identifiers. Cannot generate a unique machine ID.");
                    }

                    _machineId = ComputeSha256Hash(combinedId);
                }
            }
        }

        if (truncateTo > 0 && truncateTo < _machineId.Length)
        {
            return _machineId.Substring(0, truncateTo);
        }

        return _machineId;
    }

    /// <summary>
    /// 获取指定WMI类的所有实例的属性值，并进行排序和拼接。
    /// </summary>
    /// <param name="wmiClass">WMI类名 (e.g., "Win32_Processor")</param>
    /// <param name="wmiProperty">WMI属性名 (e.g., "ProcessorId")</param>
    /// <returns>一个经过排序和逗号分隔的标识符字符串。</returns>
    private static string GetHardwareIdentifiers(string wmiClass, string wmiProperty)
    {
        var identifiers = new List<string>();
        try
        {
            // 注意：必须使用 using 来确保 ManagementObjectSearcher 和其使用的资源被正确释放
            using (var searcher = new ManagementObjectSearcher($"SELECT {wmiProperty} FROM {wmiClass}"))
            {
                foreach (var obj in searcher.Get())
                {
                    using (obj) // 同样，ManagementObject 也需要被 Dispose
                    {
                        string value = obj[wmiProperty]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(value))
                        {
                            identifiers.Add(value);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 在生产环境中，可以考虑使用日志框架记录此错误
            Console.WriteLine($"Error retrieving WMI info for {wmiClass}: {ex.Message}");
            // 不抛出异常，而是返回空集合，让调用者决定如何处理信息缺失
        }

        // 对获取到的ID进行排序，以确保顺序的稳定性
        identifiers.Sort();

        return string.Join(",", identifiers);
    }

    /// <summary>
    /// 计算字符串的SHA256哈希值。
    /// </summary>
    private static string ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            // 将字节数组转换为小写的十六进制字符串
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}