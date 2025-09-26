// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.LocalConfig;
using Wpf.Ui.Gallery.ViewModels.Windows;

namespace Wpf.Ui.Gallery.ViewModels.Pages;

public sealed partial class SettingsViewModel(INavigationService navigationService) 
    : ObservableObject, INavigationAware
{
    private bool _isInitialized = false;

    [ObservableProperty]
    private string _printedPatternFilePath = LocalAppConfig.AppSetting.PrintedPatternFilePath;
    
    [ObservableProperty]
    private string _appVersion = string.Empty;

    [ObservableProperty]
    private ApplicationTheme _currentApplicationTheme = ApplicationTheme.Unknown;

    [ObservableProperty]
    private NavigationViewPaneDisplayMode _currentApplicationNavigationStyle =
        NavigationViewPaneDisplayMode.Left;
    
    [ObservableProperty]
    private ProduceImgLayoutFolderClassify _produceImgLayoutFolderClassify = ProduceImgLayoutFolderClassify.ByProduceBatch;

    [ObservableProperty]
    private string _fileNameFormatString = string.Empty;

    private readonly MainWindowViewModel _mainWindowViewModel;

    public async Task OnNavigatedToAsync()
    {
        if (!_isInitialized)
        {
            await InitializeViewModelAsync();
        }

        // 每次进入页面时都刷新配置
        PrintedPatternFilePath = LocalAppConfig.AppSetting.PrintedPatternFilePath;
        ProduceImgLayoutFolderClassify = LocalAppConfig.AppSetting.ProduceImgLayoutFolderClassify;
        UpdateFileNameFormatString();
    }

    /// <summary>
    /// Asynchronously called when the page is navigated away from.
    /// </summary>
    public Task OnNavigatedFromAsync()
    {
        // 清理工作现在也应该是异步的
        ApplicationThemeManager.Changed -= OnThemeChanged;
        return Task.CompletedTask; // 对于没有异步操作的清理，返回一个已完成的任务
    }

    /// <summary>
    /// Performs one-time asynchronous initialization.
    /// </summary>
    private async Task InitializeViewModelAsync()
    {
        // (如果未来有任何异步初始化，可以放在这里)
        // await SomeAsyncInitialization();
            
        CurrentApplicationTheme = ApplicationThemeManager.GetAppTheme();
        AppVersion = $"{GetAssemblyVersion()}";
            
        ApplicationThemeManager.Changed += OnThemeChanged;
            
        _isInitialized = true;

        // 返回一个已完成的任务
        await Task.CompletedTask;
    }
    
    
    partial void OnCurrentApplicationThemeChanged(ApplicationTheme oldValue, ApplicationTheme newValue)
    {
        ApplicationThemeManager.Apply(newValue);
    }

    partial void OnCurrentApplicationNavigationStyleChanged(
        NavigationViewPaneDisplayMode oldValue,
        NavigationViewPaneDisplayMode newValue
    )
    {
        _ = navigationService.SetPaneDisplayMode(newValue);
    }

    private void InitializeViewModel()
    {
        CurrentApplicationTheme = ApplicationThemeManager.GetAppTheme();
        AppVersion = $"{GetAssemblyVersion()}";

        ApplicationThemeManager.Changed += OnThemeChanged;
        UpdateFileNameFormatString();
        _isInitialized = true;
    }
    
    private void UpdateFileNameFormatString()
    {
        IEnumerable<string> mappedItems = LocalAppConfig.AppSetting.ProduceImgNameFormatList.Select(format =>
        {
            return format switch
            {
                ProduceImgNameFormat.Size => "尺寸",
                ProduceImgNameFormat.Color => "颜色",
                ProduceImgNameFormat.ProductName => "产品名",
                ProduceImgNameFormat.BatchNum => "项批次",
                _ => string.Empty,
            };
        });

        FileNameFormatString = string.Join("-", mappedItems);
    }

    private void OnThemeChanged(ApplicationTheme currentApplicationTheme, Color systemAccent)
    {
        // Update the theme if it has been changed elsewhere than in the settings.
        if (CurrentApplicationTheme != currentApplicationTheme)
        {
            CurrentApplicationTheme = currentApplicationTheme;
        }
    }

    private static string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
    }
    
    [RelayCommand]
    private void OnOpenFolder()
    {
        string path = LocalAppConfig.AppSetting.PrintedPatternFilePath;

        // 1. 健壮性检查：确保路径存在
        //    这可以防止因路径无效而导致无法预测的行为
        if (!Directory.Exists(path))
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        }

        var folderDialog = new OpenFolderDialog
        {
            Title = "Select Folder",
            InitialDirectory = path
        };

        if (folderDialog.ShowDialog() == true)
        {
            folderDialog.FolderName = folderDialog.FolderName + Path.DirectorySeparatorChar;
            LocalAppConfig.AppSetting.PrintedPatternFilePath = folderDialog.FolderName;
            PrintedPatternFilePath = folderDialog.FolderName;
            LocalAppConfig.Save(LocalAppConfig.AppSetting);
        }
    }
    
    [RelayCommand]
    private void OnFolderClassifyChanged(string parameter)
    {
        if (Enum.TryParse<ProduceImgLayoutFolderClassify>(parameter, out var folderClassify))
        {
            LocalAppConfig.AppSetting.ProduceImgLayoutFolderClassify = folderClassify;
            LocalAppConfig.Save(LocalAppConfig.AppSetting);
            Console.WriteLine($"Folder classification changed to: {ProduceImgLayoutFolderClassify}");
        }
    }
}
