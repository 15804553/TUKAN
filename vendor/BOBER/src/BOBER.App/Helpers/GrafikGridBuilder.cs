using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using BOBER.App.ViewModels;
using BOBER.Core.Constants;

namespace BOBER.App.Helpers;

/// <summary>
/// Buduje dynamiczne kolumny DataGrid dla miesiąca grafiku.
/// Dni służby są wyróżnione jaśniejszym nagłówkiem i tłem komórek.
/// </summary>
public static class GrafikGridBuilder
{
    private static readonly SolidColorBrush SummaryRowBg =
        new(Color.FromRgb(0xA8, 0x98, 0x68));

    private static readonly SolidColorBrush NormalDayFg =
        new(Color.FromRgb(0x2C, 0x28, 0x18));

    private static readonly SolidColorBrush DayNumberFg = Brushes.White;

    private static readonly SolidColorBrush WeekdayNameFg = Brushes.White;

    private static Brush GetThemeBrush(string key) =>
        Application.Current?.TryFindResource(key) as Brush ?? Brushes.Black;

    private static Brush GetDayNameBrush(DayHeaderViewModel header) =>
        header.IsSaturday ? GetThemeBrush("SaturdayBrush")
        : header.IsSunday ? GetThemeBrush("SundayBrush")
        : WeekdayNameFg;

    /// <summary>
    /// Buduje kolumny DataGrid. Gdy podano <paramref name="workDays"/>,
    /// tworzone są kolumny wyłącznie dla dni służby.
    /// </summary>
    public static void BuildColumns(
        DataGrid grid,
        int year,
        int month,
        HashSet<int>? workDays = null,
        GrafikCellColors? colors = null)
    {
        grid.Columns.Clear();
        colors ??= new GrafikCellColors();

        var numCol = new DataGridTextColumn
        {
            Header = "Lp.",
            Binding = new Binding(nameof(GrafikRowViewModel.Numer)),
            Width = new DataGridLength(46),
            MinWidth = 38,
            IsReadOnly = true,
            ElementStyle = CreateNumerCellStyle()
        };
        grid.Columns.Add(numCol);

        var nameCol = new DataGridTemplateColumn
        {
            Header = "Imię i Nazwisko",
            Width = new DataGridLength(175),
            MinWidth = 120,
            IsReadOnly = true,
            CellTemplate = CreateNameCellTemplate()
        };
        grid.Columns.Add(nameCol);

        grid.FrozenColumnCount = 2;

        var daysInMonth = DateTime.DaysInMonth(year, month);
        for (var day = 1; day <= daysInMonth; day++)
        {
            if (workDays is not null && !workDays.Contains(day))
                continue;

            var header = DayHeaderViewModel.Create(year, month, day);
            var col = new DataGridTemplateColumn
            {
                Header = header,
                HeaderTemplate = CreateDayHeaderTemplate(header),
                CellTemplate = CreateDayCellTemplate(day, colors),
                Width = new DataGridLength(84),
                MinWidth = 72,
                IsReadOnly = true,
                CanUserResize = false
            };
            grid.Columns.Add(col);
        }
    }

    private static DataTemplate CreateNameCellTemplate()
    {
        var template = new DataTemplate();
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(4, 2, 4, 2));
        borderFactory.SetValue(Border.MarginProperty, new Thickness(1));
        borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        borderFactory.SetBinding(Border.BorderBrushProperty,
            new Binding(nameof(GrafikRowViewModel.NameBorderBrush)));

