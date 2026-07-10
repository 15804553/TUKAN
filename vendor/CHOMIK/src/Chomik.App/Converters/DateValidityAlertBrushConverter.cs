using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Chomik.Core;

namespace Chomik.App.Converters;

public sealed class DateValidityAlertBrushConverter : IValueConverter
{
    private static readonly Brush WarningBrush = CreateFrozen(0xEF, 0x6C, 0x00);
    private static readonly Brush ExpiredBrush = CreateFrozen(0xC6, 0x28, 0x28);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            DateValidityStatus.Warning => WarningBrush,
            DateValidityStatus.Expired => ExpiredBrush,
            _ => Brushes.Transparent
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static SolidColorBrush CreateFrozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
