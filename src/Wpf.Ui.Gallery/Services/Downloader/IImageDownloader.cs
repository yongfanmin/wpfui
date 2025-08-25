// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Net.Http;
using Wpf.Ui.Gallery.Dto.CreateImg;

namespace Wpf.Ui.Gallery.Services.Downloader;

public interface IImageDownloader
{
    Task<LocalImgInfo?> DownloadImageAsync(
        string imageUrl,
        string directoryPath,
        string fileName,
        CancellationToken cancellationToken = default);
}