using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using BOBER.App.ViewModels;
using BOBER.Core.Constants;

namespace BOBER.App.Helpers;

public static class UrlopPlanGridBuilder
{
    private static readonly SolidColorBrush SummaryRowBg = new(Color.FromRgb(0xA8, 0x98, 0x68));
    private static readonly SolidColorBrush NormalFg = new(Color.FromRgb(0x2C, 0x28, 0x18));
    private static readonly SolidColorBrush DayNumberFg = Brushes.White;
    private static readonly SolidColorBrush Transparent = new(Colors.Transparent);
    private static readonly SolidColorBrush OverLimitBg = new(Color.FromRgb(0xC4, 0x2B, 0x1C));
    private static readonly SolidColorBrush FullLimitBg = new(Color.FromRgb(0x4A, 0x8C, 0x2A));
    private static readonly SolidColorBrush WarningBg = new(Color.FromRgb(0xE8, 0x94, 0x3A));
    private static readonly SolidColorBrush StatusFg = Brushes.White;
    private static readonly SolidColorBrush SelectionBorderBrush = new(Color.FromRgb(0x1A, 0x16, 0x0A));

    private static Brush GetThemeBrush(string key) =>
        Application.Current?.TryFindResource(key) as Brush ?? Brushes.Black;

    private static Brush SelectionHighlightBg => GetThemeBrush("AccentBrush");

