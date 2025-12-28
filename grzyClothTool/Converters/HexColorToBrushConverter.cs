using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace grzyClothTool.Converters
{
    /// <summary>
    /// Converts a hex color string (e.g., "#FF5733") to a SolidColorBrush
    /// </summary>
    public class HexColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Brushes.Transparent;

            string hexColor = value.ToString();
            
            if (string.IsNullOrWhiteSpace(hexColor))
                return Brushes.Transparent;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                return new SolidColorBrush(color);
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            return null;
        }
    }
}
