// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using NetVips;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Dto.Machine;
using Wpf.Ui.Gallery.Services.Creator;
using Wpf.Ui.Gallery.Utils;
using Wpf.Ui.Gallery.ViewModels.Pages;
using Image = NetVips.Image;

namespace Wpf.Ui.Gallery.Views.Pages;

public partial class DashboardPage : INavigableView<DashboardViewModel>
{
    public DashboardViewModel ViewModel { get; }

    private readonly IImageCreator _imageCreator;
    

    public DashboardPage(DashboardViewModel viewModel, IImageCreator imageCreator)
    {
        ViewModel = viewModel;
        DataContext = this;
        _imageCreator = imageCreator;

        InitializeComponent();

        ViewModel.PageLoadedCommand.Execute(null);
    }
    
    private void DataGridRow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not DataGridRow { ContextMenu: { } } row) return;

        /*var menuItem = row.ContextMenu.Items.OfType<MenuItem>().FirstOrDefault(x => x.Tag as string == "CreatePrintTask" || x.Tag as string == "ResetProduction");
        if (menuItem == null) return;*/
        
        // If the right-clicked row is not in the current selection,
        // clear the selection and select only the right-clicked row.
        if (!ProductBatchDataGrid.SelectedItems.Contains(row.DataContext))
        {
            ProductBatchDataGrid.SelectedItems.Clear();
            ProductBatchDataGrid.SelectedItems.Add(row.DataContext);
        }
        foreach (var menuItem in row.ContextMenu.Items.OfType<MenuItem>())
        {
            menuItem.CommandParameter = ProductBatchDataGrid.SelectedItems;
        }
        // menuItem.CommandParameter = ProductBatchDataGrid.SelectedItems;
    }

    // 创建生产排版图 
    public void createProduceLayoutImg(ProduceImgInfo produceImgInfo)
    {
        using (var canvas = _imageCreator.CreateImageFromPhysicalSize(produceImgInfo.RollOfFabric.WidthMm,
                   decimal.ToDouble(produceImgInfo.LayoutClothInfo.HeightMm),
                   produceImgInfo.MachineConfig.Dpi,
                   ImgSupportFormat.Png,
                   backgroundColor: new double[] { 255, 255, 255, 0 })) // 透明 RGBA
        {
            Image currentResult = canvas; // 使用一个变量来持有流水线的当前结果
            try
            {
                // --- 步骤 2: 按顺序处理和叠加每个裁片 ---
                foreach (PatternPieceLayout layout in produceImgInfo.PatternPieceLayoutList)
                {
                    // 将每个裁片的加载和变换都包裹在 using 块中
                    using (Image pieceRaw = Image.NewFromFile(layout.PatternPieceProduceLocalImgUrl,
                               access: Enums.Access.Random))
                        //裁片的色彩信息可能丢失 手动指定色彩空间为SRGB    
                        //using (Image piece = pieceRaw.Colourspace(Enums.Interpretation.Scrgb))
                    using (Image rotatedPiece = pieceRaw.Rotate(decimal.ToDouble(layout.Rotation)))
                    {
                        // a. Composite 创建一个全新的 Image 结果
                        Image newResult = currentResult.Composite(
                            rotatedPiece,
                            Enums.BlendMode.Over, // Over 是标准的Alpha叠加，Atop可能不是您想要的
                            x: ImageHelper.ConvertMmToPixels(layout.TranslateX, produceImgInfo.MachineConfig.Dpi),
                            y: ImageHelper.ConvertMmToPixels(layout.TranslateY, produceImgInfo.MachineConfig.Dpi)
                        );

                        // b. 释放上一个中间结果 (如果它不是最初的画布)
                        if (currentResult != canvas)
                        {
                            currentResult.Dispose();
                        }

                        // c. 将引用指向新结果
                        currentResult = newResult;
                    }
                }

                // --- 步骤 3: 保存最终结果 ---
                // 此时，currentResult 就是包含了所有叠加裁片的最终图像
                _imageCreator.SaveImageForProduction(
                    currentResult,
                    produceImgInfo.SaveLocalInfo.LocalPath + produceImgInfo.SaveLocalInfo.Name, // 您的完整路径
                    produceImgInfo.SaveLocalInfo.ImgFormat // 保存设定的图片格式
                );
            }
            finally
            {
                // --- 步骤 4: 确保最后一个中间结果被释放 ---
                // 如果至少进行了一次叠加，currentResult将不再是canvas
                if (currentResult != null && currentResult != canvas)
                {
                    currentResult.Dispose();
                }
            }
        }
    }
}