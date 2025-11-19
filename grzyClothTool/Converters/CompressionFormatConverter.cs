using System;
using System.Globalization;
using System.Windows.Data;

namespace grzyClothTool.Converters;

public class CompressionFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return "N/A";

        string compression = value.ToString();

        return compression switch
        {
            "D3DFMT_DXT1" or "DXT1" => "DXT1",
            "D3DFMT_DXT3" or "DXT3" => "DXT3",
            "D3DFMT_DXT5" or "DXT5" => "DXT5",
            "D3DFMT_A8R8G8B8" or "A8R8G8B8" => "A8R8G8B8",
            "BC1" => "BC1",
            "BC2" => "BC2",
            "BC3" => "BC3",
            "BC4" => "BC4",
            "BC5" => "BC5",
            "BC6" => "BC6",
            "BC7" => "BC7",
            "UNKNOWN" => "UNKNOWN",
            _ => "OTHER"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
