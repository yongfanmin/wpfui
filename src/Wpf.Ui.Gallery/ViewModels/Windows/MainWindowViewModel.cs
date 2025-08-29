// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Extensions.Localization;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using Wpf.Ui.Gallery.Dto.Machine;
using Wpf.Ui.Gallery.Resources;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Utils;
using Wpf.Ui.Gallery.Views.Pages;
using Wpf.Ui.Gallery.Views.Pages.BasicInput;
using Wpf.Ui.Gallery.Views.Pages.Collections;
using Wpf.Ui.Gallery.Views.Pages.DateAndTime;
using Wpf.Ui.Gallery.Views.Pages.DesignGuidance;
using Wpf.Ui.Gallery.Views.Pages.DialogsAndFlyouts;
using Wpf.Ui.Gallery.Views.Pages.Layout;
using Wpf.Ui.Gallery.Views.Pages.Media;
using Wpf.Ui.Gallery.Views.Pages.Navigation;
using Wpf.Ui.Gallery.Views.Pages.OpSystem;
using Wpf.Ui.Gallery.Views.Pages.StatusAndInfo;
using Wpf.Ui.Gallery.Views.Pages.Text;
using Wpf.Ui.Gallery.Views.Pages.Windows;
using Wpf.Ui.Gallery.Views.Windows;

namespace Wpf.Ui.Gallery.ViewModels.Windows;

//public partial class MainWindowViewModel(IStringLocalizer<Translations> localizer) : ViewModel

