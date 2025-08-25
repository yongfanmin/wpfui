// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Utils;

namespace Wpf.Ui.Gallery.Services;

public class ConfigService
{
    private readonly string _cacheFilePath;

    public LoginInfo? CurrentLoginInfo { get; private set; }
    
    public LoginRequest? LoginRequest { get; private set; }

    public ConfigService()
    {
        _cacheFilePath = Path.Combine(PathName.Config, FileName.LocalConfigFileName);
    }

    public async void SaveLoginInfo(LoginRequest loginRequest, LoginInfo loginInfo)
    {
        CurrentLoginInfo = loginInfo;

        // File.WriteAllTextAsync(_cacheFilePath, json);
        await FileHelper.WriteTextRobustAsync(_cacheFilePath, JsonSerializer.Serialize(loginRequest));
    }

}