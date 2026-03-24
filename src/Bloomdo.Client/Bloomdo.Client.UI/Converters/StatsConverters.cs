using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Bloomdo.Client.UI.Converters;

public class FirstLetterConverter : IValueConverter
{
    public static readonly FirstLetterConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && s.Length > 0)
            return s[..1].ToUpperInvariant();
        return "?";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StreakStartCornerRadiusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? new CornerRadius(8, 0, 0, 8) : new CornerRadius(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StreakEndCornerRadiusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? new CornerRadius(0, 8, 8, 0) : new CornerRadius(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StreakBrushConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isStreakDay = values.Count > 0 && values[0] is true;
        var isFreezeDay = values.Count > 1 && values[1] is true;

        if (!isStreakDay) return Brushes.Transparent;
        return isFreezeDay
            ? new SolidColorBrush(Color.Parse("#5042A5F5")) // blue for freeze
            : new SolidColorBrush(Color.Parse("#50FF9800")); // orange for goal met
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StreakCornerRadiusConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isStreakDay = values.Count > 0 && values[0] is true;
        var isStreakStart = values.Count > 1 && values[1] is true;
        var isStreakEnd = values.Count > 2 && values[2] is true;

        if (!isStreakDay) return new CornerRadius(0);

        if (isStreakStart && isStreakEnd) return new CornerRadius(14);
        if (isStreakStart) return new CornerRadius(14, 0, 0, 14);
        if (isStreakEnd) return new CornerRadius(0, 14, 14, 0);
        return new CornerRadius(0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StreakDotBrushConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isToday = values.Count > 0 && values[0] is true;
        var isFreezeDay = values.Count > 1 && values[1] is true;

        if (!isToday) return Brushes.Transparent;
        return isFreezeDay
            ? new SolidColorBrush(Color.Parse("#42A5F5"))
            : new SolidColorBrush(Color.Parse("#FF9800"));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StreakMarginConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isStreakDay = values.Count > 0 && values[0] is true;
        var isStreakStart = values.Count > 1 && values[1] is true;
        var isStreakEnd = values.Count > 2 && values[2] is true;

        if (!isStreakDay) return new Thickness(0);

        if (isStreakStart && isStreakEnd) return new Thickness(2, 4, 2, 4);
        if (isStreakStart) return new Thickness(2, 4, 0, 4);
        if (isStreakEnd) return new Thickness(0, 4, 2, 4);
        return new Thickness(0, 4, 0, 4);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BlockTypeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string color
            ? new SolidColorBrush(Color.Parse(color))
            : new SolidColorBrush(Color.Parse("#666666"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class SelectedDayBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.Parse("#42A5F5"))
            : Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BarHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            var maxHeight = 120.0;
            if (parameter is string paramStr && double.TryParse(paramStr, out var customMax))
                maxHeight = customMax;

            var height = Math.Max(4, percent / 100 * maxHeight);
            return height;
        }
        return 4.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class TodayBarBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.Parse("#42A5F5"))
            : new SolidColorBrush(Color.Parse("#7E57C2"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class GoalMetBarBrushConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isToday = values.Count > 0 && values[0] is true;
        var goalMet = values.Count > 1 && values[1] is true;

        if (isToday)
            return new SolidColorBrush(Color.Parse("#42A5F5"));
        if (goalMet)
            return new SolidColorBrush(Color.Parse("#4CAF50"));
        return new SolidColorBrush(Color.Parse("#7E57C2"));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ImprovingBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.Parse("#4CAF50"))
            : new SolidColorBrush(Color.Parse("#FF5722"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ImprovingBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.Parse("#1A4CAF50"))
            : new SolidColorBrush(Color.Parse("#1AFF5722"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PercentToWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            // Return GridLength for column width based on percentage
            return new Avalonia.Controls.GridLength(Math.Max(percent, 0.01), Avalonia.Controls.GridUnitType.Star);
        }
        return new Avalonia.Controls.GridLength(0.01, Avalonia.Controls.GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PercentToRemainingWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            var remaining = 100 - percent;
            return new Avalonia.Controls.GridLength(Math.Max(remaining, 0.01), Avalonia.Controls.GridUnitType.Star);
        }
        return new Avalonia.Controls.GridLength(99.99, Avalonia.Controls.GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class AppColorConverter : IValueConverter
{
    private static readonly string[] Colors = 
    [
        "#7E57C2", "#42A5F5", "#66BB6A", "#FFA726", "#EF5350",
        "#26C6DA", "#AB47BC", "#5C6BC0", "#29B6F6", "#26A69A"
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string name && !string.IsNullOrEmpty(name))
        {
            var index = Math.Abs(name.GetHashCode()) % Colors.Length;
            return new SolidColorBrush(Color.Parse(Colors[index]));
        }
        return new SolidColorBrush(Color.Parse(Colors[0]));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PercentToActualWidthConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && 
            values[0] is double percent && 
            values[1] is double parentWidth)
        {
            return Math.Max(0, parentWidth * percent / 100);
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
