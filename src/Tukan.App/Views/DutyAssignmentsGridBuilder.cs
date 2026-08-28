using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BOBER.App.Helpers;
using BOBER.App.ViewModels;
using Tukan.App.ViewModels;

namespace Tukan.App.Views;

internal static class DutyAssignmentsGridBuilder
{
    public const string UwagiColumnHeader = "Uwagi";
    private const double UwagiWidth = 440;

    private static readonly Brush HeaderForeground = UrlopPlanPalette.TitleForegroundBrush;
    private static readonly Brush CellForeground = UrlopPlanPalette.ForegroundBrush;

    public static void BuildColumns(DataGrid grid, int year, int month, IReadOnlyCollection<int> workDays)
    {
        grid.Columns.Clear();

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Lp.",
            Binding = new Binding(nameof(DutyAssignmentsRowViewModel.Numer)),
            Width = new DataGridLength(48),
            MinWidth = 40,
            IsReadOnly = true,
            ElementStyle = CreateCenteredTextStyle(fontWeight: FontWeights.SemiBold)
        });

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Imię i nazwisko",
            Binding = new Binding(nameof(DutyAssignmentsRowViewModel.ImieNazwisko)),
            Width = new DataGridLength(220),
            MinWidth = 180,
            IsReadOnly = true,
            ElementStyle = CreateNameStyle()
        });

        grid.FrozenColumnCount = 2;

        foreach (var day in workDays.OrderBy(day => day))
        {
            var header = DayHeaderViewModel.Create(year, month, day);
            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = header,
                HeaderTemplate = CreateDayHeaderTemplate(header),
                CellTemplate = CreateDayCellTemplate(day),
                Width = new DataGridLength(64),
                MinWidth = 56,
                IsReadOnly = true,
                CanUserResize = false
            });
        }

        var uwagiColumn = CreateUwagiColumn();
        grid.Columns.Add(uwagiColumn);
        uwagiColumn.DisplayIndex = grid.Columns.Count - 1;
    }

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
        textFactory.SetValue(TextBlock.ForegroundProperty, HeaderForeground);
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
            new Binding(nameof(DutyAssignmentsRowViewModel.UwagaMiesieczna)));

        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetBinding(TextBlock.TextProperty,
            new Binding(nameof(DutyAssignmentsRowViewModel.UwagaMiesieczna)));
        textFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
        textFactory.SetValue(TextBlock.ForegroundProperty, CellForeground);
        textFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        textFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        textFactory.SetValue(TextBlock.MaxHeightProperty, 48.0);
        textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

        borderFactory.AppendChild(textFactory);
        template.VisualTree = borderFactory;
        return template;
    }

    private static Style CreateCenteredTextStyle(FontWeight? fontWeight = null)
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(4)));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CellForeground));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
        style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
        style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));

        if (fontWeight is FontWeight value)
        {
            style.Setters.Add(new Setter(TextBlock.FontWeightProperty, value));
        }

        return style;
    }

    private static Style CreateNameStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(4)));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CellForeground));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Left));
        style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
        style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
        return style;
    }

    private static DataTemplate CreateDayHeaderTemplate(DayHeaderViewModel header)
    {
        var template = new DataTemplate();

        var panel = new FrameworkElementFactory(typeof(StackPanel));
        panel.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        panel.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);

        var dayNumber = new FrameworkElementFactory(typeof(TextBlock));
        dayNumber.SetValue(TextBlock.TextProperty, header.Day.ToString());
        dayNumber.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        dayNumber.SetValue(TextBlock.FontSizeProperty, 13.0);
        dayNumber.SetValue(TextBlock.ForegroundProperty, HeaderForeground);
        dayNumber.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);

        var dayName = new FrameworkElementFactory(typeof(TextBlock));
        dayName.SetValue(TextBlock.TextProperty, header.DayName);
        dayName.SetValue(TextBlock.FontSizeProperty, 10.0);
        dayName.SetValue(TextBlock.ForegroundProperty, HeaderForeground);
        dayName.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);

        panel.AppendChild(dayNumber);
        panel.AppendChild(dayName);
        template.VisualTree = panel;
        return template;
    }

    private static DataTemplate CreateDayCellTemplate(int day)
    {
        var template = new DataTemplate();

        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.PaddingProperty, new Thickness(2));
        border.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        border.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);

        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding($"[{day}]"));
        text.SetValue(TextBlock.ForegroundProperty, CellForeground);
        text.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        text.SetValue(TextBlock.FontSizeProperty, 12.0);
        text.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        text.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        text.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        text.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);

        border.AppendChild(text);
        template.VisualTree = border;
        return template;
    }
}
