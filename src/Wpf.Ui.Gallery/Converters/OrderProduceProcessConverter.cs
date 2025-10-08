// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Converters;

public class OrderProduceProcessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 1. 检查输入值是否可以被转换为 int
        if (value is not IConvertible convertibleValue)
        {
            return "未知状态"; // 如果值不是数字类型，返回默认文本
        }
            
        try
        {
            int intValue = convertibleValue.ToInt32(CultureInfo.InvariantCulture);

            // 2. 检查这个整数值是否是 OrderProduceProcess 枚举的一个有效成员
            if (Enum.IsDefined(typeof(ProduceBatchItemProcess), intValue))
            {
                // 3. 如果是，先将其转换为枚举类型
                ProduceBatchItemProcess status = (ProduceBatchItemProcess)intValue;
                    
                // 4. 然后，获取该枚举成员的名称字符串 (即中文名称)
                return status.ToString();
            }
                
            // 5. 如果整数值在枚举中未定义（例如，后端返回了一个新的状态码5）
            return $"未知代码 ({intValue})";
        }
        catch (Exception)
        {
            // 如果转换整数失败
            return "无效状态";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 从中文转回数字的逻辑通常不需要，保持不变
        throw new NotImplementedException();
    }
}