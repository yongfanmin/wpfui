// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Text.Json;
using Wpf.Ui.Gallery.Apis;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Models;
using Wpf.Ui.Gallery.Utils;

namespace Wpf.Ui.Gallery.Services;

public class LoginInfoService
{
    private readonly string _cacheFilePath;

    public LoginInfo? CurrentLoginInfo { get; private set; }
    
    public LoginRequest? LoginRequest { get; private set; }

    public LoginInfoService()
    {
        _cacheFilePath = Path.Combine(PathName.Config, FileName.UserCacheFileName);
    }

    public async void SaveLoginInfo(LoginRequest loginRequest, LoginInfo loginInfo)
    {
        CurrentLoginInfo = loginInfo;

        // File.WriteAllTextAsync(_cacheFilePath, json);
        await FileHelper.WriteTextRobustAsync(_cacheFilePath, JsonSerializer.Serialize(loginRequest));
    }

    public void SetSessionOnly(LoginInfo loginInfo)
    {
        CurrentLoginInfo = loginInfo;
    }

    public string getToken()
    {
        // TODO 不严谨的判断 需改
        if (CurrentLoginInfo != null && CurrentLoginInfo.UserInfo != null && CurrentLoginInfo.UserInfo.Token != null)
        {
            //return "Bearer " + CurrentLoginInfo.UserInfo.Token;
            return CurrentLoginInfo.UserInfo.Token;
        }

        return null;
    }

    public void LoadLoginInfo()
    {
        if (!File.Exists(_cacheFilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_cacheFilePath);
            LoginRequest = JsonSerializer.Deserialize<LoginRequest>(json);
        }
        catch (Exception)
        {
            // If deserialization fails, treat it as no cache
            LoginRequest = null;
        }
    }

    public void ClearLoginRequest()
    {
        LoginRequest = null;
        if (File.Exists(_cacheFilePath))
        {
            File.Delete(_cacheFilePath);
        }
    }
}