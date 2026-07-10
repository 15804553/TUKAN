using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Chomik.Core;

namespace Chomik.App.Converters;

public sealed class DateValidityBrushConverter : IValueConverter
{
    private static readonly Brush ValidBrush = CreateFrozenBrush(0xC8, 0xE6, 0xC9);
    private static readonly Brush WarningBrush = CreateFrozenBrush(0xFF, 0xE0, 0xB2);
    private static readonly Brush ExpiredBrush = CreateFrozenBrush(0xFF, 0xCD, 0xD2);
    private static readonly Brush NoneBrush = Brushes.Transparent;

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        DateTime? date = value switch
        {
            DateTime dt => dt,
            null => null,
            _ => null
        };

        return DateValidityEvaluator.Evaluate(date) switch
        {
            DateValidityStatus.Valid => ValidBrush,
            DateValidityStatus.Warning => WarningBrush,
            DateValidityStatus.Expired => ExpiredBrush,
            _ => NoneBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
