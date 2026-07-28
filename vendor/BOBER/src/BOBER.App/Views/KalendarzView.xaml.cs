using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BOBER.App.Controllers;
using BOBER.App.ViewModels;
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

    public event EventHandler? NotesChanged;

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
            : "Kliknij dzień, aby odczytać notatki, usuwać swoje lub DCA oraz wysyłać prywatne informacje między zmianami.";
        FooterTextBlock.Text = canEdit
            ? "Status odczytu: po potwierdzeniu przez zmianę pojawia się informacja przy notatce."
            : "Prywatne notatki między zmianami nie są widoczne dla DCA. Przycisk „Przeczytałem” potwierdza odczyt otrzymanej notatki.";

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

            await _controller.ApplyAutoDeleteAsync(_shiftNumber, _canEdit);
            _kolory = await _controller.GetKoloryZmianAsync();
            _workingShifts = await _controller.GetWorkingShiftsForMonthAsync(_year, _month);

            var wpisy = await _controller.GetMonthAsync(
                _year,
                _month,
                _canEdit ? null : _shiftNumber,
                includePrivateEntries: !_canEdit);
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
            NotesChanged?.Invoke(this, EventArgs.Empty);
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
                var prefix = w.TypWpisu switch
                {
                    KalendarzTypWpisu.Dca => $"DCA -> zmiana {ToRoman(w.ZmianaId)}",
                    KalendarzTypWpisu.OdpowiedzDca => $"Zmiana {ToRoman(w.AutorZmianaId ?? 0)} -> DCA",
                    _ => $"Zmiana {ToRoman(w.AutorZmianaId ?? 0)} -> zmiana {ToRoman(w.ZmianaId)}"
                };
                return $"{prefix}: {status}";
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

        var dayWpisy = _wpisy.Where(w => w.Data == date).ToList();
        var replies = dayWpisy.Where(w => w.TypWpisu == KalendarzTypWpisu.OdpowiedzDca).ToList();
        if (replies.Count > 0)
        {
            await ShowDcaDayDialogAsync(date, dayWpisy);
            return;
        }

        var workingShift = _workingShifts.GetValueOrDefault(date.Day, await _controller.GetWorkingShiftAsync(date));
        var dcaNotes = dayWpisy.Where(w => w.TypWpisu == KalendarzTypWpisu.Dca).ToList();
        var existingForShift = dcaNotes.FirstOrDefault(w => w.ZmianaId == workingShift);
        var status = BuildDcaStatusText(dcaNotes);

        var dialog = new KalendarzNotatkaDialog { Owner = OwnerWindow };
        dialog.ConfigureForEdit(
            date,
            workingShift,
            existingForShift?.Tresc ?? dcaNotes.FirstOrDefault()?.Tresc,
            status,
            canDelete: dcaNotes.Count > 0);

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

    private async Task ShowDcaDayDialogAsync(DateOnly date, IReadOnlyList<KalendarzWpis> dayWpisy)
    {
        if (_controller is null)
            return;

        var models = dayWpisy
            .OrderByDescending(w => w.DataModyfikacji)
            .Select(w => ToDcaDayEntryViewModel(w))
            .ToList();
        var dialog = new KalendarzDzienDialog { Owner = OwnerWindow };
        dialog.Configure(
            date,
            canAddPrivate: true,
            canDeleteVisibleEntries: true,
            addButtonText: "Notatka DCA");
        dialog.Entries = models;

        if (dialog.ShowDialog() != true)
            return;

        switch (dialog.ResultAction)
        {
            case KalendarzDzienDialog.DialogAction.AddPrivate:
                await EditDcaNoteForDayAsync(date);
                await ShowDcaDayDialogAsync(
                    date,
                    _wpisy.Where(w => w.Data == date).OrderByDescending(w => w.DataModyfikacji).ToList());
                break;
            case KalendarzDzienDialog.DialogAction.Open:
                if (dialog.SelectedEntry is not null)
                {
                    await OpenDcaEntryAsync(date, dialog.SelectedEntry);
                    await ShowDcaDayDialogAsync(
                        date,
                        _wpisy.Where(w => w.Data == date).OrderByDescending(w => w.DataModyfikacji).ToList());
                }
                break;
            case KalendarzDzienDialog.DialogAction.DeleteSelected:
                await DeleteSelectedShiftEntriesAsync(dialog.SelectedEntries, viewerShiftId: 0);
                break;
        }
    }

    private async Task EditDcaNoteForDayAsync(DateOnly date)
    {
        if (_controller is null)
            return;

        var workingShift = _workingShifts.GetValueOrDefault(date.Day, await _controller.GetWorkingShiftAsync(date));
        var dcaNotes = _wpisy.Where(w => w.Data == date && w.TypWpisu == KalendarzTypWpisu.Dca).ToList();
        var existingForShift = dcaNotes.FirstOrDefault(w => w.ZmianaId == workingShift);
        var dialog = new KalendarzNotatkaDialog { Owner = OwnerWindow };
        dialog.ConfigureForEdit(
            date,
            workingShift,
            existingForShift?.Tresc,
            BuildDcaStatusText(dcaNotes),
            canDelete: dcaNotes.Count > 0);

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

    private async Task OpenDcaEntryAsync(DateOnly date, KalendarzDzienWpisViewModel entry)
    {
        if (_controller is null)
            return;

        if (entry.Wpis.TypWpisu == KalendarzTypWpisu.Dca)
        {
            var workingShift = entry.Wpis.ZmianaId is >= 1 and <= 3
                ? entry.Wpis.ZmianaId
                : _workingShifts.GetValueOrDefault(date.Day, await _controller.GetWorkingShiftAsync(date));
            var dcaNotes = _wpisy.Where(w => w.Data == date && w.TypWpisu == KalendarzTypWpisu.Dca).ToList();
            var dialog = new KalendarzNotatkaDialog { Owner = OwnerWindow };
            dialog.ConfigureForEdit(
                date,
                workingShift,
                entry.Wpis.Tresc,
                BuildDcaStatusText(dcaNotes),
                canDelete: true);

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
            return;
        }

        var alreadyRead = entry.Wpis.Odczyt?.Przeczytane == true;
        var readDialog = new KalendarzNotatkaDialog { Owner = OwnerWindow };
        readDialog.ConfigureForRead(
            date,
            entry.Wpis.AutorZmianaId ?? 0,
            entry.Wpis.Tresc,
            alreadyRead,
            alreadyRead
                ? $"Przeczytane przez {entry.Wpis.Odczyt!.PrzeczytanePrzez} ({entry.Wpis.Odczyt.DataOdczytu:dd.MM.yyyy HH:mm})"
                : "Nieprzeczytane — potwierdź odczyt przyciskiem poniżej.",
            entry.Tytul,
            canConfirmRead: entry.CanConfirmRead,
            canReply: false);

        if (readDialog.ShowDialog() != true)
            return;

        if (readDialog.ResultAction == KalendarzNotatkaDialog.DialogAction.MarkRead && entry.CanConfirmRead)
        {
            await _controller.MarkAsReadAsync(entry.Wpis.Id, 0, _userLogin);
            await ReloadAsync();
        }
    }

    private async Task HandleShiftDayAsync(DateOnly date)
    {
        if (_controller is null || _shiftNumber is not int zmianaId)
            return;

        var dayWpisy = _wpisy
            .Where(w => w.Data == date && IsVisibleForShift(w, zmianaId))
            .OrderByDescending(w => w.DataModyfikacji)
            .ToList();

        if (dayWpisy.Count == 0)
        {
            await ShowShiftDayDialogAsync(date, []);
            return;
        }

        await ShowShiftDayDialogAsync(date, dayWpisy);
    }

    private async Task ShowShiftDayDialogAsync(DateOnly date, IReadOnlyList<KalendarzWpis> dayWpisy)
    {
        if (_controller is null || _shiftNumber is not int zmianaId)
            return;

        var models = dayWpisy.Select(w => ToDayEntryViewModel(w, zmianaId)).ToList();
        var dialog = new KalendarzDzienDialog { Owner = OwnerWindow };
        dialog.Configure(date, canAddPrivate: true, canDeleteVisibleEntries: true);
        dialog.Entries = models;

        if (dialog.ShowDialog() != true)
            return;

        switch (dialog.ResultAction)
        {
            case KalendarzDzienDialog.DialogAction.AddPrivate:
                await ComposePrivateShiftNoteAsync(date, zmianaId);
                await ShowShiftDayDialogAsync(
                    date,
                    _wpisy.Where(w => w.Data == date && IsVisibleForShift(w, zmianaId))
                        .OrderByDescending(w => w.DataModyfikacji)
                        .ToList());
                break;
            case KalendarzDzienDialog.DialogAction.Open:
                if (dialog.SelectedEntry is not null)
                {
                    await OpenShiftEntryAsync(date, zmianaId, dialog.SelectedEntry);
                    await ShowShiftDayDialogAsync(
                        date,
                        _wpisy.Where(w => w.Data == date && IsVisibleForShift(w, zmianaId))
                            .OrderByDescending(w => w.DataModyfikacji)
                            .ToList());
                }
                break;
            case KalendarzDzienDialog.DialogAction.DeleteSelected:
                await DeleteSelectedShiftEntriesAsync(dialog.SelectedEntries, zmianaId);
                break;
        }
    }

    private async Task ComposePrivateShiftNoteAsync(
        DateOnly date,
        int zmianaId,
        IReadOnlyList<int>? defaultTargets = null,
        string? titleOverride = null)
    {
        if (_controller is null)
            return;

        var dialog = new KalendarzNotatkaDialog { Owner = OwnerWindow };
        dialog.ConfigureForShiftCompose(date, zmianaId, defaultTargets, titleOverride);
        if (dialog.ShowDialog() != true || dialog.ResultAction != KalendarzNotatkaDialog.DialogAction.Save)
            return;

        await _controller.AddShiftNoteAsync(date, zmianaId, dialog.SelectedPrivateTargets, dialog.NoteText, _userLogin);
        await ReloadAsync();
    }

    private async Task ComposeDcaReplyAsync(DateOnly date, int zmianaId)
    {
        if (_controller is null)
            return;

        var dialog = new KalendarzNotatkaDialog { Owner = OwnerWindow };
        dialog.ConfigureForDcaReply(date, zmianaId);
        if (dialog.ShowDialog() != true || dialog.ResultAction != KalendarzNotatkaDialog.DialogAction.Save)
            return;

        await _controller.AddDcaReplyAsync(date, zmianaId, dialog.NoteText, _userLogin);
        await ReloadAsync();
    }

    private async Task OpenShiftEntryAsync(
        DateOnly date,
        int zmianaId,
        KalendarzDzienWpisViewModel entry)
    {
        if (_controller is null)
            return;

        var wpis = entry.Wpis;
        var alreadyRead = wpis.Odczyt?.Przeczytane == true;
        var readInfo = alreadyRead
            ? $"Przeczytane przez {wpis.Odczyt!.PrzeczytanePrzez} ({wpis.Odczyt.DataOdczytu:dd.MM.yyyy HH:mm})"
            : entry.CanConfirmRead
                ? "Nieprzeczytane — potwierdź odczyt przyciskiem poniżej."
                : entry.IsSent
                    ? "Wysłana — potwierdzenie odczytu należy do adresata."
                    : "Ta notatka została już zapisana dla wskazanej zmiany.";

        var dialog = new KalendarzNotatkaDialog { Owner = OwnerWindow };
        dialog.ConfigureForRead(
            date,
            wpis.TypWpisu == KalendarzTypWpisu.OdpowiedzDca
                ? 0
                : wpis.ZmianaId,
            wpis.Tresc,
            alreadyRead,
            readInfo,
            entry.Tytul,
            canConfirmRead: entry.CanConfirmRead,
            canReply: entry.CanReply);

        if (dialog.ShowDialog() != true)
            return;

        switch (dialog.ResultAction)
        {
            case KalendarzNotatkaDialog.DialogAction.MarkRead when entry.CanConfirmRead:
                await _controller.MarkAsReadAsync(wpis.Id, wpis.ZmianaId, _userLogin);
                await ReloadAsync();
                break;
            case KalendarzNotatkaDialog.DialogAction.Reply when entry.CanReply:
                if (wpis.TypWpisu == KalendarzTypWpisu.Dca)
                    await ComposeDcaReplyAsync(date, zmianaId);
                else if (wpis.AutorZmianaId is int authorShift)
                    await ComposePrivateShiftNoteAsync(
                        date,
                        zmianaId,
                        defaultTargets: [authorShift],
                        titleOverride: $"Odpowiedź do zmiany {ToRoman(authorShift)}");
                break;
        }
    }

    private async Task DeleteSelectedShiftEntriesAsync(
        IReadOnlyList<KalendarzDzienWpisViewModel> selectedEntries,
        int viewerShiftId)
    {
        if (_controller is null)
            return;

        var notAllowed = selectedEntries.Where(e => !e.CanDelete).ToList();
        if (notAllowed.Count > 0)
        {
            BoberMessageBox.Show(
                OwnerWindow,
                viewerShiftId == 0
                    ? "Możesz usuwać notatki DCA oraz odpowiedzi zmian."
                    : "Możesz usuwać tylko notatki od DCA albo prywatne notatki utworzone przez własną zmianę.",
                "Kalendarz");
            return;
        }

        var ids = selectedEntries.Select(e => e.Wpis.Id).Distinct().ToList();
        if (ids.Count == 0)
            return;

        var confirm = BoberMessageBox.Show(
            OwnerWindow,
            ids.Count == 1
                ? "Usunąć wybraną notatkę?"
                : $"Usunąć zaznaczone notatki ({ids.Count})?",
            "Kalendarz",
            BoberMessageButtons.YesNo);
        if (confirm != MessageBoxResult.Yes)
            return;

        await _controller.DeleteManyAsync(ids);
        await ReloadAsync();
    }

    private static string? BuildDcaStatusText(IReadOnlyList<KalendarzWpis> dayWpisy)
    {
        var dcaNotes = dayWpisy.Where(w => w.TypWpisu == KalendarzTypWpisu.Dca).ToList();
        if (dcaNotes.Count == 0)
            return null;

        return string.Join(
            Environment.NewLine,
            dcaNotes
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

    private static bool IsVisibleForShift(KalendarzWpis wpis, int viewerShiftId) =>
        wpis.ZmianaId == viewerShiftId
        || (wpis.TypWpisu == KalendarzTypWpisu.MiedzyZmianami && wpis.AutorZmianaId == viewerShiftId)
        || (wpis.TypWpisu == KalendarzTypWpisu.OdpowiedzDca && wpis.AutorZmianaId == viewerShiftId);

    private static KalendarzDzienWpisViewModel ToDayEntryViewModel(KalendarzWpis wpis, int viewerShiftId)
    {
        var isDcaEntry = wpis.TypWpisu == KalendarzTypWpisu.Dca;
        var isDcaReply = wpis.TypWpisu == KalendarzTypWpisu.OdpowiedzDca;
        var isOwnPrivateEntry = wpis.AutorZmianaId == viewerShiftId;
        var isSent = (!isDcaEntry && isOwnPrivateEntry) || isDcaReply;
        var isReceived = wpis.ZmianaId == viewerShiftId && !isSent;
        var isUnread = isReceived && wpis.Odczyt?.Przeczytane != true;
        var readInfo = wpis.Odczyt?.Przeczytane == true
            ? $"przeczytane przez {wpis.Odczyt.PrzeczytanePrzez}"
            : "nieprzeczytane";

        var title = isDcaEntry
            ? $"DCA -> zmiana {ToRoman(wpis.ZmianaId)}"
            : isDcaReply
                ? $"Zmiana {ToRoman(wpis.AutorZmianaId ?? 0)} -> DCA"
                : $"Zmiana {ToRoman(wpis.AutorZmianaId ?? 0)} -> zmiana {ToRoman(wpis.ZmianaId)}";

        var details = isDcaEntry
            ? $"Odebrana od DCA, {readInfo}."
            : isDcaReply
                ? $"Wysłana odpowiedź do DCA, {readInfo}."
                : isSent
                    ? $"Wysłana do zmiany {ToRoman(wpis.ZmianaId)}, {readInfo}."
                    : $"Odebrana od zmiany {ToRoman(wpis.AutorZmianaId ?? 0)}, {readInfo}.";

        return new KalendarzDzienWpisViewModel
        {
            Wpis = wpis,
            Tytul = title,
            Szczegoly = details,
            CanDelete = isDcaEntry || isOwnPrivateEntry || isDcaReply,
            CanConfirmRead = isReceived && wpis.Odczyt?.Przeczytane != true,
            CanReply = isReceived,
            IsUnread = isUnread || (isSent && wpis.Odczyt?.Przeczytane != true),
            IsSent = isSent,
            IsReceived = isReceived
        };
    }

    private static KalendarzDzienWpisViewModel ToDcaDayEntryViewModel(KalendarzWpis wpis)
    {
        var isDcaEntry = wpis.TypWpisu == KalendarzTypWpisu.Dca;
        var isUnread = wpis.Odczyt?.Przeczytane != true;
        var readInfo = !isUnread
            ? $"przeczytane przez {wpis.Odczyt!.PrzeczytanePrzez}"
            : "nieprzeczytane";

        if (isDcaEntry)
        {
            return new KalendarzDzienWpisViewModel
            {
                Wpis = wpis,
                Tytul = $"DCA -> zmiana {ToRoman(wpis.ZmianaId)}",
                Szczegoly = $"Twoja notatka do zmiany, {readInfo}.",
                CanDelete = true,
                CanConfirmRead = false,
                CanReply = false,
                IsUnread = isUnread,
                IsSent = true,
                IsReceived = false
            };
        }

        return new KalendarzDzienWpisViewModel
        {
            Wpis = wpis,
            Tytul = $"Zmiana {ToRoman(wpis.AutorZmianaId ?? 0)} -> DCA",
            Szczegoly = $"Odebrana odpowiedź zmiany, {readInfo}.",
            CanDelete = true,
            CanConfirmRead = isUnread,
            CanReply = false,
            IsUnread = isUnread,
            IsSent = false,
            IsReceived = true
        };
    }

    private static string ToRoman(int zmianaId) => zmianaId switch
    {
        0 => "DCA",
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
