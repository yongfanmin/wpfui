// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Lepo.i18n.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Refit;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Wpf.Ui.DependencyInjection;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.DependencyModel;
using Wpf.Ui.Gallery.Handlers;
using Wpf.Ui.Gallery.ImageProcessor;
using Wpf.Ui.Gallery.LocalConfig;
using Wpf.Ui.Gallery.Resources;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Services.Contracts;
using Wpf.Ui.Gallery.Services.Creator;
using Wpf.Ui.Gallery.Services.Database;
using Wpf.Ui.Gallery.Services.Downloader;
using Wpf.Ui.Gallery.Utils;
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

    // 工厂管理后台地址
    public static readonly string FactoryManageUrl = "https://factory.gongwohuo.cn";

    private static readonly string _apiDomain = FactoryManageUrl;
    // private static readonly string _domain = "https://factory.gongwohuo.cn";
    
    
    
    public static readonly LoggingLevelSwitch LevelSwitch = new(LogEventLevel.Error);
    
    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .UseSerilog(
            (context, configuration) =>
            {
                configuration
                    .MinimumLevel.ControlledBy(LevelSwitch)
                    .Enrich.FromLogContext()
                    .WriteTo.File(
                        FileName.LogFileFullPath,
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 100 * 1024 * 1024,
                        retainedFileCountLimit: 10,
                        retainedFileTimeLimit: TimeSpan.FromDays(3)
                    );
            }
        )
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

                // Main window container with navigation
                _ = services.AddSingleton<IWindow, MainWindow>();
                _ = services.AddSingleton<MainWindowViewModel>();
                _ = services.AddSingleton<INavigationService, NavigationService>();
                _ = services.AddSingleton<ISnackbarService, SnackbarService>();
                _ = services.AddSingleton<IContentDialogService, ContentDialogService>();
                _ = services.AddSingleton<WindowsProviderService>();
                // Login 登录窗口
                _ = services.AddSingleton<LoginWindow>();
                _ = services.AddSingleton<LoginWindowViewModel>();
                _ = services.AddSingleton<Services.LoginInfoService>();
                _ = services.AddSingleton<ProduceBatchItemPage>();
                _ = services.AddSingleton<ProduceBatchItemViewModel>();
                // 打印面弹窗
                _ = services.AddTransient<PrintDialog>();
                _ = services.AddTransient<PrintDialogViewModel>();
                // 打印任务弹窗
                _ = services.AddTransient<CreatePrintTaskWindow>();


                _ = services.AddSingleton<SettingsPage>();
                _ = services.AddSingleton<SettingsViewModel>();
                // Top-level pages
                _ = services.AddSingleton<DashboardPage>();
                _ = services.AddSingleton<DashboardViewModel>();
                _ = services.AddSingleton<AllControlsPage>();
                _ = services.AddSingleton<AllControlsViewModel>();
                _ = services.AddSingleton<SettingsPage>();
                _ = services.AddSingleton<SettingsViewModel>();
                _ = services.AddSingleton<ProcessStepScanPage>();
                _ = services.AddSingleton<ProcessStepScanViewModel>();
                _ = services.AddSingleton<PickingPage>();
                _ = services.AddSingleton<PickingViewModel>();
                
                // 图片下载
                _ = services.AddSingleton<IImageDownloader, ImageDownloader>();

                // 图片创建
                _ = services.AddSingleton<IImageCreator, ImageCreator>();

                // 生产图处理
                _ = services.AddSingleton<IProduceImageProcessor, ProduceImageProcessor>();

                _ = services.AddSingleton<IDatabaseService, DatabaseService>();
                
                // Photoshop组件
                _ = services.AddSingleton<Component.PhotoshopService>();
                
                // All other pages and view models
                _ = services.AddTransientFromNamespace("Wpf.Ui.Gallery.Views", GalleryAssembly.Asssembly);
                _ = services.AddTransientFromNamespace(
                    "Wpf.Ui.Gallery.ViewModels",
                    GalleryAssembly.Asssembly
                );

                _ = services.AddTransient<NetworkActivityHandler>();
                
                _ = services.AddStringLocalizer(b =>
                {
                    b.FromResource<Translations>(new("pl-PL"));
                });
                
                _ = services
                    .AddRefitClient<ILoginApi>()
                    .ConfigureHttpClient(c =>
                    {
                        //接口域名 接口地址 登录接口
                        c.BaseAddress = new Uri(_apiDomain);
                    });
                _ = services
                    .AddRefitClient<ILayoutApi>()
                    .ConfigureHttpClient(c =>
                    {
                        //接口域名 接口地址 排版接口
                        c.BaseAddress = new Uri(_apiDomain);
                    });
                _ = services
                    .AddRefitClient<IProduceBatchApi>()
                    .ConfigureHttpClient(c =>
                    {
                        //接口域名 接口地址 生产计划接口
                        c.BaseAddress = new Uri(_apiDomain);
                    }).AddHttpMessageHandler<NetworkActivityHandler>();
                _ = services
                    .AddRefitClient<IProduceBatchInfoApi>()
                    .ConfigureHttpClient(c =>
                    {
                        //接口域名 接口地址 生产计划信息接口
                        c.BaseAddress = new Uri(_apiDomain);
                    }).AddHttpMessageHandler<NetworkActivityHandler>();
                _ = services
                    .AddRefitClient<IProduceBatchDetailApi>()
                    .ConfigureHttpClient(c =>
                    {
                        //接口域名 接口地址 生产计划详情接口
                        c.BaseAddress = new Uri(_apiDomain);
                    }).AddPolicyHandler(HttpPolicyExtensions
                        // 选择要处理的异常和HTTP错误
                        .HandleTransientHttpError() // 自动处理 HttpRequestException, 5xx 状态码, 408 状态码
                        .Or<IOException>() // 特别加入 IOException 来捕获 "The response ended prematurely"
                        .Or<Refit.ApiException>(ex => ex.StatusCode == System.Net.HttpStatusCode.InternalServerError) // 也可以处理特定的 Refit 异常
                        // 设置重试策略：重试4次，每次等待时间为 2^n 秒 (即 3, 6, 9, 27 秒)
                        .WaitAndRetryAsync(4, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)))).AddHttpMessageHandler<NetworkActivityHandler>();
                _ = services
                    .AddRefitClient<IOrderApi>()
                    .ConfigureHttpClient(c =>
                    {
                        //接口域名 接口地址 订单接口
                        c.BaseAddress = new Uri(_apiDomain);
                    }).AddHttpMessageHandler<NetworkActivityHandler>();
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
        // 读取本地磁盘配置
        LocalAppConfig.Load();
        LevelSwitch.MinimumLevel = LocalAppConfig.AppSetting.LogLevel;
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        //程序启动 进入程序 开启程序 打开软件
        //_host.Start();

        initDatabase();
        AutoCleanup();
        _host.StartAsync();

        //var loginWindow = GetRequiredService<LoginWindow>();
        //loginWindow.Show();
        LoginWindow loginWindow = GetRequiredService<LoginWindow>();
        // 弹出登录窗口
        loginWindow.Show();

        //初始化线程池
        ThreadPoolConfig.Initialize();
    }
    
    // 自动数据清理
    private void AutoCleanup()
    {
        try
        {
            var databaseService = GetRequiredService<IDatabaseService>();
            databaseService.DeleteOldProductionData(7);

            FileHelper.DeleteFilesOlderThan(LocalAppConfig.AppSetting.PrintedPatternFilePath, 7);
            FileHelper.CleanupOldPatternPrintImages(7);
        }
        catch (Exception ex)
        {
            GetRequiredService<ILogger<App>>().LogError(ex, "An error occurred during automatic cleanup.");
        }
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
        try
        {
            var logger = GetRequiredService<ILogger<App>>();
            logger.LogError(e.Exception, "An unhandled exception occurred.");
        }
        finally
        {
            e.Handled = true;
        }
    }
    
    private void initDatabase()
    {
        IDatabaseService databaseService = GetRequiredService<IDatabaseService>();
        databaseService.InitializeDatabase();
    }
}
