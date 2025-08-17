// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Refit;

namespace Wpf.Ui.Gallery.Apis;

public class LoginRequest
{
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}

public class LoginResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public interface ILoginApi
{
    [Post("/api/machine/login")]
    Task<LoginResponse> Login([Body] LoginRequest request, CancellationToken cancellationToken);
}
