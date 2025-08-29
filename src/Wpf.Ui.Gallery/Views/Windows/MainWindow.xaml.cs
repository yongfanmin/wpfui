// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

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
    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        IServiceProvider serviceProvider,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService
    )
    {
        Appearance.SystemThemeWatcher.Watch(this);

        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();

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
        if (WindowState == WindowState.Minimized)
        {
            //Hide();
            //WindowState = WindowState.Normal;
        }
    }

    private void NotifyIcon_OnLeftClick(NotifyIcon sender, RoutedEventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
}