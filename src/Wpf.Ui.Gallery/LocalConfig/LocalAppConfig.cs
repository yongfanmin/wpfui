// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Serilog.Events;
using Wpf.Ui.Appearance;
using Wpf.Ui.Gallery.Config;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Dto;
using Wpf.Ui.Gallery.Dto.Machine;
using Wpf.Ui.Gallery.Dto.Picking;
using Wpf.Ui.Gallery.Utils;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Wpf.Ui.Gallery.LocalConfig;

// 1. 定义你的配置对象的结构
public class AppSetting
{
    // 增加或者修改字段 需要变更版本号才能刷新数据
    public string Version { get; set; } = "0.16";

    public WindowSettings MainWindow { get; set; } = new();

    public ApplicationTheme ApplicationTheme { get; set; } = ApplicationTheme.Light;

    // 日志输入等级
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Error;
    
    public int ComputerCpuThreads { get; set; } = Environment.ProcessorCount;

    // UI线程 默认保留1线程 防止程序或者其他应用卡死无响应
    public int ComputerUiThreads { get; set; } = 1;
    
    public int GetParallelThreads()
    {
        // 最大线程数 CPU核心数 - UI线程数 (避免占满所有CPU线程 导致操作界面卡顿 但是需要精确控制运行的线程 否则任务无法沾满工作线程 TODO 把IO类型 和 计算类型的 划分成两个线程池???)
        return Math.Max(ComputerCpuThreads - ComputerUiThreads , 1);
    }

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

    // 印花裁片/生产稿件存储路径
    public string PrintedPatternFilePath { get; set; } = FileName.DefaultPrintedPatternFilePath;

    // 默认印花裁片/生产稿件文件夹划分方式 (默认所有稿件放在同一个目录)
    public ProduceImgLayoutFolderClassify ProduceImgLayoutFolderClassify { get; set; } =
        ProduceImgLayoutFolderClassify.ByProduceBatch;

    // 文件夹分类对应的文件夹名称, 比如 存放在统一文件夹 =此文件夹叫 "统一文件夹" ; 按产品区分文件夹 = 此文件夹叫 "按产品区分" ; 文件夹名称可配置, 可中文可英文
    public Dictionary<ProduceImgLayoutFolderClassify, string> FolderNameMap { get; set; } =
        new Dictionary<ProduceImgLayoutFolderClassify, string>()
        {
            { ProduceImgLayoutFolderClassify.AllInOne, "统一文件夹" },
            { ProduceImgLayoutFolderClassify.ByOrder, "按单归类" },
            { ProduceImgLayoutFolderClassify.ByProduceBatch, "按计划归类" },
            { ProduceImgLayoutFolderClassify.ByProduct, "按产品归类" },
        };

    public string GetPrintedPatternFilePathAndClassifyFolder(string productName,long productId,string produceBatchNum,string orderNo)
    {
        string folderClassifyName = "";
        if (ProduceImgLayoutFolderClassify.Equals(ProduceImgLayoutFolderClassify.ByOrder))
        {
            folderClassifyName = "Order~" + $"[{orderNo}]" ;
        }
        if (ProduceImgLayoutFolderClassify.Equals(ProduceImgLayoutFolderClassify.ByProduceBatch))
        {
            folderClassifyName = "Batch~" +  $"[{produceBatchNum}]";
        }
        if (ProduceImgLayoutFolderClassify.Equals(ProduceImgLayoutFolderClassify.ByProduct))
        {
            folderClassifyName = "Product~" + productName + $"[{productId}]";
        }
        string directory = PrintedPatternFilePath + FolderNameMap[ProduceImgLayoutFolderClassify] + Path.DirectorySeparatorChar + folderClassifyName + Path.DirectorySeparatorChar;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        return directory;
    }

    // 生产图文件名称格式
    public List<ProduceImgNameFormat> ProduceImgNameFormatList { get; set; } = new()
    {
        // 尺码 颜色 产品名 项批号
        ProduceImgNameFormat.Size,
        ProduceImgNameFormat.Color,
        ProduceImgNameFormat.ProductName,
        ProduceImgNameFormat.BatchNum
    };

    public bool ShowStartProduceSuccessDialog { get; set; } = true;

    public bool ShowCompleteProduceSuccessDialog { get; set; } = true;

    public bool ShowDeliveryProduceSuccessDialog { get; set; } = true;
    
    public List<BasketSort> BasketSortList { get; set; } = new();
    
    // 默认发货面单打印机
    public string DefaultWaybillPrinterName { get; set; } = string.Empty;

    // 分拣完成后自动打印
    public bool AutoPrintAfterPicking { get; set; } = false;
    
    // 即将被打印机打印的印花图临时存放文件夹
    public string PrintTaskDestinationFolder { get; set; } = string.Empty;
    
    // 主界面 列表条目 选中后是否弹出 帮助小提示
    public bool ShowDashboardSelectionHelp { get; set; } = true;
    
    public PrintTaskConfig PrintTaskConfig { get; set; } = new();
}


