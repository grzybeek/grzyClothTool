using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace grzyClothTool.Converters;

public class FilePathToShortVersionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string filePath && !string.IsNullOrEmpty(filePath))
        {
            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);

            if (directory != null && fileName != null)
            {
                var parentFolder = Path.GetFileName(directory);
                return $"{parentFolder}/{fileName}";
            }
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}
