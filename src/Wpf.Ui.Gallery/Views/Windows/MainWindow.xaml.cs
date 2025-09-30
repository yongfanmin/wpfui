// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.ComponentModel;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.LocalConfig;
using Wpf.Ui.Gallery.Services.Contracts;
using Wpf.Ui.Gallery.ViewModels.Windows;
using Wpf.Ui.Gallery.Views.Pages;
using Wpf.Ui.Tray.Controls;

namespace Wpf.Ui.Gallery.Views.Windows;

public partial class MainWindow : IWindow
{
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    private bool _isPaneOpenedOrClosedFromCode;

    private bool _isUserClosedPane;

    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        IServiceProvider serviceProvider,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService
    )
    {
        _serviceProvider = serviceProvider;
        _navigationService = navigationService;

        // Visibility = Visibility.Hidden;

        SystemThemeWatcher.Watch(this);

        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();

        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        navigationService.SetNavigationControl(NavigationView);
        contentDialogService.SetDialogHost(RootContentDialog);
        // navigationService.Navigate(typeof(DashboardPage));
        ApplicationThemeManager.Apply(
            LocalAppConfig.AppSetting.ApplicationTheme
        );
        Loaded += OnLoaded;
    }

    public MainWindowViewModel ViewModel { get; }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private void OnNavigationSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not NavigationView navigationView)
        {
            return;
        }

        NavigationView.SetCurrentValue(
            NavigationView.HeaderVisibilityProperty,
            navigationView.SelectedItem?.TargetPageType != typeof(DashboardPage)
                ? Visibility.Visible
                : Visibility.Collapsed
        );
    }

    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isUserClosedPane)
        {
            return;
        }

        _isPaneOpenedOrClosedFromCode = true;
        NavigationView.SetCurrentValue(NavigationView.IsPaneOpenProperty, e.NewSize.Width > 1200);
        _isPaneOpenedOrClosedFromCode = false;
    }

    private void NavigationView_OnPaneOpened(NavigationView sender, RoutedEventArgs args)
    {
        if (_isPaneOpenedOrClosedFromCode)
        {
            return;
        }

        _isUserClosedPane = false;
    }

    private void NavigationView_OnPaneClosed(NavigationView sender, RoutedEventArgs args)
    {
        if (_isPaneOpenedOrClosedFromCode)
        {
            return;
        }

        _isUserClosedPane = true;
    }
    
    private void MainWindow_OnClosing(object sender, CancelEventArgs e)
    {
        // 打印一条调试信息，确认事件已触发
        System.Diagnostics.Debug.WriteLine("窗口关闭事件已被拦截。");

        // 显示一个确认对话框
        var result = System.Windows.MessageBox.Show(
            "关闭软件生产将停止,是否确定关闭？",
            "退出确认",
            System.Windows.MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        // 检查用户的选择
        if (result == System.Windows.MessageBoxResult.No)
        {
            // 如果用户点击了“否”，则取消关闭操作
            e.Cancel = true;
            System.Diagnostics.Debug.WriteLine("窗口关闭操作已被用户取消。");
        }
        else
        {
            // 如果用户点击了“是”，则不执行任何操作，窗口将继续正常关闭
            // 在这里您可以添加一些清理资源的代码
            System.Diagnostics.Debug.WriteLine("用户确认关闭，窗口将关闭。");
        }
    }
    
    private void NotifyIcon_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not NotifyIcon notifyIcon)
        {
            return;
        }

        if (notifyIcon.IsRegistered)
        {
            return;
        }

        notifyIcon.Register();
    }
}