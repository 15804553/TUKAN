using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BOBER.App.Controllers;
using BOBER.App.Views.Chrome;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using MediaColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace BOBER.App.Views;

public partial class KalendarzView : UserControl
{
    private static readonly string[] DayHeaders = ["Pn", "Wt", "Śr", "Cz", "Pt", "So", "Nd"];

    private KalendarzController? _controller;
    private int _year;
    private int _month;
    private bool _canEdit;
    private string _userLogin = string.Empty;
    private int? _shiftNumber;
    private bool _isLoading;
    private IReadOnlyDictionary<int, string> _kolory = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, int> _workingShifts = new Dictionary<int, int>();
    private List<KalendarzWpis> _wpisy = [];

    public bool IsEmbedded { get; set; }

    public KalendarzView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void Initialize(
        KalendarzController controller,
        bool canEdit,
        string userLogin,
        int? shiftNumber)
    {
        _controller = controller;
        _canEdit = canEdit;
        _userLogin = userLogin;
        _shiftNumber = shiftNumber;
        _year = controller.DefaultYear;
        _month = controller.DefaultMonth;

        HintTextBlock.Text = canEdit
            ? "Kliknij dzień, aby dodać lub edytować notatkę."
            : "Niebieskie „i” oznacza notatkę od DCA — kliknij, aby odczytać.";
        FooterTextBlock.Text = canEdit
            ? "Status odczytu: po potwierdzeniu przez zmianę pojawia się informacja przy notatce."
            : "Po odczytaniu notatki naciśnij „Przeczytałem”, aby potwierdzić zapoznanie się z treścią.";

        PopulateYearCombo();
        _ = ReloadAsync();
    }

    public Task RefreshAsync() => ReloadAsync();

    private void PopulateYearCombo()
    {
        YearComboBox.Items.Clear();
        var current = DateTime.Today.Year;
        for (var y = current - 1; y <= current + 2; y++)
            YearComboBox.Items.Add(y);
        YearComboBox.SelectedItem = _year;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (_controller is not null)
            await ReloadAsync();
    }

    private async void OnPrevMonthClick(object sender, RoutedEventArgs e)
    {
        if (_month == 1)
        {
            _month = 12;
            _year--;
        }
        else
        {
            _month--;
        }

        SyncYearCombo();
        await ReloadAsync();
    }

    private async void OnNextMonthClick(object sender, RoutedEventArgs e)
    {
        if (_month == 12)
        {
            _month = 1;
            _year++;
        }
        else
        {
            _month++;
        }

        SyncYearCombo();
        await ReloadAsync();
    }

