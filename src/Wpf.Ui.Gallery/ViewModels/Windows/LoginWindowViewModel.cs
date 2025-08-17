// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Services.Contracts;
using Wpf.Ui.Gallery.Views.Windows;

namespace Wpf.Ui.Gallery.ViewModels.Windows;

public partial class LoginWindowViewModel : ObservableObject
{
    private readonly ILoginApi _loginApi;
    private readonly ISnackbarService _snackbarService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _account = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public LoginWindowViewModel(
        ILoginApi loginApi,
        ISnackbarService snackbarService,
        IServiceProvider serviceProvider
    )
    {
        _loginApi = loginApi;
        _snackbarService = snackbarService;
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private async Task Login()
    {
        IsLoading = true;

        var request = new LoginRequest { Account = Account, Password = Password };

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _loginApi.Login(request, cts.Token);

            if (response.Success)
            {
                var mainWindow = _serviceProvider.GetRequiredService<IWindow>();
                mainWindow.Show();

                Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault()?.Close();
            }
            else
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Login Failed",
                    Content = response.Message ?? "An unknown error occurred.",
                    CloseButtonText = "OK"
                };

                _ = await messageBox.ShowDialogAsync();
            }
        }
        catch (OperationCanceledException)
        {
            _snackbarService.Show(
                "Login Failed",
                "The request timed out. Please try again.",
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24),
                TimeSpan.FromSeconds(5)
            );
        }
        catch (Exception e)
        {
            _snackbarService.Show(
                "Login Failed",
                e.Message,
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24),
                TimeSpan.FromSeconds(5)
            );
        }
        finally
        {
            IsLoading = false;
        }
    }
}
