using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Bloomdo.Client.UI.Converters;

/// <summary>
/// Converts a byte[] (PNG image data) to an Avalonia Bitmap for display in Image controls.
/// Returns null when the input is null or empty, so the Image becomes invisible.
/// </summary>
public sealed class BytesToBitmapConverter : IValueConverter
{
    public static readonly BytesToBitmapConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] { Length: > 0 } bytes)
            return null;

        try
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
