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
        int length = input.Length;

        for (int i = 0; i < length; i++)
        {
            // 关键逻辑：检查从当前字符到末尾的剩余字符数
            // 如果剩余字符数是4的倍数，并且我们不在字符串的开头，
            // 那么就在当前字符前添加一个空格。
            int remainingChars = length - i;
            if (i > 0 && remainingChars % 4 == 0)
            {
                sb.Append(' ');
            }
        
            sb.Append(input[i]);
        }

        return sb.ToString();
    }

    public static string LastNum4(string input)
    {
        // 检查输入是否为 null 或空字符串
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // 如果字符串长度不足4位，则返回原字符串
        if (input.Length <= 4)
        {
            return input;
        }

        // 计算截取的起始位置
        int startIndex = input.Length - 4;
    
        // 从计算出的起始位置开始截取
        return input.Substring(startIndex);
    }
}