    public static void BuildColumns(
        DataGrid grid,
        int year,
        int month,
        int maxUrlopowNaSluzbie,
        IReadOnlySet<int>? workDays = null,
        Brush? dzienSluzbyBrush = null)
    {
        grid.Columns.Clear();

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Lp.",
            Binding = new Binding(nameof(UrlopPlanRowViewModel.Numer)),
            Width = new DataGridLength(46),
            IsReadOnly = true,
            ElementStyle = CreateNumerCellStyle()
        });

        grid.Columns.Add(new DataGridTemplateColumn
        {
            Header = "Nazwisko i imię",
            Width = new DataGridLength(180),
            IsReadOnly = true,
            CellTemplate = CreateNameCellTemplate()
        });

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "W",
            Binding = new Binding(nameof(UrlopPlanRowViewModel.WypoczynkowyCount)),
            Width = new DataGridLength(44),
            IsReadOnly = true,
            ElementStyle = CreateYearCountCellStyle(
                nameof(UrlopPlanRowViewModel.WypoczynkowyCount),
                UrlopPlanInstructions.LimitWypoczynkowy)
        });

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "D",
            Binding = new Binding(nameof(UrlopPlanRowViewModel.DodatkowyCount)),
            Width = new DataGridLength(44),
            IsReadOnly = true,
            ElementStyle = CreateYearCountCellStyle(
                nameof(UrlopPlanRowViewModel.DodatkowyCount),
                UrlopPlanInstructions.LimitDodatkowy)
        });

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "R",
            Binding = new Binding(nameof(UrlopPlanRowViewModel.RodzicielskiCount)),
            Width = new DataGridLength(44),
            IsReadOnly = true,
            ElementStyle = CreateYearCountCellStyle(
                nameof(UrlopPlanRowViewModel.RodzicielskiCount),
                UrlopPlanInstructions.LimitRodzicielski)
        });

        grid.FrozenColumnCount = 2;

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var workDayBrush = dzienSluzbyBrush ?? new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0x00));
        for (var day = 1; day <= daysInMonth; day++)
        {
            var header = DayHeaderViewModel.Create(year, month, day);
            var isWorkDay = workDays?.Contains(day) == true;

            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = header,
                HeaderTemplate = CreateDayHeaderTemplate(header, isWorkDay, workDayBrush),
                CellTemplate = CreateDayCellTemplate(day, maxUrlopowNaSluzbie),
                Width = new DataGridLength(32),
                MinWidth = 28,
                IsReadOnly = true,
                CanUserResize = false
            });
        }
    }

    private static DataTemplate CreateDayHeaderTemplate(DayHeaderViewModel header, bool isWorkDay, Brush workDayBrush)
    {
        var template = new DataTemplate();
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        borderFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(2, 1, 2, 1));

        if (isWorkDay)
            borderFactory.SetValue(Border.BackgroundProperty, workDayBrush);

        var dayNumFactory = new FrameworkElementFactory(typeof(TextBlock));
        dayNumFactory.SetValue(TextBlock.TextProperty, header.Day.ToString());
        dayNumFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        dayNumFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
        dayNumFactory.SetValue(TextBlock.ForegroundProperty, DayNumberFg);
        dayNumFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        dayNumFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        borderFactory.AppendChild(dayNumFactory);
        template.VisualTree = borderFactory;
        return template;
    }

    private static DataTemplate CreateDayCellTemplate(int day, int maxUrlopowNaSluzbie)
    {
        var template = new DataTemplate();
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        borderFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        borderFactory.SetBinding(
            Border.BackgroundProperty,
            CreateSummaryRowAppearanceBinding(day, maxUrlopowNaSluzbie, background: true));

        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetBinding(TextBlock.TextProperty, new Binding($"[{day}]")
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        textFactory.SetBinding(
            TextBlock.ForegroundProperty,
            CreateSummaryRowAppearanceBinding(day, maxUrlopowNaSluzbie, background: false));
        textFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        textFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
        textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

        borderFactory.AppendChild(textFactory);
        template.VisualTree = borderFactory;
        return template;
    }

    private static MultiBinding CreateSummaryRowAppearanceBinding(int day, int maxUrlopow, bool background)
    {
        var binding = new MultiBinding
        {
            Converter = SummaryRowDayAppearanceConverter.Instance,
            ConverterParameter = (maxUrlopow, background)
        };
        binding.Bindings.Add(new Binding($"[{day}]")
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        binding.Bindings.Add(new Binding(nameof(UrlopPlanRowViewModel.IsSummaryRow)));
        return binding;
    }

    private static Style CreateYearCountCellStyle(string countPropertyName, int limit)
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
        style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(TextBlock.BackgroundProperty, CreateYearCountAppearanceBinding(countPropertyName, limit, background: true)));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CreateYearCountAppearanceBinding(countPropertyName, limit, background: false)));
        return style;
    }

    private static MultiBinding CreateYearCountAppearanceBinding(string countPropertyName, int limit, bool background)
    {
        var binding = new MultiBinding
        {
            Converter = YearCountAppearanceConverter.Instance,
            ConverterParameter = (limit, background)
        };
        binding.Bindings.Add(new Binding(countPropertyName));
        binding.Bindings.Add(new Binding(nameof(UrlopPlanRowViewModel.IsSummaryRow)));
        return binding;
    }

    private static DataTemplate CreateNameCellTemplate()
    {
        var template = new DataTemplate();
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(4, 2, 4, 2));
        borderFactory.SetValue(Border.MarginProperty, new Thickness(1));
        borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);

        var borderStyle = new Style(typeof(Border));
        borderStyle.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(0)));
        var selectionHighlight = new DataTrigger
        {
            Binding = new Binding(nameof(UrlopPlanRowViewModel.IsSelectionHighlight)),
            Value = true
        };
        selectionHighlight.Setters.Add(new Setter(Border.BackgroundProperty, SelectionHighlightBg));
        selectionHighlight.Setters.Add(new Setter(Border.BorderBrushProperty, SelectionBorderBrush));
        selectionHighlight.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2.5)));
        borderStyle.Triggers.Add(selectionHighlight);
        borderFactory.SetValue(FrameworkElement.StyleProperty, borderStyle);

        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetBinding(TextBlock.TextProperty,
            new Binding(nameof(UrlopPlanRowViewModel.ImieNazwisko)));
        textFactory.SetValue(TextBlock.ForegroundProperty, NormalFg);
        textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

        var textStyle = new Style(typeof(TextBlock));
        var selectionFg = new DataTrigger
        {
            Binding = new Binding(nameof(UrlopPlanRowViewModel.IsSelectionHighlight)),
            Value = true
        };
        selectionFg.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
        textStyle.Triggers.Add(selectionFg);
        textFactory.SetValue(FrameworkElement.StyleProperty, textStyle);

        borderFactory.AppendChild(textFactory);
        template.VisualTree = borderFactory;
        return template;
    }

    private static Style CreateNumerCellStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, NormalFg));
        style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
        style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
        return style;
    }

    private sealed class YearCountAppearanceConverter : IMultiValueConverter
    {
        public static readonly YearCountAppearanceConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var (limit, useBackground) = parameter is ValueTuple<int, bool> tuple
                ? tuple
                : (UrlopPlanInstructions.LimitWypoczynkowy, true);

            var isSummary = values.Length > 1 && values[1] is true;
            if (isSummary)
                return useBackground ? SummaryRowBg : NormalFg;

            var count = values.Length > 0 && values[0] is int c ? c : 0;
            if (count > limit)
                return useBackground ? OverLimitBg : StatusFg;
            if (count == limit)
                return useBackground ? FullLimitBg : StatusFg;
            return useBackground ? SummaryRowBg : NormalFg;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class SummaryRowDayAppearanceConverter : IMultiValueConverter
    {
        public static readonly SummaryRowDayAppearanceConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var (maxUrlopow, useBackground) = parameter is ValueTuple<int, bool> tuple
                ? tuple
                : (UrlopPlanInstructions.DefaultMaxUrlopowNaSluzbie, true);

            var isSummary = values.Length > 1 && values[1] is true;
            if (!isSummary)
                return useBackground ? Transparent : NormalFg;

            var cell = values.Length > 0 ? values[0]?.ToString() ?? string.Empty : string.Empty;
            if (string.IsNullOrEmpty(cell))
                return useBackground ? Transparent : NormalFg;

            if (!int.TryParse(cell, out var count))
                return useBackground ? SummaryRowBg : NormalFg;

            if (count >= maxUrlopow)
                return useBackground ? OverLimitBg : StatusFg;
            if (count == maxUrlopow - 1)
                return useBackground ? WarningBg : StatusFg;
            if (count == maxUrlopow - 2)
                return useBackground ? FullLimitBg : StatusFg;
            return useBackground ? SummaryRowBg : NormalFg;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
