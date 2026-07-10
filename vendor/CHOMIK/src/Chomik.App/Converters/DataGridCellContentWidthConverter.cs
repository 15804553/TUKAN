using System.Globalization;
using System.Windows.Data;

namespace Chomik.App.Converters;

/// <summary>
/// Ogranicza szerokość TextBlock w komórce DataGrid, aby działało zawijanie tekstu.
/// </summary>
public sealed class DataGridCellContentWidthConverter : IValueConverter
{
    public double PaddingSubtract { get; set; } = 12;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width && width > PaddingSubtract)
        {
            return width - PaddingSubtract;
        }

        return double.NaN;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
