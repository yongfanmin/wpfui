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

        if (scanCode.All(char.IsDigit) && scanCode.Length == 13)
        {
            return true;
        }
        return false;
    }
    
    public static bool IsOrderNo(string scanCode)
    {
        if (string.IsNullOrEmpty(scanCode))
        {
            return false;
        }

        if (scanCode.All(char.IsDigit) && scanCode.Length > 15)
        {
            return true;
        }
        return false;
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
        return false;
    }
    
    public static string EasyWatchNo(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= 4)
        {
            return input;
        }

        // 使用 StringBuilder 以获得最佳性能
        var sb = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            // 在每4个字符的边界处（但不是在字符串的开头）添加一个空格
            if (i > 0 && i % 4 == 0)
            {
                sb.Append(' ');
            }
            
            sb.Append(input[i]);
        }

        return sb.ToString();
    }
}