using System.Globalization;

namespace BrainEx.Converters;

/// <summary>int > 0 → true，用于 Badge 的 IsVisible 绑定</summary>
public class IntToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int n && n > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
