using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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

        var uwagiColumn = CreateUwagiColumn();
        grid.Columns.Add(uwagiColumn);
        uwagiColumn.DisplayIndex = grid.Columns.Count - 1;
    }

    public const string UwagiColumnHeader = "Uwagi";
    private const double UwagiWidth = 440;

    private static DataGridTemplateColumn CreateUwagiColumn()
    {
        return new DataGridTemplateColumn
        {
            Header = UwagiColumnHeader,
            HeaderTemplate = CreateUwagiHeaderTemplate(),
            CellTemplate = CreateUwagiCellTemplate(),
            Width = new DataGridLength(UwagiWidth),
            MinWidth = 160,
            IsReadOnly = true,
            CanUserResize = true
        };
    }

    private static DataTemplate CreateUwagiHeaderTemplate()
    {
        var template = new DataTemplate();
        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetValue(TextBlock.TextProperty, UwagiColumnHeader);
        textFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        textFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
        textFactory.SetValue(TextBlock.ForegroundProperty, DayNumberFg);
        textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        textFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        textFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 2, 4, 2));
        template.VisualTree = textFactory;
        return template;
    }

    private static DataTemplate CreateUwagiCellTemplate()
    {
        var template = new DataTemplate();
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(4, 2, 4, 2));
        borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        borderFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        borderFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        borderFactory.SetValue(FrameworkElement.CursorProperty, Cursors.IBeam);
        borderFactory.SetBinding(FrameworkElement.ToolTipProperty,
            new Binding(nameof(GrafikRowViewModel.UwagaMiesieczna)));

        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetBinding(TextBlock.TextProperty,
            new Binding(nameof(GrafikRowViewModel.UwagaMiesieczna)));
        textFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
        textFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        textFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        textFactory.SetValue(TextBlock.MaxHeightProperty, 48.0);
        textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        textFactory.SetBinding(TextBlock.ForegroundProperty,
            new Binding(nameof(GrafikRowViewModel.RowForeground)));

        var textStyle = new Style(typeof(TextBlock));
        var hideForSummary = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsSummaryRow)),
            Value = true
        };
        hideForSummary.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        textStyle.Triggers.Add(hideForSummary);

        var hideForNotes = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsNotesRow)),
            Value = true
        };
        hideForNotes.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        textStyle.Triggers.Add(hideForNotes);
        textFactory.SetValue(FrameworkElement.StyleProperty, textStyle);

        borderFactory.AppendChild(textFactory);

        var borderStyle = new Style(typeof(Border));
        borderStyle.Setters.Add(new Setter(UIElement.IsHitTestVisibleProperty, true));
        var disableSummary = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsSummaryRow)),
            Value = true
        };
        disableSummary.Setters.Add(new Setter(UIElement.IsHitTestVisibleProperty, false));
        disableSummary.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Arrow));
        borderStyle.Triggers.Add(disableSummary);

        var disableNotes = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsNotesRow)),
            Value = true
        };
        disableNotes.Setters.Add(new Setter(UIElement.IsHitTestVisibleProperty, false));
        disableNotes.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Arrow));
        borderStyle.Triggers.Add(disableNotes);
        borderFactory.SetValue(FrameworkElement.StyleProperty, borderStyle);

        template.VisualTree = borderFactory;
        return template;
    }

    private const double SummaryLineFontSize = 12.0;
    private const double SummaryLineHeight = 15.0;

    private static readonly SolidColorBrush SelectionBorderBrush =
        new(Color.FromRgb(0x1A, 0x16, 0x0A));

    private static Brush SelectionHighlightBg =>
        GetThemeBrush("AccentBrush");

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

        // Podświetlenie osoby przy zaznaczonej komórce dnia w tym wierszu.
        var selectionHighlight = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsSelectionHighlight)),
            Value = true
        };
        selectionHighlight.Setters.Add(new Setter(Border.BackgroundProperty, SelectionHighlightBg));
        selectionHighlight.Setters.Add(new Setter(Border.BorderBrushProperty, SelectionBorderBrush));
        selectionHighlight.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2.5)));
        borderStyle.Triggers.Add(selectionHighlight);
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

        var selectionFg = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsSelectionHighlight)),
            Value = true
        };
        selectionFg.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
        selectionFg.Setters.Add(new Setter(TextBlock.ForegroundProperty, NormalDayFg));
        textStyle.Triggers.Add(selectionFg);

        var summaryText = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsSummaryRow)),
            Value = true
        };
        summaryText.Setters.Add(new Setter(TextBlock.FontSizeProperty, SummaryLineFontSize));
        summaryText.Setters.Add(new Setter(TextBlock.LineHeightProperty, SummaryLineHeight));
        summaryText.Setters.Add(new Setter(TextBlock.LineStackingStrategyProperty, LineStackingStrategy.BlockLineHeight));
        summaryText.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top));
        summaryText.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(2, 0, 2, 0)));
        textStyle.Triggers.Add(summaryText);
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
        normalStyle.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(0)));
        normalStyle.Setters.Add(new Setter(Border.BorderBrushProperty, SelectionBorderBrush));
        normalStyle.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(0)));

        var hideNormalForSummary = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsSummaryRow)),
            Value = true
        };
        hideNormalForSummary.Setters.Add(new Setter(FrameworkElement.VisibilityProperty, Visibility.Collapsed));
        normalStyle.Triggers.Add(hideNormalForSummary);

        var hideNormalForNotes = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsNotesRow)),
            Value = true
        };
        hideNormalForNotes.Setters.Add(new Setter(FrameworkElement.VisibilityProperty, Visibility.Collapsed));
        normalStyle.Triggers.Add(hideNormalForNotes);

        // Widoczne zaznaczenie także na żółtym WS (tło wpisu zasłania AccentBrush komórki).
        var selected = new DataTrigger
        {
            Binding = new Binding(nameof(DataGridCell.IsSelected))
            {
                RelativeSource = new RelativeSource(
                    RelativeSourceMode.FindAncestor, typeof(DataGridCell), 1)
            },
            Value = true
        };
        selected.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2.5)));
        selected.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(0)));
        normalStyle.Triggers.Add(selected);

        normalBorderFactory.SetValue(FrameworkElement.StyleProperty, normalStyle);
        rootFactory.AppendChild(normalBorderFactory);

        var summaryPanelFactory = CreateSummaryCellPanel(day);
        rootFactory.AppendChild(summaryPanelFactory);

        var notesPanelFactory = CreateNotesCellPanel(day);
        rootFactory.AppendChild(notesPanelFactory);

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

        var contentGrid = new FrameworkElementFactory(typeof(Grid));

        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        // Typ wpisu + flaga planu → U / Uₚ / Uᵣ (urlop przeniesiony z planu urlopów).
        var tekstBinding = new MultiBinding { Converter = WpisTekstConverter.Instance };
        tekstBinding.Bindings.Add(new Binding($"[{day}]"));
        tekstBinding.Bindings.Add(new Binding($"{nameof(GrafikRowViewModel.FromUrlopPlan)}[{day}]"));
        textFactory.SetBinding(TextBlock.TextProperty, tekstBinding);
        textFactory.SetValue(TextBlock.FontSizeProperty, 15.0);
        textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        textFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 2, 2, 2));

        var textStyle = new Style(typeof(TextBlock));
        textStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, NormalDayFg));
        textStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));

        var oddalStrike = new DataTrigger
        {
            Binding = new Binding($"[{day}]") { Converter = OddalFlagConverter.Instance },
            Value = true
        };
        oddalStrike.Setters.Add(new Setter(TextBlock.TextDecorationsProperty, TextDecorations.Strikethrough));
        oddalStrike.Setters.Add(new Setter(TextBlock.FontSizeProperty, 16.0));
        textStyle.Triggers.Add(oddalStrike);
        textFactory.SetValue(FrameworkElement.StyleProperty, textStyle);
        contentGrid.AppendChild(textFactory);

        // Znaczek „.” / „?” — grubszy, ale nadal mniejszy niż główne litery U/D/S.
        var markFactory = new FrameworkElementFactory(typeof(TextBlock));
        markFactory.SetBinding(TextBlock.TextProperty,
            new Binding($"[{day}]") { Converter = WpisZnaczekConverter.Instance });
        markFactory.SetValue(TextBlock.FontSizeProperty, 14.0);
        markFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.ExtraBold);
        markFactory.SetValue(TextBlock.ForegroundProperty, NormalDayFg);
        markFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
        markFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top);
        markFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 2, 0));
        markFactory.SetBinding(UIElement.VisibilityProperty,
            new Binding($"[{day}]") { Converter = ZnaczekVisibilityConverter.Instance });
        contentGrid.AppendChild(markFactory);

        borderFactory.AppendChild(contentGrid);
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
        panelFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);
        panelFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 2, 2, 1));

        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetBinding(TextBlock.TextProperty, new Binding($"[{day}]"));
        textFactory.SetValue(TextBlock.ForegroundProperty, NormalDayFg);
        textFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        textFactory.SetValue(TextBlock.FontSizeProperty, SummaryLineFontSize);
        textFactory.SetValue(TextBlock.LineHeightProperty, SummaryLineHeight);
        textFactory.SetValue(TextBlock.LineStackingStrategyProperty, LineStackingStrategy.BlockLineHeight);
        textFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top);
        textFactory.SetValue(TextBlock.PaddingProperty, new Thickness(0));
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

    private static readonly SolidColorBrush NoteIconBrush =
        new(Color.FromRgb(0x2E, 0xA8, 0x44));

    private static readonly SolidColorBrush KalendarzIconBrush =
        new(Color.FromRgb(0x1E, 0x88, 0xE5));

    private static FrameworkElementFactory CreateNotesCellPanel(int day)
    {
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        borderFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(0, 2, 0, 2));

        var iconsPanel = new FrameworkElementFactory(typeof(StackPanel));
        iconsPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        iconsPanel.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        iconsPanel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        // Zielona „N” — notatka grafiku służb.
        iconsPanel.AppendChild(CreateRoundNoteIcon(
            day,
            bindingPath: $"[{day}]",
            background: NoteIconBrush,
            letter: "N"));

        // Niebieska „i” — notatka kalendarza od DCA.
        iconsPanel.AppendChild(CreateRoundNoteIcon(
            day,
            bindingPath: $"{nameof(GrafikRowViewModel.KalendarzNotes)}[{day}]",
            background: KalendarzIconBrush,
            letter: "i",
            margin: new Thickness(2, 0, 0, 0)));

        borderFactory.AppendChild(iconsPanel);

        var notesStyle = new Style(typeof(Border));
        notesStyle.Setters.Add(new Setter(FrameworkElement.VisibilityProperty, Visibility.Collapsed));
        var showNotes = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsNotesRow)),
            Value = true
        };
        showNotes.Setters.Add(new Setter(FrameworkElement.VisibilityProperty, Visibility.Visible));
        notesStyle.Triggers.Add(showNotes);
        borderFactory.SetValue(FrameworkElement.StyleProperty, notesStyle);

        return borderFactory;
    }

    private static FrameworkElementFactory CreateRoundNoteIcon(
        int day,
        string bindingPath,
        Brush background,
        string letter,
        Thickness? margin = null)
    {
        var iconBorder = new FrameworkElementFactory(typeof(Border));
        iconBorder.SetValue(Border.WidthProperty, 18.0);
        iconBorder.SetValue(Border.HeightProperty, 18.0);
        iconBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
        iconBorder.SetValue(Border.BackgroundProperty, background);
        iconBorder.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        iconBorder.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        iconBorder.SetValue(FrameworkElement.CursorProperty, Cursors.Help);
        if (margin is Thickness m)
            iconBorder.SetValue(FrameworkElement.MarginProperty, m);

        iconBorder.SetBinding(FrameworkElement.ToolTipProperty, new Binding(bindingPath));
        // Opacity zamiast Visibility — pewniejsze odświeżanie w DataGrid po Items.Refresh().
        iconBorder.SetBinding(UIElement.OpacityProperty,
            new Binding(bindingPath) { Converter = NonEmptyOpacityConverter.Instance });
        iconBorder.SetBinding(UIElement.IsHitTestVisibleProperty,
            new Binding(bindingPath) { Converter = NonEmptyBoolConverter.Instance });

        var letterBlock = new FrameworkElementFactory(typeof(TextBlock));
        letterBlock.SetValue(TextBlock.TextProperty, letter);
        letterBlock.SetValue(TextBlock.ForegroundProperty, Brushes.White);
        letterBlock.SetValue(TextBlock.FontSizeProperty, 10.0);
        letterBlock.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        letterBlock.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        letterBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        iconBorder.AppendChild(letterBlock);

        return iconBorder;
    }

    private sealed class NonEmptyOpacityConverter : IValueConverter
    {
        public static readonly NonEmptyOpacityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            string.IsNullOrWhiteSpace(value?.ToString()) ? 0.0 : 1.0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class NonEmptyBoolConverter : IValueConverter
    {
        public static readonly NonEmptyBoolConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            !string.IsNullOrWhiteSpace(value?.ToString());

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class WpisTloConverter(GrafikCellColors colors) : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var typ = value?.ToString();

            if (GrafikWpisTypy.MaTloWolnejSluzby(typ))
                return colors.WsTlo;

            var kod = GrafikWpisTypy.BazowyKod(typ);

            // Del/S: własny kolor → on; „brak” → żółte tylko gdy zachowano tło WS (sufiks *), inaczej bez wypełnienia.
            if (kod.Equals(GrafikWpisTypy.Delegacja, StringComparison.OrdinalIgnoreCase))
            {
                if (colors.DelTlo is not null)
                    return colors.DelTlo;
                return GrafikWpisTypy.MaZachowaneTloWs(typ) ? colors.WsTlo : Brushes.Transparent;
            }

            if (kod.Equals(GrafikWpisTypy.Szkolenie, StringComparison.OrdinalIgnoreCase))
            {
                if (colors.STlo is not null)
                    return colors.STlo;
                return GrafikWpisTypy.MaZachowaneTloWs(typ) ? colors.WsTlo : Brushes.Transparent;
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    /// <summary>values[0]=TypWpisu, values[1]=FromUrlopPlan → tekst główny (U / Uₚ / Uᵣ).</summary>
    private sealed class WpisTekstConverter : IMultiValueConverter
    {
        public static readonly WpisTekstConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var typ = values.Length > 0 ? values[0]?.ToString() : null;
            var fromPlan = values.Length > 1 && values[1] is true;
            return GrafikWpisTypy.TekstGlowny(typ, fromPlan);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class WpisZnaczekConverter : IValueConverter
    {
        public static readonly WpisZnaczekConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            GrafikWpisTypy.TekstZnaczka(value?.ToString());

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class ZnaczekVisibilityConverter : IValueConverter
    {
        public static readonly ZnaczekVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            string.IsNullOrEmpty(GrafikWpisTypy.TekstZnaczka(value?.ToString()))
                ? Visibility.Collapsed
                : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class OddalFlagConverter : IValueConverter
    {
        public static readonly OddalFlagConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var typ = value?.ToString();
            if (!GrafikWpisTypy.MaOddal(typ))
                return false;

            var bazowy = GrafikWpisTypy.BazowyKod(typ);
            return !bazowy.Equals(GrafikWpisTypy.WolnaSluzba, StringComparison.OrdinalIgnoreCase);
        }

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

        var highlight = new DataTrigger
        {
            Binding = new Binding(nameof(GrafikRowViewModel.IsSelectionHighlight)),
            Value = true
        };
        highlight.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
        highlight.Setters.Add(new Setter(TextBlock.BackgroundProperty, SelectionHighlightBg));
        style.Triggers.Add(highlight);
        return style;
    }
}
