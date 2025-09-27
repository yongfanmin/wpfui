// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Helpers;

internal sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string enumString)
        {
            return false;
        }

        if (value == null)
        {
            return false;
        }

        var enumType = value.GetType();

        if (!enumType.IsEnum)
        {
            return false;
        }

        if (!Enum.IsDefined(enumType, value))
        {
            return false;
        }

        var enumValue = Enum.Parse(enumType, enumString);

        return enumValue.Equals(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string enumString)
        {
            return Binding.DoNothing;
        }

        if (value is false)
        {
            return Binding.DoNothing;
        }

        if (string.IsNullOrEmpty(enumString))
        {
            return Binding.DoNothing;
        }

        return Enum.Parse(targetType, enumString);
    }
}