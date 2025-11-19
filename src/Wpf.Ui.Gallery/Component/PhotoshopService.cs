// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Config;

namespace Wpf.Ui.Gallery.Component;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Text.RegularExpressions;

public class PhotoshopService
{
    private static dynamic _photoshopApp = null;
    public static bool KeepAlive { get; set; } = false;
    
    /// <summary>
    /// 连接到正在运行的 Photoshop 实例，如果未运行，则创建一个新的实例。
    /// </summary>
    private static void GetOrConnectToPhotoshop()
    {
        if (_photoshopApp != null)
        {
            try
            {
                string appName = _photoshopApp.Name;
                return; 
            }
            catch (COMException)
            {
                Cleanup(true);
            }
        }

        string progID = FindLatestPhotoshopProgID();
        if (string.IsNullOrEmpty(progID))
        {
            throw new InvalidOperationException("未在系统中找到任何已安装的 Adobe Photoshop。");
        }
            
        try
        {
            _photoshopApp = GetActiveObjectByProgID(progID);
        }
        catch (COMException)
        {
            try
            {
                Type psType = Type.GetTypeFromProgID(progID);
                if (psType == null)
                {
                    throw new InvalidOperationException($"无法通过 ProgID '{progID}' 创建 Photoshop 实例。");
                }
                _photoshopApp = Activator.CreateInstance(psType);
            }
            catch (Exception)
            {
                Cleanup(true);
                throw;
            }
        }

        if (_photoshopApp == null)
        {
            throw new InvalidOperationException("无法连接或创建 Photoshop 实例。");
        }
            
        _photoshopApp.Visible = true;
        // 3. 设置为静默运行
        // 注意：在后期绑定中，我们直接调用方法，如果方法或属性不存在，会在运行时抛出异常。
        // 这段JS代码是与版本无关的，非常安全。
        // TODO 应该是重复开关PS导致的错误
        // System.Runtime.InteropServices.COMException (0x8001010A): 消息筛选器显示应用程序正在使用中。 (0x8001010A (RPC_E_SERVERCALL_RETRYLATER))
        // System.Runtime.InteropServices.COMException (0x80010108): 被调用的对象已与其客户端断开连接。 (0x80010108 (RPC_E_DISCONNECTED))
        // System.Runtime.InteropServices.COMException (0x80042260): 发生了常规 Photoshop 错误。该功能可能无法在这个版本的 Photoshop 中使用。
        // Microsoft.CSharp.RuntimeBinder.RuntimeBinderException: 'System.__ComObject' does not contain a definition for 'DoJavaScript'
        _photoshopApp.DoJavaScript("app.displayDialogs = DialogModes.NO;");
    }
    
    /// <summary>
    /// 异步在后台使用 Photoshop JSX 脚本处理单个图片，自动检测已安装的 Photoshop 版本。
    /// </summary>
    /// <param name="imagePath">要处理的图片的完整路径。</param>
    /// <param name="outputFolderPath">处理后图片的保存文件夹路径。</param>
    /// <param name="jsxScriptPath">要执行的 JSX 脚本的完整路径。</param>
    /// <returns>一个元组，包含操作是否成功以及结果或错误信息。</returns>
    public static async Task<(bool IsSuccess, string Message)> ProcessImageAsync(List<string> imagePathList)
    {
        string jsxScriptPath = FileName.getPhotoshopJsxScriptPath();
        if (!File.Exists(jsxScriptPath)) return (false, $"PS任务脚本不存在,请联系开发商: '{jsxScriptPath}'");
        if (imagePathList.Count == 0)
        {
            return (false, "待处理图片列表为空");
        }
        return await Task.Run(() =>
        {
            try
            {
                GetOrConnectToPhotoshop();
            }
            catch (Exception ex)
            {
                return (false, $"连接或启动 Photoshop 失败: {ex.Message}");
            }
    
            List<string> errorList = new List<string>();
            string result = "未知错误。";
            try
            {
                foreach (string imagePath in imagePathList)
                {
                    if (File.Exists(imagePath))
                    {
                        try
                        {
                            object[] arguments = { imagePath, "", "" };
                            const int psNeverShowDebugger = 2;
                            result = _photoshopApp.DoJavaScriptFile(jsxScriptPath, arguments, psNeverShowDebugger);
    
                            if (string.IsNullOrEmpty(result) || !result.Trim().Equals("Success", StringComparison.OrdinalIgnoreCase))
                            {
                                errorList.Add(Path.GetFileName(imagePath));
                            }
                        }
                        catch (Exception)
                        {
                            errorList.Add(Path.GetFileName(imagePath));
                        }
                    }
                }
            }
            finally
            {
                if (!KeepAlive)
                {
                    Cleanup();
                }
            }
    
            if (errorList.Count == imagePathList.Count)
            {
                // TODO 实际PS处理成功 但是有点报错 先跳过错误处理
                // return (false, "所有图片处理失败。");
            }
    
            if (errorList.Count > 0)
            {
                return (true, $"部分图片处理失败: {string.Join(", ", errorList)}");
            }
    
            return (true, "处理成功！");
        });
    }
    
    /// <summary>
    /// 释放对 Photoshop COM 对象的引用。
    /// 应在您的应用程序关闭时调用此方法。
    /// </summary>
    public static void Cleanup(bool force = false)
    {
        if (_photoshopApp != null)
        {
            if (force || !KeepAlive)
            {
                try
                {
                    _photoshopApp.Quit();
                }
                catch (COMException)
                {
                    // Ignore exceptions during quit, as Photoshop might have been closed manually.
                }
                finally
                {
                    Marshal.ReleaseComObject(_photoshopApp);
                    _photoshopApp = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }
    }

    /// <summary>
    /// 扫描 Windows 注册表，查找已安装的最新版本的 Photoshop 的 ProgID。
    /// </summary>
    /// <returns>最新版本的 ProgID (例如 "Photoshop.Application.170")，如果未找到则返回 null。</returns>
    private static string FindLatestPhotoshopProgID()
    {
        string baseKeyPath = @"Photoshop.Application";
        string latestProgID = null;
        int latestVersion = 0;

        try
        {
            using (RegistryKey classesRoot = Registry.ClassesRoot)
            {
                // 检查非版本化的 ProgID 是否存在，作为备选项
                if (classesRoot.OpenSubKey(baseKeyPath) != null)
                {
                    latestProgID = baseKeyPath;
                }

                // 遍历所有子键以查找版本化的 ProgID
                foreach (string subKeyName in classesRoot.GetSubKeyNames())
                {
                    if (subKeyName.StartsWith(baseKeyPath))
                    {
                        // 匹配类似 "Photoshop.Application.170" 的格式
                        Match match = Regex.Match(subKeyName, @"\.(\d+)$");
                        if (match.Success)
                        {
                            if (int.TryParse(match.Groups[1].Value, out int version))
                            {
                                // 找到一个版本号更高的，就更新它
                                if (version > latestVersion)
                                {
                                    latestVersion = version;
                                    latestProgID = subKeyName;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (System.Security.SecurityException)
        {
            // 无法访问注册表
            return null;
        }
        catch (Exception)
        {
            // 其他未知错误
            return null;
        }

        return latestProgID;
    }

    [DllImport("ole32.dll")]
    private static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid pclsid);

    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

    private static object GetActiveObjectByProgID(string progID)
    {
        Guid clsid;
        CLSIDFromProgID(progID, out clsid);

        object obj;
        GetActiveObject(ref clsid, IntPtr.Zero, out obj);
        return obj;
    }
}