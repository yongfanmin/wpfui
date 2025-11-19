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
                Cleanup();
            }
        }

        string progID = FindLatestPhotoshopProgID();
        if (string.IsNullOrEmpty(progID))
        {
            throw new InvalidOperationException("未在系统中找到任何已安装的 Adobe Photoshop。");
        }
            
        try
        {
            // 现在因为有了正确的 using 指令，这个方法可以被正确解析
            _photoshopApp = Marshal.GetActiveObject(progID);
        }
        catch (COMException)
        {
            Type psType = Type.GetTypeFromProgID(progID);
            if (psType == null)
            {
                throw new InvalidOperationException($"无法通过 ProgID '{progID}' 创建 Photoshop 实例。");
            }
            _photoshopApp = Activator.CreateInstance(psType);
        }

        if (_photoshopApp == null)
        {
            throw new InvalidOperationException("无法连接或创建 Photoshop 实例。");
        }
            
        _photoshopApp.Visible = true;
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
            // 使用 dynamic 关键字进行后期绑定
            dynamic app = null;
            string result = "未知错误。";
            // 1. 自动查找已安装的最新版 Photoshop 的 ProgID
            string progID = FindLatestPhotoshopProgID();
            if (string.IsNullOrEmpty(progID))
            {
                return (false, "未在系统中找到任何已安装的 Adobe Photoshop。");
            }

            // 2. 通过 ProgID 动态创建 Photoshop 实例
            Type psType = Type.GetTypeFromProgID(progID);
            if (psType == null)
            {
                return (false, $"无法通过 ProgID '{progID}' 创建 Photoshop 实例。");
            }
            // TODO PS未响应报错 (一直重复开关PS  内存耗尽?)
            /*System.Runtime.InteropServices.COMException (0x80080005): Retrieving the COM class factory for component with CLSID {DD6CF2ED-4840-40A1-A393-B1F74F54D59A} failed due to the following error: 80080005 服务器运行失败 (0x80080005 (CO_E_SERVER_EXEC_FAILURE)).
                at System.RuntimeTypeHandle.AllocateComObject(Void* pClassFactory)
            at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
            at Wpf.Ui.Gallery.Component.PhotoshopService.<>c__DisplayClass0_0.<ProcessImageAsync>b__0() in D:\POD\exeSoftware\wpf-exe-master\src\Wpf.Ui.Gallery\Component\PhotoshopService.cs:line 51
            at System.Threading.Tasks.Task`1.InnerInvoke()
            at System.Threading.ExecutionContext.RunFromThreadPoolDispatchLoop(Thread threadPoolThread, ExecutionContext executionContext, ContextCallback callback, Object state)*/
            app = Activator.CreateInstance(psType);
            if (app is null)
            {
                return (false,"与 Photoshop 通信时发生错误。请确保 Photoshop 已正确安装。");
            }
            // 3. 设置为静默运行
            // 注意：在后期绑定中，我们直接调用方法，如果方法或属性不存在，会在运行时抛出异常。
            // 这段JS代码是与版本无关的，非常安全。
            // TODO 应该是重复开关PS导致的错误
            // System.Runtime.InteropServices.COMException (0x8001010A): 消息筛选器显示应用程序正在使用中。 (0x8001010A (RPC_E_SERVERCALL_RETRYLATER))
            // System.Runtime.InteropServices.COMException (0x80010108): 被调用的对象已与其客户端断开连接。 (0x80010108 (RPC_E_DISCONNECTED))
            // System.Runtime.InteropServices.COMException (0x80042260): 发生了常规 Photoshop 错误。该功能可能无法在这个版本的 Photoshop 中使用。
            // Microsoft.CSharp.RuntimeBinder.RuntimeBinderException: 'System.__ComObject' does not contain a definition for 'DoJavaScript'
            app.DoJavaScript("app.displayDialogs = DialogModes.NO;");

            List<string> errorList = new List<string>();
            try
            {
                foreach (string imagePath in imagePathList)
                {
                    if (File.Exists(imagePath))
                    {
                        try
                        {
                            // 4. 准备并执行脚本
                            string newFileName = Path.GetFileNameWithoutExtension(imagePath);
                            object[] arguments = { imagePath, "", "" };

                            // 【关键修正】PsJavaScriptExecutionMode 枚举在后期绑定中不可用，
                            // 我们直接使用其整数值。psNeverShowDebugger 的值是 2。
                            const int psNeverShowDebugger = 2;
                            result = app.DoJavaScriptFile(jsxScriptPath, arguments, psNeverShowDebugger);

                            // 5. 处理脚本返回值
                            // 如果脚本成功但没有返回值，DoJavaScriptFile 可能会返回 null
                            if (string.IsNullOrEmpty(result) || result.Trim().Equals("Success", StringComparison.OrdinalIgnoreCase))
                            {

                            }
                            else
                            {
                                errorList.Add(imagePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            errorList.Add(imagePath);
                        }
                    }
                }
            }
            finally
            {
                // 预检查
                // 6. 【至关重要】关闭并彻底释放 COM 对象

                if (app != null)
                {
                    app.Quit();
                    Marshal.ReleaseComObject(app);
                    // 主动进行垃圾回收，有助于清理COM的运行时可调用包装 (RCW)
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            return (true, "处理成功！");
        });
    }
    
    /// <summary>
    /// 释放对 Photoshop COM 对象的引用。
    /// 应在您的应用程序关闭时调用此方法。
    /// </summary>
    public static void Cleanup()
    {
        if (_photoshopApp != null)
        {
            Marshal.ReleaseComObject(_photoshopApp);
            _photoshopApp = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
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
}