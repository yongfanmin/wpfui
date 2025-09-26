// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Concurrent;
using Wpf.Ui.Gallery.LocalConfig;

namespace Wpf.Ui.Gallery.Config;

public static class ParallelTaskRunner
{
    /// <summary>
    /// Executes a list of actions concurrently with a specified maximum degree of parallelism,
    /// and returns a Task that completes when all actions are finished.
    /// </summary>
    public static Task RunAllWithLimitedConcurrencyAsync(
        IEnumerable<Action> taskList,
        int maxConcurrency)
    {
        if (taskList == null)
        {
            throw new ArgumentNullException(nameof(taskList));
        }
        if (maxConcurrency < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Concurrency must be at least 1.");
        }

        // --- [核心解决方案在这里] ---

        // 1. 将整个并行操作，包裹在一个 Task.Run 中
        //    这使得整个方法可以立即返回一个可供等待的Task，而不会阻塞调用者
        return Task.Run(() =>
        {
            // 2. 设置并行选项，精确控制CPU核心占用
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency
            };

            // 用于线程安全地收集所有任务中发生的异常
            var exceptions = new ConcurrentQueue<Exception>();

            // 3. 使用 Parallel.ForEach 并传入我们的选项
            Parallel.ForEach(taskList, parallelOptions, action =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    // 捕获单个任务的异常
                    exceptions.Enqueue(ex);
                }
            });

            // 4. 在所有并行任务都结束后，检查是否有异常发生
            if (!exceptions.IsEmpty)
            {
                // 如果有异常，则抛出一个 AggregateException，
                // 这会让调用方 await 的 Task 进入 Faulted 状态
                throw new AggregateException(exceptions);
            }
        });
    }
}