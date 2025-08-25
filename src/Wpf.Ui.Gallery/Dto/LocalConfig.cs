// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Dto;

public class LocalConfig
{
    public int TYPE_SET = TypeSet.MANUAL;

    private static class TypeSet
    {
        // 手动排版
        public const int MANUAL = 0;
        // 自动排版 省钱-节约布料(所有裁片按照最密集排版节约布料)
        public const int AUTO_ECONOMIZE = 1;
        // 自动排版 容易生产(同一件衣服尽量一起邻接打印)
        public const int AUTO_EASY_PRODUCE = 2;
    }
}