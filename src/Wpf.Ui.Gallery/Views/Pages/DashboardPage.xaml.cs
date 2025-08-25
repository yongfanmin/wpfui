// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using NetVips;
using Wpf.Ui.Gallery.Constant;
using Wpf.Ui.Gallery.Dto.CreateImg;
using Wpf.Ui.Gallery.Services.Creator;
using Wpf.Ui.Gallery.Services.Downloader;
using Wpf.Ui.Gallery.Utils;
using Wpf.Ui.Gallery.ViewModels.Pages;

namespace Wpf.Ui.Gallery.Views.Pages;

public partial class DashboardPage : INavigableView<DashboardViewModel>
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();

        ViewModel.PageLoadedCommand.Execute(null);
    }
    
    //private readonly IImageCreator _imageCreator;
    

    private void Test(object sender, RoutedEventArgs e)
    {
        /*RollOfFabric rollOfFabric = new RollOfFabric();
        rollOfFabric.WidthMm = 2 * 1000; //2米布料卷
        rollOfFabric.CurrentMaxLengthMm = 30 * 1000; //当前剩余30米
        int targetDpi = 100;
        using (var jpegCanvas = _imageCreator.CreateImageFromPhysicalSize(rollOfFabric.WidthMm, 1000, 100,
                   ImgSupportFormat.Jpeg,
                   backgroundColor: new double[] { 255, 255, 255 })) // 白色 RGB
        {
            PatternPieceLayout patternPiece1 = new PatternPieceLayout();
            patternPiece1.Rotation = 0;
            patternPiece1.ViewId = 1;
            patternPiece1.TranslateX = 0;
            patternPiece1.TranslateY = 0;
            patternPiece1.PatternPieceProduceLocalImgUrl =
                "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-GWH5256519437737\\Pattern-print\\后片.png";
            PatternPieceLayout patternPiece2 = new PatternPieceLayout();
            patternPiece1.Rotation = 0;
            patternPiece1.ViewId = 2;
            patternPiece1.TranslateX = 0;
            patternPiece1.TranslateY = 0;
            patternPiece1.PatternPieceProduceLocalImgUrl =
                "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-GWH5256519437737\\Pattern-print\\右袖.png";
            PatternPieceLayout patternPiece3 = new PatternPieceLayout();
            patternPiece1.Rotation = 0;
            patternPiece1.ViewId = 3;
            patternPiece1.TranslateX = 0;
            patternPiece1.TranslateY = 0;
            patternPiece1.PatternPieceProduceLocalImgUrl =
                "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-GWH5256519437737\\Pattern-print\\左袖.png";
            PatternPieceLayout patternPiece4 = new PatternPieceLayout();
            patternPiece1.Rotation = 0;
            patternPiece1.ViewId = 4;
            patternPiece1.TranslateX = 0;
            patternPiece1.TranslateY = 0;
            patternPiece1.PatternPieceProduceLocalImgUrl =
                "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-GWH5256519437737\\Pattern-print\\领子.png";
            PatternPieceLayout patternPiece5 = new PatternPieceLayout();
            patternPiece1.Rotation = 0;
            patternPiece1.ViewId = 5;
            patternPiece1.TranslateX = 0;
            patternPiece1.TranslateY = 0;
            patternPiece1.PatternPieceProduceLocalImgUrl =
                "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-GWH5256519437737\\Pattern-print\\前片.png";
            Image patternPieceImg = Image.NewFromFile(patternPiece1.PatternPieceProduceLocalImgUrl);
            Image tempCanvas = jpegCanvas.Composite(patternPieceImg, Enums.BlendMode.Atop,
                ImageHelper.ConvertMmToPixels(patternPiece1.TranslateX, targetDpi),
                ImageHelper.ConvertMmToPixels(patternPiece1.TranslateY, targetDpi));
            _imageCreator.SaveImageForProduction(tempCanvas, "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-GWH5256519437737\\result.png", ImgSupportFormat.Jpeg);
            // 4. 保存为 JPEG
            //ImageHelper.SaveImageForProduction(jpegCanvas, "D:/output/piece_mm", ImgSupportFormat.Jpeg);
        }*/
        
        // The user will implement this.
    }
}