    private async void OnYearChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || YearComboBox.SelectedItem is not int year)
            return;

        _year = year;
        await ReloadAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e) => await ReloadAsync();

    private void SyncYearCombo()
    {
        if (!YearComboBox.Items.Contains(_year))
            YearComboBox.Items.Add(_year);
        YearComboBox.SelectedItem = _year;
    }

    private async Task ReloadAsync()
    {
        if (_controller is null || _isLoading)
            return;

        _isLoading = true;
        try
        {
            MonthTitleTextBlock.Text = $"{GrafikNurkowyConstants.MonthNames[_month]} {_year}";

            _kolory = await _controller.GetKoloryZmianAsync();
            _workingShifts = await _controller.GetWorkingShiftsForMonthAsync(_year, _month);

            int? filter = _canEdit ? null : _shiftNumber;
            var wpisy = await _controller.GetMonthAsync(_year, _month, filter);
            _wpisy = wpisy.ToList();

            ApplyLegend();
            BuildCalendarGrid();
        }
        catch (Exception ex)
        {
            BoberMessageBox.Show(OwnerWindow, ex.Message, "Kalendarz — błąd");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ApplyLegend()
    {
        Legend1.Background = BrushFromHex(_kolory.GetValueOrDefault(1, RoleKeys.GetDefaultKolorHex(RoleKeys.KalendarzZmiana1)));
        Legend2.Background = BrushFromHex(_kolory.GetValueOrDefault(2, RoleKeys.GetDefaultKolorHex(RoleKeys.KalendarzZmiana2)));
        Legend3.Background = BrushFromHex(_kolory.GetValueOrDefault(3, RoleKeys.GetDefaultKolorHex(RoleKeys.KalendarzZmiana3)));
    }

    private void BuildCalendarGrid()
    {
        CalendarGrid.Children.Clear();
        CalendarGrid.RowDefinitions.Clear();
        CalendarGrid.ColumnDefinitions.Clear();

        for (var c = 0; c < 7; c++)
            CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var r = 0; r < 6; r++)
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        for (var d = 0; d < 7; d++)
        {
            var header = new TextBlock
            {
                Text = DayHeaders[d],
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = (Brush)FindResource("ForegroundMutedBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, d);
            CalendarGrid.Children.Add(header);
        }

        var first = new DateOnly(_year, _month, 1);
        var daysInMonth = DateTime.DaysInMonth(_year, _month);
        // Poniedziałek = 0
        var startOffset = ((int)first.DayOfWeek + 6) % 7;

        for (var day = 1; day <= daysInMonth; day++)
        {
            var index = startOffset + day - 1;
            var row = 1 + index / 7;
            var col = index % 7;
            var date = new DateOnly(_year, _month, day);
            var cell = CreateDayCell(date);
            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, col);
            CalendarGrid.Children.Add(cell);
        }
    }

    private Border CreateDayCell(DateOnly date)
    {
        var workingShift = _workingShifts.GetValueOrDefault(date.Day, 1);
        var colorHex = _kolory.GetValueOrDefault(
            workingShift,
            RoleKeys.GetDefaultKolorHex(RoleKeys.KalendarzKluczForZmiana(workingShift)));

        var dayWpisy = _wpisy.Where(w => w.Data == date).ToList();
        var hasNote = dayWpisy.Count > 0;
        var allRead = hasNote && dayWpisy.All(w => w.Odczyt?.Przeczytane == true);
        var anyUnread = hasNote && dayWpisy.Any(w => w.Odczyt?.Przeczytane != true);

        var border = new Border
        {
            Background = BrushFromHex(colorHex),
            BorderBrush = (Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(2),
            Cursor = Cursors.Hand,
            Tag = date,
            Padding = new Thickness(6, 4, 6, 4)
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var dayNumber = new TextBlock
        {
            Text = date.Day.ToString(),
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = Brushes.Black
        };
        Grid.SetRow(dayNumber, 0);
        grid.Children.Add(dayNumber);

        if (hasNote)
        {
            var info = new TextBlock
            {
                Text = "ℹ",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = allRead
                    ? new SolidColorBrush(MediaColor.FromRgb(0x70, 0x70, 0x70))
                    : new SolidColorBrush(MediaColor.FromRgb(0x1E, 0x88, 0xE5)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                ToolTip = BuildNoteTooltip(dayWpisy)
            };
            Grid.SetRow(info, 1);
            grid.Children.Add(info);

            if (_canEdit && anyUnread)
            {
                border.BorderBrush = new SolidColorBrush(MediaColor.FromRgb(0x1E, 0x88, 0xE5));
                border.BorderThickness = new Thickness(2);
            }
        }

        border.Child = grid;
        border.MouseLeftButtonUp += async (_, _) => await OnDayClickAsync(date);
        return border;
    }

    private static string BuildNoteTooltip(IReadOnlyList<KalendarzWpis> wpisy)
    {
        return string.Join(
            Environment.NewLine,
            wpisy.Select(w =>
            {
                var status = w.Odczyt?.Przeczytane == true
                    ? $"przeczytane ({w.Odczyt.PrzeczytanePrzez}, {w.Odczyt.DataOdczytu:dd.MM.yyyy HH:mm})"
                    : "nieprzeczytane";
                return $"Zmiana {ToRoman(w.ZmianaId)}: {status}";
            }));
    }

    private async Task OnDayClickAsync(DateOnly date)
    {
        if (_controller is null)
            return;

        try
        {
            if (_canEdit)
                await HandleDcaDayAsync(date);
            else
                await HandleShiftDayAsync(date);
        }
        catch (Exception ex)
        {
            BoberMessageBox.Show(OwnerWindow, ex.Message, "Kalendarz — błąd");
        }
    }

    private async Task HandleDcaDayAsync(DateOnly date)
    {
        if (_controller is null)
            return;

        var workingShift = _workingShifts.GetValueOrDefault(date.Day, await _controller.GetWorkingShiftAsync(date));
        var dayWpisy = _wpisy.Where(w => w.Data == date).ToList();
        var existingForShift = dayWpisy.FirstOrDefault(w => w.ZmianaId == workingShift);
        var status = BuildDcaStatusText(dayWpisy);

        var dialog = new KalendarzNotatkaDialog { Owner = OwnerWindow };
        dialog.ConfigureForEdit(
            date,
            workingShift,
            existingForShift?.Tresc ?? dayWpisy.FirstOrDefault()?.Tresc,
            status,
            canDelete: dayWpisy.Count > 0);

        if (dialog.ShowDialog() != true)
            return;

        var targets = dialog.TargetAllShifts
            ? (IReadOnlyList<int>)[1, 2, 3]
            : [workingShift];

        switch (dialog.ResultAction)
        {
            case KalendarzNotatkaDialog.DialogAction.Save:
                await _controller.UpsertAsync(date, targets, dialog.NoteText, _userLogin);
                break;
            case KalendarzNotatkaDialog.DialogAction.Delete:
                await _controller.DeleteAsync(date, targets);
                break;
            default:
                return;
        }

        await ReloadAsync();
    }

    private async Task HandleShiftDayAsync(DateOnly date)
    {
        if (_controller is null || _shiftNumber is not int zmianaId)
            return;

        var wpis = _wpisy.FirstOrDefault(w => w.Data == date && w.ZmianaId == zmianaId);
        if (wpis is null)
        {
            BoberMessageBox.Show(OwnerWindow, "Brak notatki DCA dla tego dnia.", "Kalendarz");
            return;
        }

        var alreadyRead = wpis.Odczyt?.Przeczytane == true;
        var readInfo = alreadyRead
            ? $"Przeczytane przez {wpis.Odczyt!.PrzeczytanePrzez} ({wpis.Odczyt.DataOdczytu:dd.MM.yyyy HH:mm})"
            : null;

        var dialog = new KalendarzNotatkaDialog { Owner = OwnerWindow };
        dialog.ConfigureForRead(date, zmianaId, wpis.Tresc, alreadyRead, readInfo);

        if (dialog.ShowDialog() != true)
            return;

        if (dialog.ResultAction == KalendarzNotatkaDialog.DialogAction.MarkRead)
        {
            await _controller.MarkAsReadAsync(wpis.Id, zmianaId, _userLogin);
            await ReloadAsync();
        }
    }

    private static string? BuildDcaStatusText(IReadOnlyList<KalendarzWpis> dayWpisy)
    {
        if (dayWpisy.Count == 0)
            return null;

        return string.Join(
            Environment.NewLine,
            dayWpisy
                .OrderBy(w => w.ZmianaId)
                .Select(w =>
                {
                    if (w.Odczyt?.Przeczytane == true)
                    {
                        return $"Zmiana {ToRoman(w.ZmianaId)}: przeczytane — {w.Odczyt.PrzeczytanePrzez} ({w.Odczyt.DataOdczytu:dd.MM.yyyy HH:mm})";
                    }

                    return $"Zmiana {ToRoman(w.ZmianaId)}: nieprzeczytane";
                }));
    }

    private static string ToRoman(int zmianaId) => zmianaId switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        _ => zmianaId.ToString()
    };

    private static Brush BrushFromHex(string hex)
    {
        try
        {
            var color = (MediaColor)WpfColorConverter.ConvertFromString(hex)!;
            return new SolidColorBrush(color);
        }
        catch
        {
            return Brushes.LightGray;
        }
    }

    private Window? OwnerWindow => Window.GetWindow(this);
}
