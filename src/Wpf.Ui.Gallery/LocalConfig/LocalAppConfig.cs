// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text.Json;
using Wpf.Ui.Appearance;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.Machine;
using Wpf.Ui.Gallery.Utils;

namespace Wpf.Ui.Gallery.LocalConfig;

// 1. 定义你的配置对象的结构
public class AppSetting
{
    public string Version { get; set; } = "0.1";

    public WindowSettings MainWindow { get; set; } = new();

    public ApplicationTheme ApplicationTheme { get; set; } = ApplicationTheme.Light;

    // 订单请求时间间隔/s 默认5秒
    private int _orderRequestDurationSec = 5;

    public int OrderRequestDurationSec
    {
        get
        {
            return _orderRequestDurationSec;
        }

        set
        {
            if (value < 3)
            {
                // 请求间隔不能小于3秒
                _orderRequestDurationSec = 3;
            }
            else
            {
                // 否则，使用传入的值
                _orderRequestDurationSec = value;
            }
        }
    }
}

public class WindowSettings
{
    public int Width { get; set; } = 1280;

    public int Height { get; set; } = 720;
}

// 2. 创建配置管理服务类
public class LocalAppConfig
{
    // 软件设置
    public static readonly string AppSettingFileName = "AppSetting.Config";


    //用户信息缓存文件名称
    public static readonly string UserCacheFileName = "UserInfo.cache";

    //本地缓存文件名称
    public static readonly string LocalConfigFileName = "Local.Config";

    //机器设置文件名称
    public static readonly string MachineConfigFileName = "MachineConfig.json";

    public static AppSetting AppSetting { get; private set; } = new();

    /// <summary>
    /// 核心：类型到文件路径的映射字典。
    /// 在这里集中注册所有需要自动管理的类型。
    /// 本地存储 本地文件 类型与文件地址映射关系
    /// </summary>
    public static readonly Dictionary<Type, string> _filePathRegistry = new()
    {
        // 注册 MachineConfig 类型，并指定其文件路径
        { typeof(MachineConfig), Path.Combine(PathName.Config, MachineConfigFileName) },

        // 注册 LoginInfo 类型
        { typeof(LoginInfo), Path.Combine(PathName.Config, UserCacheFileName) },

        // 软件设置 AppSetting 类型
        { typeof(AppSetting), Path.Combine(PathName.Config, AppSettingFileName) },
    };

    // 4. 使用静态构造函数来确定配置文件路径
    static LocalAppConfig()
    {
    }

    // 5. 提供一个公共的、静态的加载方法
    public static void Load()
    {
        AppSetting appSetting = FileHelper.ReadFromJsonFileAuto<AppSetting>();
        if (appSetting is not null)
        {
            if (AppSetting.Version != appSetting.Version)
            {
                // 磁盘配置文件跟代码配置文件不一致, 说明磁盘文件版本落后代码配置文件的版本, 则把代码配置文件的版本写入磁盘 (不然可能磁盘配置文件缺字段 虽然没影响)
                Save(AppSetting);
            }

            AppSetting = appSetting;
        }
        else
        {
            // 初始化软件配置
            AppSetting = new AppSetting();
            Save(AppSetting);
        }
    }

    // 6. 提供一个公共的、静态的保存方法
    public static void Save<T>(T localConfig)
    {
        FileHelper.WriteToJsonFile(localConfig);
    }
}