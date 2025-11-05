// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.Converters;


public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        string enumValue = value.ToString();
        string targetValue = parameter.ToString();

        return enumValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return null;

        bool useValue = (bool)value;
        string targetValue = parameter.ToString();

        if (useValue)
            return Enum.Parse(targetType, targetValue);

        return Binding.DoNothing;
    }
}