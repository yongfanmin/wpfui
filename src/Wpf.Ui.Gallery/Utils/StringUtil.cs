// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Utils;

public static class StringUtil
{
    // 扫描到的编码 是一个项批号
    public static bool IsBatchNo(string scanCode)
    {
        if (string.IsNullOrEmpty(scanCode))
        {
            return false;
        }
        return scanCode.All(char.IsDigit);
    }

    // 扫描到的编码  是一个 子项号
    public static bool IsItem(string scanCode)
    {
        if (string.IsNullOrEmpty(scanCode))
        {
            return false;
        }
        if (scanCode.Contains("-"))
        {
            return scanCode.Split("-").ToList().All(item => item.All(char.IsDigit));
        }
        return true;
    }
}