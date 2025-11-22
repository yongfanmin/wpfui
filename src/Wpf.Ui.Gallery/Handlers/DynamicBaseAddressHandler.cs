// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.


using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Wpf.Ui.Gallery.LocalConfig;

namespace Wpf.Ui.Gallery.Handlers;

public class DynamicBaseAddressHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var domain = LocalAppConfig.AppSetting.Domain;
        if (!string.IsNullOrEmpty(domain))
        {
            request.RequestUri = new Uri(
                new Uri(domain),
                request.RequestUri?.PathAndQuery.TrimStart('/')
            );
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}