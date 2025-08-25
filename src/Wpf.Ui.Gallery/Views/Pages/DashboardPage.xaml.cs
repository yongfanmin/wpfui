// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using NetVips;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Dto.Machine;
using Wpf.Ui.Gallery.Services.Creator;
using Wpf.Ui.Gallery.Services.Downloader;
using Wpf.Ui.Gallery.Utils;
using Wpf.Ui.Gallery.ViewModels.Pages;

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


    private void Test(object sender, RoutedEventArgs e)
    {
        ProduceImgInfo produceImgInfo = new ProduceImgInfo();

        MachineConfig machineConfig = new MachineConfig();
        machineConfig.Dpi = 150;
        machineConfig.PrintWidthMm = 2000; // 2米宽度料卷


        RollOfFabric rollOfFabric = new RollOfFabric();
        rollOfFabric.WidthMm = 550;
        rollOfFabric.CurrentMaxLengthMm = 30 * 1000; //当前剩余30米

        SaveLocalInfo saveLocalInfo = new SaveLocalInfo();
        saveLocalInfo.LocalPath =
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\ProduceLayoutImg\\";
        saveLocalInfo.ImgFormat = ImgSupportFormat.Png;

        LayoutClothInfo layoutClothInfo = new LayoutClothInfo();
        layoutClothInfo.WidthMm = rollOfFabric.WidthMm;
        layoutClothInfo.HeightMm = 1000;

        List<PatternPieceLayout> patternPieceLayouts = new List<PatternPieceLayout>();
        PatternPieceLayout patternPiece1 = new PatternPieceLayout();
        patternPiece1.Rotation = 90;
        patternPiece1.ViewId = 1;
        patternPiece1.TranslateX = -70;
        patternPiece1.TranslateY = -150;
        patternPiece1.PatternPieceProduceLocalImgUrl =
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-GWH5256519437737\\Pattern-print\\后片.png";

        patternPieceLayouts.Add(patternPiece1);

        PatternPieceLayout patternPiece2 = new PatternPieceLayout();
        patternPiece2.Rotation = 60;
        patternPiece2.ViewId = 2;
        patternPiece2.TranslateX = -320;
        patternPiece2.TranslateY = 385;
        patternPiece2.PatternPieceProduceLocalImgUrl =
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-GWH5256519437737\\Pattern-print\\右袖.png";

        patternPieceLayouts.Add(patternPiece2);

        PatternPieceLayout patternPiece3 = new PatternPieceLayout();
        patternPiece3.Rotation = -5;
        patternPiece3.ViewId = 3;
        patternPiece3.TranslateX = -38;
        patternPiece3.TranslateY = 530;
        patternPiece3.PatternPieceProduceLocalImgUrl =
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-GWH5256519437737\\Pattern-print\\左袖.png";

        patternPieceLayouts.Add(patternPiece3);

        PatternPieceLayout patternPiece4 = new PatternPieceLayout();
        patternPiece4.Rotation = 13;
        patternPiece4.ViewId = 4;
        patternPiece4.TranslateX = -50;
        patternPiece4.TranslateY = 385;
        patternPiece4.PatternPieceProduceLocalImgUrl =
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-GWH5256519437737\\Pattern-print\\领子.png";

        patternPieceLayouts.Add(patternPiece4);

        PatternPieceLayout patternPiece5 = new PatternPieceLayout();
        patternPiece5.Rotation = 90;
        patternPiece5.ViewId = 5;
        patternPiece5.TranslateX = -70;
        patternPiece5.TranslateY = 220;
        patternPiece5.PatternPieceProduceLocalImgUrl =
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-GWH5256519437737\\Pattern-print\\前片.png";

        patternPieceLayouts.Add(patternPiece5);

        produceImgInfo.MachineConfig = machineConfig;
        produceImgInfo.RollOfFabric = rollOfFabric;
        produceImgInfo.PatternPieceLayoutList = patternPieceLayouts;
        produceImgInfo.Layout = ProduceLayout.MANUAL;
        produceImgInfo.SaveLocalInfo = saveLocalInfo;
        produceImgInfo.LayoutClothInfo = layoutClothInfo;

        createProduceLayoutImg(produceImgInfo);
        // The user will implement this.
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