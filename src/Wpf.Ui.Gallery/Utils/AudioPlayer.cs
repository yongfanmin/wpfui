// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.LocalConfig;

namespace Wpf.Ui.Gallery.Utils;

public static class AudioPlayer
{
     private static readonly List<MediaPlayer> PlayingMedia = new List<MediaPlayer>();

     private static bool _isPlaying = false;
     
     public static void PlayManualWaiting()
     {
         if (!_isPlaying)
         {
             _isPlaying = true;
             PlayAudio(AppContext.BaseDirectory + "/Assets/Audio/manual_waiting.mp3");
             _isPlaying = false;
         }
     }

     public static void PlaySuccessAudio()
     {
         PlayAudio(AppContext.BaseDirectory + "/Assets/Audio/success.mp3");
     }
     
     public static void PlayErrorAudio()
     {
         PlayAudio(AppContext.BaseDirectory + "/Assets/Audio/error.mp3");
     }
     
     public static void PlayClearBasketAudio()
     {
         PlayAudio(AppContext.BaseDirectory + "/Assets/Audio/clear_basket.mp3");
     }
     
     public static void PlayCompleteAudio()
     {
         PlayAudio(AppContext.BaseDirectory + "/Assets/Audio/complete.mp3");
     }
     
    /// <summary>
    /// [最终版本] 以“即发即忘”(Fire-and-Forget)的方式异步播放一个音频文件。
    /// </summary>
    public static async void PlayAudio(string audioFilePath)
    {
        if (!LocalAppConfig.AppSetting.IsSoundEnabled)
        {
            return;
        }
        
        if (!File.Exists(audioFilePath))
        {
            Console.WriteLine($"音频文件不存在: {audioFilePath}");
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var mediaPlayer = new MediaPlayer();

            // --- [这是最关键的、正确的 API 调用] ---
            // 1. 为每个事件声明独立的、类型完全匹配的委托变量
            EventHandler? onMediaEnded = null;
            EventHandler<ExceptionEventArgs>? onMediaFailed = null;

            // 2. 创建一个统一的清理方法，避免代码重复
            Action cleanup = () =>
            {
                // 确保解除所有订阅
                if (onMediaEnded != null) mediaPlayer.MediaEnded -= onMediaEnded;
                if (onMediaFailed != null) mediaPlayer.MediaFailed -= onMediaFailed;

                mediaPlayer.Close();
                lock (PlayingMedia)
                {
                    PlayingMedia.Remove(mediaPlayer);
                }
            };

            // 3. 定义并订阅 MediaEnded 事件的处理程序
            onMediaEnded = (sender, e) =>
            {
                cleanup();
            };

            // 4. 定义并订阅 MediaFailed 事件的处理程序
            onMediaFailed = (sender, e) =>
            {
                Console.WriteLine($"音频播放失败: {e.ErrorException.Message}");
                cleanup();
            };

            mediaPlayer.MediaEnded += onMediaEnded;
            mediaPlayer.MediaFailed += onMediaFailed;

            try
            {
                mediaPlayer.Open(new Uri(audioFilePath, UriKind.Absolute));
                
                lock (PlayingMedia)
                {
                    PlayingMedia.Add(mediaPlayer);
                }

                mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打开音频文件时出错: {ex.Message}");
                // 如果打开失败，也需要执行清理
                cleanup();
            }
        });
    }
}