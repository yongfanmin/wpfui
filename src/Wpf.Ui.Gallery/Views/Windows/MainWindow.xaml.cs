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

public partial class MainWindow : FluentWindow
{
    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        IServiceProvider serviceProvider,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService
    )
    {
        InitializeComponent();
        Appearance.SystemThemeWatcher.Watch(this);

        ViewModel = viewModel;
        DataContext = this;



        Loaded += async (sender, args) =>
        {
            await ViewModel.InitializeAsync();
        };

        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        navigationService.SetNavigationControl(NavigationView);
        contentDialogService.SetDialogHost(RootContentDialog);
        
        ApplicationThemeManager.Apply(
            LocalAppConfig.AppSetting.ApplicationTheme
        );
    }

    public MainWindowViewModel ViewModel { get; }

    private bool _isUserClosedPane;

    private bool _isPaneOpenedOrClosedFromCode;

    private void OnNavigationSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.NavigationView navigationView)
        {
            return;
        }

        /*NavigationView.SetCurrentValue(
            NavigationView.HeaderVisibilityProperty,
            navigationView.SelectedItem?.TargetPageType != typeof(DashboardPage)
                ? Visibility.Visible
                : Visibility.Collapsed
        );*/
    }

    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isUserClosedPane)
        {
            return;
        }

        _isPaneOpenedOrClosedFromCode = true;
        // NavigationView.SetCurrentValue(NavigationView.IsPaneOpenProperty, e.NewSize.Width > 1200);
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
    
    private void OnStateChanged(object sender, EventArgs e)
    {
        switch (WindowState)
        {
            case WindowState.Minimized:
                // 当窗口被最小化时执行这里的代码
                System.Diagnostics.Debug.WriteLine("窗口已最小化。");
                // 例如，您之前的逻辑：
                // Hide();
                // WindowState = WindowState.Normal;
                break;

            case WindowState.Maximized:
                // 当窗口被最大化时执行这里的代码
                System.Diagnostics.Debug.WriteLine("窗口已最大化。");
                break;

            case WindowState.Normal:
                // 当窗口恢复到正常大小时执行这里的代码
                System.Diagnostics.Debug.WriteLine("窗口已恢复正常大小。");
                break;
        }
    }
    
    private void OnClosing(object sender, CancelEventArgs e)
    {
        // 在这里添加关闭按钮被点击时的逻辑
        System.Diagnostics.Debug.WriteLine("窗口正在关闭...");

        // 示例：显示一个确认对话框，并根据用户的选择决定是否真的关闭窗口
        var result = System.Windows.MessageBox.Show("您确定要关闭应用程序吗？", "确认", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.No)
        {
            // 如果用户点击“否”，则取消关闭操作
            e.Cancel = true; 
            System.Diagnostics.Debug.WriteLine("窗口关闭操作已取消。");
        }
        else
        {
            // 如果用户点击“是”，则不执行任何操作，窗口会继续正常关闭
            System.Diagnostics.Debug.WriteLine("窗口将要关闭。");
        }
    }

    private void NotifyIcon_OnLeftClick(NotifyIcon sender, RoutedEventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
}