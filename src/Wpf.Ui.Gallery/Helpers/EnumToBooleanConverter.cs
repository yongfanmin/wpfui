// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Appearance;
using Wpf.Ui.Gallery.Constant;

namespace Wpf.Ui.Gallery.Helpers;

internal sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string enumString)
        {
            throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
        }

        if (value.GetType().Equals(typeof(Wpf.Ui.Appearance.ApplicationTheme)))
        {
            if (!Enum.IsDefined(typeof(Wpf.Ui.Appearance.ApplicationTheme), value))
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterValueMustBeAnEnum");
            }

            var enumValue = Enum.Parse(typeof(Wpf.Ui.Appearance.ApplicationTheme), enumString);

            return enumValue.Equals(value);
        }
        else if (value.GetType().Equals(typeof(ProduceImgLayoutFolderClassify)))
        {
            if (!Enum.IsDefined(typeof(ProduceImgLayoutFolderClassify), value))
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterValueMustBeAnEnum");
            }

            var enumValue = Enum.Parse(typeof(ProduceImgLayoutFolderClassify), enumString);

            return enumValue.Equals(value);
        }
        else
        {
            return null;
        }
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        
        /*
        if (parameter is not string enumString)
        {
            throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
        }

        return Enum.Parse(typeof(Wpf.Ui.Appearance.ApplicationTheme), enumString);
        */
        
        if (value == null || parameter == null)
            return null;

        bool useValue = (bool)value;
        string targetValue = parameter.ToString();

        if (useValue)
        {
            return Enum.Parse(targetType, targetValue);
        }

        return Binding.DoNothing;
    }
}
