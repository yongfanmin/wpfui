// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;

namespace Wpf.Ui.Gallery.Dto;

public class LoginInfo
{
    public UserInfo UserInfo { get; set; }
    
    public int isBatch { get; set; }
}

public class UserInfo
{
    [JsonPropertyName("id")]
    public long id { get; set; }
    
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
    
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }
    
    [JsonPropertyName("username")]
    public string UserName { get; set; }
    
    [JsonPropertyName("mobile")]
    public string Mobile { get; set; }
    
    [JsonPropertyName("avatar")]
    public string Avatar { get; set; }
    
    [JsonPropertyName("score")]
    public int Score { get; set; }
    
    [JsonPropertyName("token")]
    public string Token { get; set; }
    
    [JsonPropertyName("createtime")]
    public long CreateTime { get; set; }
    
    [JsonPropertyName("expiretime")]
    public long ExpireTime { get; set; }
    
    [JsonPropertyName("expire_in")]
    public long ExpireIn { get; set; }
    
    [JsonPropertyName("factory_id")]
    public int FactoryId { get; set; }
}