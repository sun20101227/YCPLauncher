using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using Color = System.Windows.Media.Color;

namespace YCPLauncher.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value == null || string.IsNullOrEmpty(value.ToString()) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToVisibilityInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null && !string.IsNullOrEmpty(value.ToString()) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Ping int → green/yellow/red brush. Handles 999 as offline.</summary>
public class PingToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Green  = new(Color.FromRgb(0x22, 0xC5, 0x5E));
    private static readonly SolidColorBrush Yellow = new(Color.FromRgb(0xF5, 0x9E, 0x0B));
    private static readonly SolidColorBrush Red    = new(Color.FromRgb(0xEF, 0x44, 0x44));
    private static readonly SolidColorBrush Grey   = new(Color.FromRgb(0x4A, 0x55, 0x68));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int ping)
        {
            if (ping >= 999) return Grey;
            if (ping < 50)   return Green;
            if (ping <= 120) return Yellow;
            return Red;
        }
        return Grey;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Ping → label text (shows "超时" for 999)</summary>
public class PingToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int p && p >= 999 ? "超时" : $"{value} ms";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>int == 0 → Collapsed (hide when count is zero)</summary>
public class ZeroToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int i && i == 0 ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>int == 0 → Visible (show empty state)</summary>
public class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int i && i == 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
