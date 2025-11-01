#target photoshop

// ===================================================================================
// 脚本说明:
// 此脚本将打开一个指定路径的 TIFF 文件，
// 查找名为 "Alpha 2" 的通道，将其转换为名为 "W1" 的红色专色通道，
// 然后保存并覆盖原始文件，最后关闭该文档。
// 整个过程无任何用户交互弹窗。
// ===================================================================================
function processImage(filePath, outputFolderPath, newFileName) {
	// 设置 Photoshop 为非交互模式，禁止所有对话框弹出
	app.displayDialogs = DialogModes.NO;

	// 1. 定义要处理的图片文件的完整路径
	// 注意：在 JavaScript 中，路径中的反斜杠 \ 需要写成 \\ 或者直接使用正斜杠 /

	// 创建一个文件对象
	var imageFile = new File(filePath);

	// 声明一个变量用于引用文档
	var doc;

	// 使用 try-catch 块来捕获可能发生的任何错误（如文件不存在、通道不存在等）
	try {
		// 2. 检查文件是否存在，如果存在则打开它
		if (imageFile.exists) {
			doc = app.open(imageFile);

			// 3. 按名称查找目标通道 "Alpha 2"
			var targetChannel = doc.channels.getByName("Alpha 2");

			// 4. 将通道类型更改为专色通道
			targetChannel.kind = ChannelType.SPOTCOLOR;

			// 5. 更改通道的名称为 "W1"
			targetChannel.name = "W1";

			// 6. 设置专色通道的颜色和实色度 (纯红色)
			var spotColor = new SolidColor();
			spotColor.rgb.red = 255;
			spotColor.rgb.green = 0;
			spotColor.rgb.blue = 0;
			
			targetChannel.color = spotColor;
			targetChannel.opacity = 100; // 实色度

			// 7. 准备 TIFF 保存选项，以确保专色通道被正确保存
			var tiffSaveOptions = new TiffSaveOptions();
			tiffSaveOptions.alphaChannels = true;      // 关键：必须为 true 才能保存专色通道
			tiffSaveOptions.layers = true;             // 如果文件有图层，保留图层
			tiffSaveOptions.imageCompression = TIFFEncoding.NONE; // 无损保存

			// 8. 保存并覆盖原始文件
			// 使用 saveAs 并指定原始路径 (doc.fullName) 来实现带选项的覆盖保存
			doc.saveAs(doc.fullName, tiffSaveOptions, true);

			// 9. 关闭文档，不再次提示保存
			doc.close(SaveOptions.DONOTSAVECHANGES);
			
			// (已移除成功提示的 alert)

		} else {
			// 如果文件不存在，就在 Adobe ExtendScript Toolkit 的控制台中显示错误
			// 如果不是在调试环境中运行，这一步将不会有任何可见提示
			$.writeln("错误：文件不存在于指定路径: " + filePath);
		}

	} catch (e) {
		// 如果在处理过程中发生任何错误（例如找不到通道），
		// 也在控制台中记录错误，并确保如果文档已打开则将其关闭而不保存。
		$.writeln("脚本执行失败: " + e);
		if (doc) {
			doc.close(SaveOptions.DONOTSAVECHANGES);
		}
	}
}

// ===================================================================================
// 这是脚本的入口点。
// 它检查 C# 是否传入了参数。'arguments' 是一个预定义的数组，包含了所有传入的参数。
// ===================================================================================
if (arguments.length > 2) {
    var inputPath = arguments[0];
    var outputPath = arguments[1];
    var fileName = arguments[2];
    
    // 调用主函数，并将函数的返回值作为整个脚本的最终结果返回给 C#
    processImage(inputPath, outputPath, fileName);
}