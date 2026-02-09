using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace Bloomdo.UI.Converters
{
    public class BoolToThicknessConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isToday = value is bool b && b;
            return isToday ? new Avalonia.Thickness(3) : new Avalonia.Thickness(1);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class BoolToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isToday = value is bool b && b;
            return isToday ? new SolidColorBrush(Color.Parse("#2E7D32")) : new SolidColorBrush(Color.Parse("#4A4A4A"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
