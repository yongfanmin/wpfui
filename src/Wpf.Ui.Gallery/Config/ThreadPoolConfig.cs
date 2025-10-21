// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.LocalConfig;

namespace Wpf.Ui.Gallery.Config;

public class ThreadPoolConfig
{
    /// <summary>
    /// Initializes the ThreadPool with a minimum number of threads.
    /// This is an optional optimization step.
    /// </summary>
    public static void Initialize()
    {
        // 获取CPU核心数
        int processorCount = LocalAppConfig.AppSetting.GetParallelThreads();
            
        // 这是一个可选的性能优化：
        // 告诉线程池，请“预热”并至少准备好 `processorCount` 个线程。
        // 这样可以减少在前几个任务到达时，因需要创建新线程而带来的微小延迟。
        // workerThreads: 工作线程
        // completionPortThreads: I/O线程
        ThreadPool.SetMinThreads(1, processorCount);
        ThreadPool.SetMaxThreads(LocalAppConfig.AppSetting.GetParallelThreads(), LocalAppConfig.AppSetting.GetParallelThreads());
        Console.WriteLine($"内置线程池已初始化，最小工作线程数: {processorCount}");
    }

    /// <summary>
    /// Enqueues a task to be executed by the built-in .NET ThreadPool.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    public static void Enqueue(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        // [核心代码在这里]
        // 将一个 Action 包装在 WaitCallback 中，并将其排入队列。
        // 第一个参数是将在线程池线程上执行的方法。
        // 第二个参数是传递给该方法的对象（在这里我们不需要，所以是 null）。
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                // 在线程池线程中执行用户的Action
                action();
            }
            catch (Exception ex)
            {
                // 强烈建议：在线程池的顶层捕获所有异常，
                // 否则未处理的异常可能会终止整个应用程序进程。
                Console.WriteLine($"在线程池任务中发生未处理的异常: {ex}");
            }
        });
    }
    
    public static void EnqueueAsync(Func<Task> asyncAction)
    {
        if (asyncAction == null)
        {
            throw new ArgumentNullException(nameof(asyncAction));
        }

        // 2. 我们仍然使用 QueueUserWorkItem 来将工作排入线程池
        ThreadPool.QueueUserWorkItem(async _ =>
        {
            try
            {
                // 3. [关键] 在线程池线程中，我们现在可以 'await' 异步委托
                await asyncAction();
            }
            catch (Exception ex)
            {
                // 统一捕获所有在异步任务链中发生的异常
                Console.WriteLine($"在线程池异步任务中发生未处理的异常: {ex}");
            }
        });
    }
}