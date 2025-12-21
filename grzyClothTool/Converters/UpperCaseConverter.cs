using System;
using System.Globalization;
using System.Windows.Data;

namespace grzyClothTool.Converters;

[ValueConversion(typeof(string), typeof(string))]
public class UpperCaseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return stringValue.ToUpperInvariant();
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}