public partial class MainWindowViewModel : ViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LoginInfoService _loginInfoService;
    //[ObservableProperty]
    //private string _applicationTitle = localizer["WPF UI Gallery"];

    [ObservableProperty]
    private string _applicationTitle;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty] private bool _isNetworkActive = false;
    
    private bool _isInitialized = false;
    private readonly INavigationService _navigationService;
    private readonly IContentDialogService _contentDialogService;

    public MainWindowViewModel(
        IStringLocalizer<Translations> localizer,
        IServiceProvider serviceProvider,
        LoginInfoService loginInfoService,
        INavigationService navigationService,
        IContentDialogService contentDialogService
    )
    {
        _serviceProvider = serviceProvider;
        _loginInfoService = loginInfoService;
        _navigationService = navigationService;
        _contentDialogService = contentDialogService;
        _applicationTitle = localizer["获取用户名称失败"];

        if (_loginInfoService.CurrentLoginInfo is not null)
        {
            UserName = _loginInfoService.CurrentLoginInfo.UserInfo.UserName;
        }
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        string machineUniqueId = MachineUniqueId.GetId();
        MachineConfig machineConfig = FileHelper.ReadFromJsonFileAuto<MachineConfig>();

        if (machineConfig is null)
        {
            PopulateNavigationItems();
            _navigationService.Navigate(typeof(Views.Pages.DashboardPage));
            // 暂时跳过校验 逻辑有点问题
            // Populate with ONLY the settings item
            /*MenuItems.Clear();
            FooterMenuItems.Clear();
            FooterMenuItems.Add(new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            });

            // Navigate to settings and show the dialog
            _navigationService.Navigate(typeof(Views.Pages.SettingsPage));

            // 强制弹出提示框
            var machineConfigNotExistBox = await _contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions()
                {
                    Title = "软件初始化警告",
                    Content = "机器未配置 请先配置此机器参数.",
                    PrimaryButtonText = "前往配置机器",
                    CloseButtonText = "退出软件",
                }
            );*/
        }
        else if(machineConfig.MachineUniqueId != machineUniqueId)
        {
            var machineConfigModifyBox = await _contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions()
                {
                    Title = "软件初始化警告",
                    Content = "机器有变化 请重新配置此机器参数.",
                    PrimaryButtonText = "前往配置机器",
                    CloseButtonText = "退出软件",
                }
            );
            if (machineConfigModifyBox == ContentDialogResult.Primary)
            {
                // 跳到配置页面
                _navigationService.Navigate(typeof(Views.Pages.SettingsPage));
            }
            else
            {
                // 关闭程序
                Application.Current.Shutdown();
            }
        }
        else
        {
            // If config is fine, populate the full menu
            PopulateNavigationItems();
            _navigationService.Navigate(typeof(Views.Pages.DashboardPage));
        }
        
        _isInitialized = true;
    }

    public void PopulateNavigationItems()
    {
        MenuItems.Clear();
        FooterMenuItems.Clear();

        MenuItems.Add(new NavigationViewItem("Home", SymbolRegular.Home24, typeof(DashboardPage)));
        MenuItems.Add(new NavigationViewItem()
        {
            Content = "Design guidance",
            Icon = new SymbolIcon { Symbol = SymbolRegular.DesignIdeas24 },
            MenuItemsSource = new object[]
            {
                new NavigationViewItem("Typography", SymbolRegular.TextFont24, typeof(TypographyPage)),
                new NavigationViewItem("Icons", SymbolRegular.Diversity24, typeof(IconsPage)),
                new NavigationViewItem("Colors", SymbolRegular.Color24, typeof(ColorsPage)),
            },
        });
        MenuItems.Add(new NavigationViewItem("All samples", SymbolRegular.List24, typeof(AllControlsPage)));
        MenuItems.Add(new NavigationViewItemSeparator());
        MenuItems.Add(new NavigationViewItem("Basic Input", SymbolRegular.CheckboxChecked24, typeof(BasicInputPage))
        {
            MenuItemsSource = new object[]
            {
                new NavigationViewItem(nameof(Anchor), typeof(AnchorPage)),
                new NavigationViewItem(nameof(Wpf.Ui.Controls.Button), typeof(ButtonPage)),
                new NavigationViewItem(nameof(DropDownButton), typeof(DropDownButtonPage)),
                new NavigationViewItem(nameof(HyperlinkButton), typeof(HyperlinkButtonPage)),
                new NavigationViewItem(nameof(ToggleButton), typeof(ToggleButtonPage)),
                new NavigationViewItem(nameof(ToggleSwitch), typeof(ToggleSwitchPage)),
                new NavigationViewItem(nameof(CheckBox), typeof(CheckBoxPage)),
                new NavigationViewItem(nameof(ComboBox), typeof(ComboBoxPage)),
                new NavigationViewItem(nameof(RadioButton), typeof(RadioButtonPage)),
                new NavigationViewItem(nameof(RatingControl), typeof(RatingPage)),
                new NavigationViewItem(nameof(ThumbRate), typeof(ThumbRatePage)),
                new NavigationViewItem(nameof(SplitButton), typeof(SplitButtonPage)),
                new NavigationViewItem(nameof(Slider), typeof(SliderPage)),
            },
        });
        MenuItems.Add(new NavigationViewItem
        {
            Content = "Collections",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Table24 },
            TargetPageType = typeof(CollectionsPage),
            MenuItemsSource = new object[]
            {
                new NavigationViewItem(nameof(System.Windows.Controls.DataGrid), typeof(DataGridPage)),
                new NavigationViewItem(nameof(ListBox), typeof(ListBoxPage)),
                new NavigationViewItem(nameof(Ui.Controls.ListView), typeof(ListViewPage)),
                new NavigationViewItem(nameof(TreeView), typeof(TreeViewPage)),
#if DEBUG
                new NavigationViewItem("TreeList", typeof(TreeListPage)),
#endif
            },
        });
        MenuItems.Add(new NavigationViewItem("Date & time", SymbolRegular.CalendarClock24, typeof(DateAndTimePage))
        {
            MenuItemsSource = new object[]
            {
                new NavigationViewItem(nameof(CalendarDatePicker), typeof(CalendarDatePickerPage)),
                new NavigationViewItem(nameof(System.Windows.Controls.Calendar), typeof(CalendarPage)),
                new NavigationViewItem(nameof(DatePicker), typeof(DatePickerPage)),
                new NavigationViewItem(nameof(TimePicker), typeof(TimePickerPage)),
            },
        });
        MenuItems.Add(new NavigationViewItem("Dialogs & flyouts", SymbolRegular.Chat24, typeof(DialogsAndFlyoutsPage))
        {
            MenuItemsSource = new object[]
            {
                new NavigationViewItem(nameof(Snackbar), typeof(SnackbarPage)),
                new NavigationViewItem(nameof(ContentDialog), typeof(ContentDialogPage)),
                new NavigationViewItem(nameof(Flyout), typeof(FlyoutPage)),
                new NavigationViewItem(nameof(Wpf.Ui.Controls.MessageBox), typeof(MessageBoxPage)),
            },
        });
