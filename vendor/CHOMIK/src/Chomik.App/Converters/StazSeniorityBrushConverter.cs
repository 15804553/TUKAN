using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Chomik.Core;

namespace Chomik.App.Converters;

public sealed class StazSeniorityBrushConverter : IValueConverter
{
    private static readonly Brush UnderTenBrush = CreateFrozenBrush(0xC8, 0xE6, 0xC9);
    private static readonly Brush TenToNineteenBrush = CreateFrozenBrush(0xFF, 0xE0, 0xB2);
    private static readonly Brush TwentyOrMoreBrush = CreateFrozenBrush(0xFF, 0xCD, 0xD2);
    private static readonly Brush NoneBrush = Brushes.Transparent;

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int? years = value switch
        {
            int i => i,
            null => null,
            _ => null
        };

        return StazSeniorityEvaluator.Evaluate(years) switch
        {
            StazSeniorityStatus.UnderTenYears => UnderTenBrush,
            StazSeniorityStatus.TenToNineteenYears => TenToNineteenBrush,
            StazSeniorityStatus.TwentyYearsOrMore => TwentyOrMoreBrush,
            _ => NoneBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
