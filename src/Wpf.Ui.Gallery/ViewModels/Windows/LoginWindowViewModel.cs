// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Services;
using Wpf.Ui.Gallery.Services.Contracts;
using Wpf.Ui.Gallery.Views.Windows;

namespace Wpf.Ui.Gallery.ViewModels.Windows;

public partial class LoginWindowViewModel : ObservableObject
{
    private readonly ILoginApi _loginApi;
    private readonly ISnackbarService _snackbarService;
    private readonly IServiceProvider _serviceProvider;
    private readonly LoginInfoService _loginInfoService;

    [ObservableProperty]
    private string _account = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _rememberMe;

    public LoginWindowViewModel(
        ILoginApi loginApi,
        ISnackbarService snackbarService,
        IServiceProvider serviceProvider,
        LoginInfoService loginInfoService
    )
    {
        _loginApi = loginApi;
        _snackbarService = snackbarService;
        _serviceProvider = serviceProvider;
        _loginInfoService = loginInfoService;
        loginInfoService.LoadLoginInfo();
        if (loginInfoService.LoginRequest is not null)
        {
            // 原来是保存登录后信息  可以直接跳过
            //var mainWindow = GetRequiredService<IWindow>();
            //mainWindow.Show();
            // 改成 存储账号密码 还是需要手动点击登录按钮 (避免上面的存储方式 还需要判断token是否在有效期内)
            _account = loginInfoService.LoginRequest.Account;
            _password = loginInfoService.LoginRequest.Password;
            _rememberMe = true;
        }
    }

    [RelayCommand]
    private async Task Login()
    {
        IsLoading = true;
        var request = new LoginRequest { Account = Account, Password = Password };

        try
        {
            FactoryApiResponse<LoginInfo> response = await _loginApi.Login(request);

            if (response.IsSuccess)
            {
                var mainWindow = _serviceProvider.GetRequiredService<IWindow>();
                mainWindow.Show();

                Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault()?.Close();

                if (RememberMe)
                {
                    _loginInfoService.SaveLoginInfo(request, response.Data);
                }
                else
                {
                    _loginInfoService.ClearLoginRequest();
                }

                _loginInfoService.SetSessionOnly(response.Data);
            }
            else
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Login Failed",
                    Content = response.Msg ?? "An unknown error occurred.",
                    CloseButtonText = "OK"
                };

                _ = await messageBox.ShowDialogAsync();
            }
        }
        catch (OperationCanceledException)
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Login Failed",
                Content = "An error occurred.",
                CloseButtonText = "OK"
            };

            _ = await messageBox.ShowDialogAsync();
        }
        catch (Exception e)
        {
            IsLoading = false;
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "登录出错",
                Content = "An error occurred.",
                CloseButtonText = "OK"
            };

            _ = await messageBox.ShowDialogAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
