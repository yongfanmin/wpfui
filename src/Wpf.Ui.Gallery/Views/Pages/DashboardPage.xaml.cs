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

    // 单裁片效果图合成 ,  一个产品的效果图 需要由多个裁片叠加 最后再叠加上AO高光图得出最终产品效果图
    private void Test1(object sender, RoutedEventArgs e)
    {
        // --- 1. 定义文件路径 ---
        string uvMapPath = "C:\\Users\\gongw\\Desktop\\UVmap.png";
        string patternPath = "C:\\Users\\gongw\\Desktop\\PatternPrint.png";
        string outputPath = "C:\\Users\\gongw\\Desktop\\show2.png";
        Console.WriteLine("开始执行 UV 纹理映射...");

        // --- 新增: 定义印花图的变换参数 ---
        double offsetX = 80.0;    // X轴向右平移 xx 像素
        double offsetY = 0.0;    // Y轴向下平移 xx 像素
        double rotate = 0;     // 顺时针旋转 xx 度
        double scaleX = 1;      // X轴放大到 xx% 1=100%等于原图大小
        double scaleY = 1;      // Y轴缩小到 xx%

        Console.WriteLine("开始执行带仿射变换的 UV 纹理映射...");

        try
        {
            // --- 2. 加载图像 ---
            using var uvMap = Image.NewFromFile(uvMapPath);
            using var pattern = Image.NewFromFile(patternPath, access: Enums.Access.Sequential);

            Console.WriteLine("图像加载成功。");

            if (!uvMap.HasAlpha())
            {
                Console.WriteLine("错误：UVmap.png 必须包含 Alpha 通道。");
                return;
            }

            // --- 3. 准备位移计算所需的数据 (这部分与之前完全相同) ---
            var distortedU = uvMap[0];
            var distortedV = uvMap[1];
            var alphaChannel = uvMap[uvMap.Bands - 1];

            Console.WriteLine("正在将坐标数据转换为高精度浮点数格式...");
            using var distortedUFloat = distortedU.Cast(Enums.BandFormat.Float);
            using var distortedVFloat = distortedV.Cast(Enums.BandFormat.Float);

            Console.WriteLine("正在内存中生成高精度基准坐标图...");
            using var baseCoordinates = Image.Xyz(uvMap.Width, uvMap.Height);
            var baseX = baseCoordinates[0];
            var baseY = baseCoordinates[1];

            var xScale = (double)(uvMap.Width - 1) / 255.0;
            var yScale = (double)(uvMap.Height - 1) / 255.0;
            
            var distortedX = distortedUFloat.Linear(new[] { xScale }, new[] { 0.0 });
            var distortedY = distortedVFloat.Linear(new[] { yScale }, new[] { 0.0 });

            Console.WriteLine("正在计算高精度像素位移场...");
            var displacementX = distortedX - baseX;
            var displacementY = distortedY - baseY;
            var initialXMap = baseX + displacementX;
            var initialYMap = baseY + displacementY;

            // --- 核心改造: 对采样坐标图进行逆向仿射变换 ---
            Console.WriteLine("正在对采样坐标应用逆向仿射变换...");
            
            // 计算印花图的中心点，这是旋转和缩放的轴心
            var patternCenterX = (double)pattern.Width / 2.0;
            var patternCenterY = (double)pattern.Height / 2.0;

            // C# Math 函数使用弧度，所以需要转换角度
            // 我们执行的是逆向变换，所以旋转角度要取负值
            var angleRad = -rotate * Math.PI / 180.0;
            var cosAngle = Math.Cos(angleRad);
            var sinAngle = Math.Sin(angleRad);

            // 步骤 1: 逆向平移 (Translate)
            var translatedX = initialXMap - offsetX;
            var translatedY = initialYMap - offsetY;

            // 步骤 2: 逆向旋转 (Rotate)
            // a. 将坐标系原点移动到印花图中心
            var centeredX = translatedX - patternCenterX;
            var centeredY = translatedY - patternCenterY;
            // b. 应用标准2D旋转公式
            var rotatedX = centeredX * cosAngle - centeredY * sinAngle;
            var rotatedY = centeredX * sinAngle + centeredY * cosAngle;
            // c. 将坐标系原点移回左上角
            var uncenteredX = rotatedX + patternCenterX;
            var uncenteredY = rotatedY + patternCenterY;

            // 步骤 3: 逆向缩放 (Scale)
            // a. 将坐标系原点移动到印花图中心
            centeredX = uncenteredX - patternCenterX;
            centeredY = uncenteredY - patternCenterY;
            // b. 应用逆向缩放 (除以 scale)
            var scaledX = centeredX / scaleX;
            var scaledY = centeredY / scaleY;
            // c. 将坐标系原点移回左上角
            var finalXMap = scaledX + patternCenterX;
            var finalYMap = scaledY + patternCenterY;

            // --- 改造结束 ---

            // 将经过完整逆向变换的坐标图合并成最终的 indexMap
            using var indexMap = finalXMap.Bandjoin(finalYMap);

            // --- 7. 执行核心扭曲操作 (这部分与之前完全相同) ---
            Console.WriteLine("正在根据最终坐标扭曲印花图 (使用 Bicubic 插值)...");
            using var interpolator = Interpolate.NewFromName("bicubic");
            using var warpedColorImage = pattern.Mapim(indexMap, interpolate: interpolator);

            // --- 8. 应用最终形状 ---
            using var warpedRgb = warpedColorImage.ExtractBand(0, 3);
            using var finalImageWithAlpha = warpedRgb.Bandjoin(alphaChannel);
            Console.WriteLine("Alpha 形状已应用。");
            
            // --- 9. 后期锐化 ---
            Console.WriteLine("正在进行后期锐化以增强细节...");
            using var finalImage = finalImageWithAlpha.Sharpen(sigma: 0.75, m2: 2.0);
            
            // --- 10. 保存最终结果 ---
            finalImage.WriteToFile(outputPath);

            Console.WriteLine($"处理成功！高品质效果图已保存到: {outputPath}");
        }
        catch (VipsException ex)
        {
            Console.WriteLine($"处理时发生 NetVips 错误: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生未知错误: {ex.Message}");
        }
    }

    // 单件排版方法
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
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\ProduceImgLayout\\";
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
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-batch-2582242560001\\Pattern-print\\后片.png";

        patternPieceLayouts.Add(patternPiece1);

        PatternPieceLayout patternPiece2 = new PatternPieceLayout();
        patternPiece2.Rotation = 60;
        patternPiece2.ViewId = 2;
        patternPiece2.TranslateX = -320;
        patternPiece2.TranslateY = 385;
        patternPiece2.PatternPieceProduceLocalImgUrl =
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-batch-2582242560001\\Pattern-print\\右袖.png";

        patternPieceLayouts.Add(patternPiece2);

        PatternPieceLayout patternPiece3 = new PatternPieceLayout();
        patternPiece3.Rotation = -5;
        patternPiece3.ViewId = 3;
        patternPiece3.TranslateX = -38;
        patternPiece3.TranslateY = 530;
        patternPiece3.PatternPieceProduceLocalImgUrl =
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-batch-2582242560001\\Pattern-print\\左袖.png";

        patternPieceLayouts.Add(patternPiece3);

        PatternPieceLayout patternPiece4 = new PatternPieceLayout();
        patternPiece4.Rotation = 13;
        patternPiece4.ViewId = 4;
        patternPiece4.TranslateX = -50;
        patternPiece4.TranslateY = 385;
        patternPiece4.PatternPieceProduceLocalImgUrl =
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-batch-2582242560001\\Pattern-print\\领子.png";

        patternPieceLayouts.Add(patternPiece4);

        PatternPieceLayout patternPiece5 = new PatternPieceLayout();
        patternPiece5.Rotation = 90;
        patternPiece5.ViewId = 5;
        patternPiece5.TranslateX = -70;
        patternPiece5.TranslateY = 220;
        patternPiece5.PatternPieceProduceLocalImgUrl =
            "D:\\POD\\exeSoftware\\wpfui-main\\src\\Wpf.Ui.Gallery\\bin\\Debug\\net9.0-windows10.0.26100.0\\Cache\\Factory-1053\\Order-batch-2582242560001\\Pattern-print\\前片.png";

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