#if DEBUG
        MenuItems.Add(new NavigationViewItem("Layout", SymbolRegular.News24, typeof(LayoutPage))
        {
            MenuItemsSource = new object[]
            {
                new NavigationViewItem("Expander", typeof(ExpanderPage)),
                new NavigationViewItem("CardControl", typeof(CardControlPage)),
                new NavigationViewItem("CardAction", typeof(CardActionPage)),
            },
        });
#endif
        MenuItems.Add(new NavigationViewItem
        {
            Content = "Media",
            Icon = new SymbolIcon { Symbol = SymbolRegular.PlayCircle24 },
            TargetPageType = typeof(MediaPage),
            MenuItemsSource = new object[]
            {
                new NavigationViewItem("Image", typeof(ImagePage)),
                new NavigationViewItem("Canvas", typeof(CanvasPage)),
                new NavigationViewItem("WebView", typeof(WebViewPage)),
                new NavigationViewItem("WebBrowser", typeof(WebBrowserPage)),
            },
        });
        MenuItems.Add(new NavigationViewItem("Navigation", SymbolRegular.Navigation24, typeof(NavigationPage))
        {
            MenuItemsSource = new object[]
            {
                new NavigationViewItem("BreadcrumbBar", typeof(BreadcrumbBarPage)),
                new NavigationViewItem("NavigationView", typeof(NavigationViewPage)),
                new NavigationViewItem("Menu", typeof(MenuPage)),
                new NavigationViewItem("Multilevel navigation", typeof(MultilevelNavigationPage)),
                new NavigationViewItem("TabControl", typeof(TabControlPage)),
            },
        });
        MenuItems.Add(new NavigationViewItem(
            "Status & info",
            SymbolRegular.ChatBubblesQuestion24,
            typeof(StatusAndInfoPage)
        )
        {
            MenuItemsSource = new object[]
            {
                new NavigationViewItem("InfoBadge", typeof(InfoBadgePage)),
                new NavigationViewItem("InfoBar", typeof(InfoBarPage)),
                new NavigationViewItem("ProgressBar", typeof(ProgressBarPage)),
                new NavigationViewItem("ProgressRing", typeof(ProgressRingPage)),
                new NavigationViewItem("ToolTip", typeof(ToolTipPage)),
            },
        });
        MenuItems.Add(new NavigationViewItem("Text", SymbolRegular.DrawText24, typeof(TextPage))
        {
            MenuItemsSource = new object[]
            {
                new NavigationViewItem(nameof(AutoSuggestBox), typeof(AutoSuggestBoxPage)),
                new NavigationViewItem(nameof(NumberBox), typeof(NumberBoxPage)),
                new NavigationViewItem(nameof(Wpf.Ui.Controls.PasswordBox), typeof(PasswordBoxPage)),
                new NavigationViewItem(nameof(Wpf.Ui.Controls.RichTextBox), typeof(RichTextBoxPage)),
                new NavigationViewItem(nameof(Label), typeof(LabelPage)),
                new NavigationViewItem(nameof(Wpf.Ui.Controls.TextBlock), typeof(TextBlockPage)),
                new NavigationViewItem(nameof(Wpf.Ui.Controls.TextBox), typeof(TextBoxPage)),
            },
        });
        MenuItems.Add(new NavigationViewItem("System", SymbolRegular.Desktop24, typeof(OpSystemPage))
        {
            MenuItemsSource = new object[]
            {
                new NavigationViewItem("Clipboard", typeof(ClipboardPage)),
                new NavigationViewItem("FilePicker", typeof(FilePickerPage)),
            },
        });
        MenuItems.Add(new NavigationViewItem("Windows", SymbolRegular.WindowApps24, typeof(WindowsPage)));

        FooterMenuItems.Add(new NavigationViewItem("Settings", SymbolRegular.Settings24, typeof(SettingsPage)));
    }

    [RelayCommand]
    private void Logout()
    {
        _loginInfoService.ClearLoginRequest();

        var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
        loginWindow.Show();

        // Close main window
        foreach (var window in Application.Current.Windows.OfType<MainWindow>())
        {
            window.Close();
        }
    }
    
    [ObservableProperty]
    private ObservableCollection<object> _menuItems = new();

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems = new();

    [ObservableProperty]
    private ObservableCollection<Wpf.Ui.Controls.MenuItem> _trayMenuItems =
    [
        new Wpf.Ui.Controls.MenuItem { Header = "Home", Tag = "tray_home" },
        new Wpf.Ui.Controls.MenuItem { Header = "Close", Tag = "tray_close" },
    ];
}