        var borderStyle = new Style(typeof(Border));
        borderStyle.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(0)));
        var nurekBorder = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsNurek)),
            Value = true
        };
        nurekBorder.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2)));
        borderStyle.Triggers.Add(nurekBorder);
        borderFactory.SetValue(FrameworkElement.StyleProperty, borderStyle);

        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetBinding(TextBlock.TextProperty,
            new Binding(nameof(GrafikRowViewModel.ImieNazwisko)));
        textFactory.SetValue(TextBlock.FontSizeProperty, 15.0);
        textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        textFactory.SetValue(TextBlock.PaddingProperty, new Thickness(2, 2, 2, 2));
        textFactory.SetBinding(TextBlock.ForegroundProperty,
            new Binding(nameof(GrafikRowViewModel.RowForeground)));

        var textStyle = new Style(typeof(TextBlock));
        var nurekFg = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsNurek)),
            Value = true
        };
        nurekFg.Setters.Add(new Setter(TextBlock.ForegroundProperty, NormalDayFg));
        textStyle.Triggers.Add(nurekFg);
        textFactory.SetValue(FrameworkElement.StyleProperty, textStyle);

        borderFactory.AppendChild(textFactory);
        template.VisualTree = borderFactory;
        return template;
    }

    private static DataTemplate CreateDayHeaderTemplate(DayHeaderViewModel header)
    {
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(StackPanel));
        factory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);

        var dayNumFactory = new FrameworkElementFactory(typeof(TextBlock));
        dayNumFactory.SetValue(TextBlock.TextProperty, header.Day.ToString());
        dayNumFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        dayNumFactory.SetValue(TextBlock.FontSizeProperty, 13.0);
        dayNumFactory.SetValue(TextBlock.ForegroundProperty, DayNumberFg);
        dayNumFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);

        var dayNameFactory = new FrameworkElementFactory(typeof(TextBlock));
        dayNameFactory.SetValue(TextBlock.TextProperty, header.DayName);
        dayNameFactory.SetValue(TextBlock.FontSizeProperty, 10.0);
        dayNameFactory.SetValue(TextBlock.ForegroundProperty, GetDayNameBrush(header));
        dayNameFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);

        factory.AppendChild(dayNumFactory);
        factory.AppendChild(dayNameFactory);
        template.VisualTree = factory;
        return template;
    }

    private static DataTemplate CreateDayCellTemplate(int day, GrafikCellColors colors)
    {
        var template = new DataTemplate();
        var rootFactory = new FrameworkElementFactory(typeof(Grid));
        rootFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        rootFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);

        var normalBorderFactory = CreateNormalCellBorder(day, colors);
        var normalStyle = new Style(typeof(Border));
        normalStyle.Setters.Add(new Setter(FrameworkElement.VisibilityProperty, Visibility.Visible));
        var hideNormal = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsSummaryRow)),
            Value = true
        };
        hideNormal.Setters.Add(new Setter(FrameworkElement.VisibilityProperty, Visibility.Collapsed));
        normalStyle.Triggers.Add(hideNormal);
        normalBorderFactory.SetValue(FrameworkElement.StyleProperty, normalStyle);
        rootFactory.AppendChild(normalBorderFactory);

        var summaryPanelFactory = CreateSummaryCellPanel(day);
        rootFactory.AppendChild(summaryPanelFactory);

        template.VisualTree = rootFactory;
        return template;
    }

    private static FrameworkElementFactory CreateNormalCellBorder(int day, GrafikCellColors colors)
    {
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        borderFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        borderFactory.SetBinding(Border.BackgroundProperty, new Binding($"[{day}]")
        {
            Converter = new WpisTloConverter(colors)
        });

        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetBinding(TextBlock.TextProperty,
            new Binding($"[{day}]") { Converter = WsCellTextConverter.Instance });
        textFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        textFactory.SetValue(TextBlock.FontSizeProperty, 15.0);
        textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        textFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 2, 2, 2));

        var textStyle = new Style(typeof(TextBlock));
        textStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, NormalDayFg));
        textFactory.SetValue(FrameworkElement.StyleProperty, textStyle);

        borderFactory.AppendChild(textFactory);
        return borderFactory;
    }

    private static FrameworkElementFactory CreateSummaryCellPanel(int day)
    {
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.BackgroundProperty, SummaryRowBg);
        borderFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        borderFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);

        var panelFactory = new FrameworkElementFactory(typeof(StackPanel));
        panelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        panelFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        panelFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        panelFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 1, 2, 1));

        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetBinding(TextBlock.TextProperty, new Binding($"[{day}]"));
        textFactory.SetValue(TextBlock.ForegroundProperty, NormalDayFg);
        textFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        textFactory.SetValue(TextBlock.FontSizeProperty, 13.0);
        textFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        panelFactory.AppendChild(textFactory);

        borderFactory.AppendChild(panelFactory);

        var summaryStyle = new Style(typeof(Border));
        summaryStyle.Setters.Add(new Setter(FrameworkElement.VisibilityProperty, Visibility.Collapsed));
        var showSummary = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsSummaryRow)),
            Value = true
        };
        showSummary.Setters.Add(new Setter(FrameworkElement.VisibilityProperty, Visibility.Visible));
        summaryStyle.Triggers.Add(showSummary);
        borderFactory.SetValue(FrameworkElement.StyleProperty, summaryStyle);

        return borderFactory;
    }

    private sealed class WpisTloConverter(GrafikCellColors colors) : IValueConverter
    {
        private static readonly SolidColorBrush Transparent = new(Colors.Transparent);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value?.ToString() switch
            {
                "D" => colors.DyzurTlo,
                "WS" => colors.WsTlo,
                "Del" => colors.DyzurTlo,
                _ => Transparent
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class WsCellTextConverter : IValueConverter
    {
        public static readonly WsCellTextConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value?.ToString() == "WS" ? string.Empty : value ?? string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private static Style CreateNumerCellStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(4, 4, 4, 4)));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, NormalDayFg));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 13.0));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
        style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
        return style;
    }
}
