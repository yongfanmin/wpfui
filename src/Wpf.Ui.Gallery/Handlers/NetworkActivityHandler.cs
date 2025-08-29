// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.ViewModels.Windows;

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Wpf.Ui.Gallery.Message;


namespace Wpf.Ui.Gallery.Handlers;

public class NetworkActivityHandler : DelegatingHandler
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    private readonly DispatcherTimer _pollingTimer;
    
    public NetworkActivityHandler(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _mainWindowViewModel.IsNetworkActive = true;

        // 任何订阅了此消息类型的地方，都会收到通知 (主要用来广播给网络请求指示灯 让灯闪烁用的 DashboardView页面)
        WeakReferenceMessenger.Default.Send(new NetworkActivityChangedMessage(_mainWindowViewModel.IsNetworkActive));
        try
        {
            // Call the inner handler.
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            _mainWindowViewModel.IsNetworkActive = false;
            WeakReferenceMessenger.Default.Send(new NetworkActivityChangedMessage(_mainWindowViewModel.IsNetworkActive));
        }
    }
}