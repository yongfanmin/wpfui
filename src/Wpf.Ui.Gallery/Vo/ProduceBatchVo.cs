// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using CommunityToolkit.Mvvm;
using Wpf.Ui.Gallery.Constant; // <-- [核心修复在这里] 添加这一行

namespace Wpf.Ui.Gallery.Vo;

public partial class ProduceBatchVo : ObservableObject
{
    // 批次号
    [ObservableProperty]
    private string _produceBatchNum;

    // 批次下被授权生产产品
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProduceBatchItemProcess))] // 当它变化时，也通知 ProduceBatchItemNum 更新
    private int _avlProduceBatchItemCount;
    
    // 项批次总数
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DataDownloadProgress))]
    [NotifyPropertyChangedFor(nameof(ImgDownloadProgress))]
    [NotifyPropertyChangedFor(nameof(PiecePrintProgress))]
    [NotifyPropertyChangedFor(nameof(LayoutCreateProgress))]
    [NotifyPropertyChangedFor(nameof(ProduceBatchItemProcess))] // 当总数变化时，所有进度条都需要更新
    private int _produceBatchItemCount;
    
    public string ProduceBatchItemProcess
    {
        get
        {
            if (AvlProduceBatchItemCount > 0 && AvlProduceBatchItemCount >= ProduceBatchItemCount)
            {
                return $"{ProduceBatchItemCount}";
            }
            return $"{AvlProduceBatchItemCount} / {ProduceBatchItemCount} 仅部分可生产";
        }
    }

    // 生产数据下载数量
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(DataDownloadProgress))]
    private int _dataDownloadCount;

    private readonly string tips = "已加载";
    
    public string DataDownloadProgress
    {
        get
        {
            if (ProduceBatchItemCount > 0 && DataDownloadCount >= ProduceBatchItemCount)
            {
                return tips;
            }
            return $"{DataDownloadCount} / {ProduceBatchItemCount}";
        }
    }
    
    
    //图片下载数量
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImgDownloadProgress))] // 添加通知
    private int _imgDownloadCount;
    
    public string ImgDownloadProgress
    {
        get
        {
            if (ProduceBatchItemCount > 0 && ImgDownloadCount >= ProduceBatchItemCount)
            {
                return tips;
            }
            return $"{ImgDownloadCount} / {ProduceBatchItemCount}";
        }
    }
    
    
    //裁片印花完成数量
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PiecePrintProgress))] // 添加通知
    private int _piecePrintCount;
    
    public string PiecePrintProgress
    {
        get
        {
            if (ProduceBatchItemCount > 0 && PiecePrintCount >= ProduceBatchItemCount)
            {
                return tips;
            }
            return $"{PiecePrintCount} / {ProduceBatchItemCount}";
        }
    }
    
    //生产排版完成数量
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LayoutCreateProgress))] // 添加通知
    private int _layoutCreateCount;
    
    public string LayoutCreateProgress
    {
        get
        {
            if (ProduceBatchItemCount > 0 && LayoutCreateCount >= ProduceBatchItemCount)
            {
                return tips;
            }
            return $"{LayoutCreateCount} / {ProduceBatchItemCount}";
        }
    }
    
    // 批次状态
    [ObservableProperty]
    private ProduceBatchStatus _produceBatchStatus;
    
    [ObservableProperty]
    private DateTime _factoryGetTime;
}