public class PrintTaskConfig : ObservableObject
{
    private OutputFormat _outputFormat = OutputFormat.Png;
    public OutputFormat OutputFormat
    {
        get => _outputFormat;
        set => SetProperty(ref _outputFormat, value);
    }

    private LayoutOption _layoutOption = LayoutOption.Automatic;
    public LayoutOption LayoutOption
    {
        get => _layoutOption;
        set => SetProperty(ref _layoutOption, value);
    }
    
    // 打印机宽度 毫米
    private int _machinePrintWidthMm = 600;
    public int MachinePrintWidthMm
    {
        get => _machinePrintWidthMm;
        set => SetProperty(ref _machinePrintWidthMm, value);
    }
    
    // 打印机安全边缘 (左右各 ? 毫米)
    private int _machinePrintSafeEdgeMm = 10;
    public int MachinePrintSafeEdgeMm
    {
        get => _machinePrintSafeEdgeMm;
        set => SetProperty(ref _machinePrintSafeEdgeMm, value);
    }

    // 印花图安全间距( ? 毫米) 用于裁剪
    private int _printImgPaddingMm = 10;
    public int PrintImgPaddingMm
    {
        get => _printImgPaddingMm;
        set => SetProperty(ref _printImgPaddingMm, value);
    }
    
    // 打印机打印精度
    private int _machineDpi = 300;
    public int MachineDpi
    {
        get => _machineDpi;
        set => SetProperty(ref _machineDpi, value);
    }
    
    // 白墨烫画专色通道内缩量
    private int _whiteInkBleedPx = 2;
    public int WhiteInkBleedPx
    {
        get => _whiteInkBleedPx;
        set => SetProperty(ref _whiteInkBleedPx, value);
    }
    
    // 喷涂白墨涂层的专色通道 边缘清晰度判定 需要高于此清晰度的边缘才喷涂白墨专色通道 [0-255]
    private int _whiteInkEdgeStrength = 190;
    public int WhiteInkEdgeStrength
    {
        get => _whiteInkEdgeStrength;
        set => SetProperty(ref _whiteInkEdgeStrength, value);
    }
    
    // 白墨烫画专色通道内缩量 安全像素 比这个安全像素还窄的图像不会被内缩
    private int _whiteInkBleedSafePx = 2;
    public int WhiteInkBleedSafePx
    {
        get => _whiteInkBleedSafePx;
        set => SetProperty(ref _whiteInkBleedSafePx, value);
    }
    
    // 打印跟踪信息 默认打印信息
    private bool _isOrderTrack = true;
    public bool IsOrderTrack
    {
        get => _isOrderTrack;
        set => SetProperty(ref _isOrderTrack, value);
    }
    
    // 跟踪单内容设置
    private OrderTrackConfig _orderTrackConfig = new OrderTrackConfig();
    public OrderTrackConfig OrderTrackConfig
    {
        get => _orderTrackConfig;
        set => SetProperty(ref _orderTrackConfig, value);
    }
    
    
    
    public bool IsNeedLayout()
    {
        return this.LayoutOption == LayoutOption.Automatic;
    }

    public bool IsCymk()
    {
        return this.OutputFormat == OutputFormat.TifWithSpotColor || this.OutputFormat == OutputFormat.TifCymk;
    }
}

public enum OutputFormat
{
    Png, // .png(RGBA)
    Jpg, // .jpg(RGB)
    TifCymk, // .tif(CYMK)
    TifWithSpotColor // .tif(CYMK+专色通道)
}

public enum LayoutOption
{
    Automatic, // 自动排版
    Manual // 不排版(后续手动排版)
}

public class OrderTrackConfig
{
    //信息条 默认宽度自动 布局 [靠左 居中 靠右]
    
    // 信息条默认高度 39毫米
    public decimal HeightMm { get; set; } = 39;
    
    // 二维码外框宽度 默认2毫米
    public decimal QrCodeBorderMm { get; set; } = 2;
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
                // TODO 这些写入 没有保留旧配置
                Save(AppSetting);
            }
            if (AppSetting.ComputerCpuThreads != Environment.ProcessorCount)
            {
                //如果配置内 CPU核心数与电脑实际核心数不一致， 则配置成此电脑的核心数 (防止软件被复制到其他电脑, CPU核心数变化 软件无法充分利用CPU核心)
                appSetting.ComputerCpuThreads = Environment.ProcessorCount;
                Save(appSetting);
            }
            AppSetting = appSetting;
        }
        else
        {
            // 初始化软件配置
            AppSetting = new AppSetting();
            Save(AppSetting);
        }

        if (!Directory.Exists(AppSetting.PrintedPatternFilePath))
        {
            // 生产稿件存储目录不存在 则创建一个 (可能软件被移动目录了)
            Directory.CreateDirectory(AppSetting.PrintedPatternFilePath);
        }
    }

    // 6. 提供一个公共的、静态的保存方法
    public static void Save<T>(T localConfig)
    {
        FileHelper.WriteToJsonFile(localConfig);
    }
}