// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Net.Http;
using Lepo.i18n.DependencyInjection;
using Refit;
using Wpf.Ui.DependencyInjection;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.DependencyModel;
using Wpf.Ui.Gallery.ImageProcessor;
using Wpf.Ui.Gallery.Resources;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Services.Contracts;
using Wpf.Ui.Gallery.Services.Creator;
using Wpf.Ui.Gallery.Services.Downloader;
using Wpf.Ui.Gallery.ViewModels.Pages;
using Wpf.Ui.Gallery.ViewModels.Windows;
using Wpf.Ui.Gallery.Views.Pages;
using Wpf.Ui.Gallery.Views.Windows;

namespace Wpf.Ui.Gallery;

public partial class App
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging

    private static readonly string _domain = "http://factory.sds-diy.xyz";
    
    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(c =>
        {
            _ = c.SetBasePath(AppContext.BaseDirectory);
        })
        .ConfigureServices(
            (_1, services) =>
            {
                _ = services.AddNavigationViewPageProvider();
                
                // App Host
                _ = services.AddHostedService<ApplicationHostService>();
                // 1. 确保 HttpClient 已经被注册 (作为单例是最佳实践)
                _ = services.AddSingleton<HttpClient>();
                // Main window container with navigation
                _ = services.AddSingleton<IWindow, MainWindow>();
                _ = services.AddSingleton<MainWindowViewModel>();
                _ = services.AddSingleton<INavigationService, NavigationService>();
                _ = services.AddSingleton<ISnackbarService, SnackbarService>();
                _ = services.AddSingleton<IContentDialogService, ContentDialogService>();
                _ = services.AddSingleton<WindowsProviderService>();
                
                // Login 登录窗口
                _ = services.AddTransient<LoginWindow>();
                _ = services.AddTransient<LoginWindowViewModel>();

                // 添加用户信息本地存储服务
                _ = services.AddSingleton<LoginInfoService>();
                
                
                // 添加主面板服务
                _ = services.AddTransient<DashboardViewModel>();


                // Top-level pages
                _ = services.AddSingleton<DashboardPage>();
                _ = services.AddSingleton<DashboardViewModel>();
                _ = services.AddSingleton<AllControlsPage>();
                _ = services.AddSingleton<AllControlsViewModel>();
                _ = services.AddSingleton<SettingsPage>();
                _ = services.AddSingleton<SettingsViewModel>();
                // 图片下载
                _ = services.AddSingleton<IImageDownloader, ImageDownloader>();
                
                // 图片创建
                _ = services.AddSingleton<IImageCreator, ImageCreator>();
                
                // 生产图处理
                _ = services.AddSingleton<IProduceImageProcessor, ProduceImageProcessor>();

                // All other pages and view models
                _ = services.AddTransientFromNamespace("Wpf.Ui.Gallery.Views", GalleryAssembly.Asssembly);
                _ = services.AddTransientFromNamespace(
                    "Wpf.Ui.Gallery.ViewModels",
                    GalleryAssembly.Asssembly
                );

                _ = services.AddStringLocalizer(b =>
                {
                    b.FromResource<Translations>(new("pl-PL"));
                });
                
                _ = services
                    .AddRefitClient<ILoginApi>()
                    .ConfigureHttpClient(c =>
                    {
                        //接口域名 接口地址 登录接口
                        c.BaseAddress = new Uri(_domain);
                    });
                _ = services
                    .AddRefitClient<IProduceBatchApi>()
                    .ConfigureHttpClient(c =>
                    {
                        //接口域名 接口地址 生产批次接口
                        c.BaseAddress = new Uri(_domain);
                    });
                _ = services
                    .AddRefitClient<IProduceBatchInfoApi>()
                    .ConfigureHttpClient(c =>
                    {
                        //接口域名 接口地址 生产批次信息接口
                        c.BaseAddress = new Uri(_domain);
                    });
                _ = services
                    .AddRefitClient<IProduceBatchDetailApi>()
                    .ConfigureHttpClient(c =>
                    {
                        //接口域名 接口地址 生产批次详情接口
                        c.BaseAddress = new Uri(_domain);
                    });
            }
        )
        .Build();

    /// <summary>
    /// Gets registered service.
    /// </summary>
    /// <typeparam name="T">Type of the service to get.</typeparam>
    /// <returns>Instance of the service or <see langword="null"/>.</returns>
    public static T GetRequiredService<T>()
        where T : class
    {
        return _host.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Occurs when the application is loading.
    /// </summary>
    private void OnStartup(object sender, StartupEventArgs e)
    {
        Console.InputEncoding = System.Text.Encoding.UTF8;
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        //程序启动 进入程序 开启程序 打开软件
        //_host.Start();
        
        _host.StartAsync();

        //var loginWindow = GetRequiredService<LoginWindow>();
        //loginWindow.Show();
        var loginWindow = GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    /// <summary>
    /// Occurs when the application is closing.
    /// </summary>
    private void OnExit(object sender, ExitEventArgs e)
    {
        _host.StopAsync().Wait();

        _host.Dispose();
    }

    /// <summary>
    /// Occurs when an exception is thrown by an application but not handled.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
    }
}
