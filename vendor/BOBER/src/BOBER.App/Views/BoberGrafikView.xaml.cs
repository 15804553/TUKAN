using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using BOBER.App.Controllers;
using BOBER.App.Helpers;
using BOBER.App.Logging;
using BOBER.App.ViewModels;
using BOBER.App.Views.Chrome;
using BOBER.Core.Constants;
using BOBER.Core.Grafik;

namespace BOBER.App.Views;

/// <summary>Grafik roczny BOBER — osadzalny w TUKAN lub w oknie standalone.</summary>
public partial class BoberGrafikView : UserControl
{
    private MainController? _controller;
    private int _year;
    private readonly bool[] _monthLoaded = new bool[13];
    private (GrafikRowViewModel Vm, int Month, int Day)? _selectedCell;
    private bool _initializeCalled;
    private int _setupGeneration;
    private bool _isRestrictingSelection;
    private readonly GrafikUndoStack _undoStack = new();
    private bool _isUndoing;

    private static readonly string[] MonthNames =
    [
        "", "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec",
        "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień"
    ];

    public bool IsEmbedded { get; set; }

    public event EventHandler? LogoutRequested;

    public BoberGrafikView()
    {
        Resources = UrlopPlanPalette.CreateResources();
        InitializeComponent();
        Loaded += OnLoaded;
        IsVisibleChanged += OnIsVisibleChanged;
        PreviewKeyDown += OnPreviewUndoKeyDown;
        _undoStack.Changed += (_, _) => UpdateUndoButtonState();
    }

    public void Initialize(MainController controller)
    {
        _controller = controller;
        _year = controller.CurrentYear;
        ApplyEmbeddedUi();
        _initializeCalled = true;

        if (IsLoaded)
        {
            _ = SetupGrafikUiAsync();
        }
    }

    public async Task ReloadAfterSettingsAsync()
    {
        if (_controller is null)
        {
            return;
        }

        await _controller.LoadAsync();
        Array.Fill(_monthLoaded, false);
        _undoStack.Clear();

        var selected = MonthTabControl.SelectedIndex + 1;
        if (selected >= 1)
        {
            await LoadMonthAsync(selected);
        }
    }

    /// <summary>Odświeża dane po powrocie z innego widoku TUKAN (np. edycja personelu).</summary>
    public async Task OdswiezPoAktywacjiAsync()
    {
        if (_controller is null || !_initializeCalled || !IsLoaded) return;
        if (!Enumerable.Range(1, 12).Any(m => _monthLoaded[m])) return;

        await ReloadAfterSettingsAsync();
    }

    private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true || !IsEmbedded) return;
        await OdswiezPoAktywacjiAsync();
    }

    private void ApplyEmbeddedUi()
    {
        if (!IsEmbedded)
        {
            return;
        }

        SettingsButton.Visibility = Visibility.Collapsed;
        LogoutButton.Visibility = Visibility.Collapsed;
    }

    private Window? OwnerWindow => Window.GetWindow(this);

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_initializeCalled)
        {
            return;
        }

        await SetupGrafikUiAsync();
    }

    private async Task SetupGrafikUiAsync()
    {
        if (_controller is null)
        {
            return;
        }

        var generation = ++_setupGeneration;

        MonthTabControl.Items.Clear();
        Array.Fill(_monthLoaded, false);
        _selectedCell = null;
        _undoStack.Clear();

        ZmianaTextBlock.Text = _controller.NazwaZmiany;
        RokTextBlock.Text = $"Rok {_year}";

        try
        {
            await _controller.LoadAsync();
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex,
                "Nie udało się załadować danych z bazy Chomik. Sprawdź ścieżkę do bazy danych w Ustawieniach.");
            return;
        }

        if (generation != _setupGeneration)
        {
            return;
        }

        for (var m = 1; m <= 12; m++)
        {
            var tab = new TabItem
            {
                Header = MonthNames[m],
                Tag = m,
                Content = CreateMonthGrid(m)
            };
            MonthTabControl.Items.Add(tab);
        }

        var currentMonth = DateTime.Today.Month;
        MonthTabControl.SelectedIndex = currentMonth - 1;

        if (generation != _setupGeneration)
        {
            return;
        }

        await LoadMonthAsync(currentMonth);
    }

    private Grid CreateMonthGrid(int month)
    {
        var compactButtonPadding = new Thickness(10, 4, 10, 4);

        var outerGrid = new Grid();
        outerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2, 0, 2)
        };

        var exportBtn = new Button
        {
            Content = $"Eksportuj {MonthNames[month]} do Excel",
            Style = (Style)FindResource("UrlopPlanSecondaryButton"),
            Margin = new Thickness(0, 0, 6, 0),
            Padding = compactButtonPadding,
            FontSize = 12,
            MinHeight = 28,
            Tag = month
        };
        exportBtn.Click += OnExportClick;
        btnPanel.Children.Add(exportBtn);

        if (IsEmbedded && _controller is { IsShiftScoped: true })
        {
            var nurkowyBtn = new Button
            {
                Content = $"Generuj / aktualizuj grafik nurkowy — {MonthNames[month]}",
                Style = (Style)FindResource("UrlopPlanSecondaryButton"),
                Margin = new Thickness(0, 0, 6, 0),
                Padding = compactButtonPadding,
                FontSize = 12,
                MinHeight = 28,
                Tag = month
            };
            nurkowyBtn.Click += OnGenerateGrafikNurkowyClick;
            btnPanel.Children.Add(nurkowyBtn);
        }

        Grid.SetRow(btnPanel, 0);
        outerGrid.Children.Add(btnPanel);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetRow(scrollViewer, 1);

        var dataGrid = new DataGrid
        {
            Style = (Style)FindResource("UrlopPlanDataGrid"),
            Tag = month,
            Name = $"MonthGrid_{month}",
            SelectionMode = DataGridSelectionMode.Extended
        };

        dataGrid.LoadingRow += OnDataGridLoadingRow;
        dataGrid.MouseRightButtonDown += OnDataGridRightClick;
        dataGrid.MouseDoubleClick += OnDataGridDoubleClick;
        dataGrid.SelectedCellsChanged += OnSelectedCellsChanged;
        dataGrid.KeyDown += OnDataGridKeyDown;
        dataGrid.PreviewMouseLeftButtonDown += OnDataGridPreviewMouseLeftButtonDown;

        scrollViewer.Content = dataGrid;
        outerGrid.Children.Add(scrollViewer);

        return outerGrid;
    }

    private async void OnMonthTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_controller is null || MonthTabControl.SelectedItem is not TabItem selectedTab)
        {
            return;
        }

        var month = (int)selectedTab.Tag;
        if (_monthLoaded[month])
        {
            return;
        }

        await LoadMonthAsync(month);
    }

    private async Task LoadMonthAsync(int month)
    {
        if (_controller is null)
        {
            return;
        }

        var dataGrid = FindMonthGrid(month);
        if (dataGrid is null)
        {
            return;
        }

        try
        {
            var workDays = await _controller.GetWorkDaysForMonthAsync(_year, month);
            dataGrid.SelectionMode = DataGridSelectionMode.Extended;
            GrafikGridBuilder.BuildColumns(dataGrid, _year, month, workDays, _controller.GetCellColors());
            var rows = await _controller.BuildRowsAsync(_year, month);
            dataGrid.ItemsSource = rows;
            _monthLoaded[month] = true;
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd ładowania grafiku");
        }
    }

    private void OnDataGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            grid.Focus();
            Keyboard.Focus(grid);
        }
    }

    private void OnSelectedCellsChanged(object? sender, SelectedCellsChangedEventArgs e)
    {
        if (_isRestrictingSelection || sender is not DataGrid grid)
            return;

        var validCells = GetValidDayCellInfos(grid).ToList();
        if (validCells.Count == 0)
        {
            if (grid.SelectedCells.Count > 0)
                RestrictSelection(grid, Array.Empty<DataGridCellInfo>());
            _selectedCell = null;
            UpdateRowSelectionHighlight(grid, null);
            return;
        }

        // Multi-select tylko w jednym wierszu (zakres dni jednej osoby).
        var anchorRow = ResolveAnchorRow(e, validCells);
        var sameRowCells = validCells
            .Where(c => ReferenceEquals(c.Item, anchorRow))
            .ToList();

        if (sameRowCells.Count != grid.SelectedCells.Count)
            RestrictSelection(grid, sameRowCells);

        var primary = ResolvePrimaryCell(grid, sameRowCells);
        if (primary.Item is GrafikRowViewModel row
            && row.FunkcjonariuszId.HasValue
            && primary.Column?.Header is DayHeaderViewModel dayHeader)
        {
            _selectedCell = (row, (int)grid.Tag, dayHeader.Day);
            UpdateRowSelectionHighlight(grid, row);
            grid.Focus();
            Keyboard.Focus(grid);
        }
        else
        {
            _selectedCell = null;
            UpdateRowSelectionHighlight(grid, null);
        }
    }

    private static void UpdateRowSelectionHighlight(DataGrid grid, GrafikRowViewModel? activeRow)
    {
        if (grid.ItemsSource is not IEnumerable<GrafikRowViewModel> rows)
            return;

        foreach (var row in rows)
            row.IsSelectionHighlight = ReferenceEquals(row, activeRow);
    }

    private static GrafikRowViewModel? ResolveAnchorRow(
        SelectedCellsChangedEventArgs e,
        IReadOnlyList<DataGridCellInfo> validCells)
    {
        for (var i = e.AddedCells.Count - 1; i >= 0; i--)
        {
            if (e.AddedCells[i].Item is GrafikRowViewModel added)
                return added;
        }

        return validCells[0].Item as GrafikRowViewModel;
    }

    private static DataGridCellInfo ResolvePrimaryCell(
        DataGrid grid,
        IReadOnlyList<DataGridCellInfo> sameRowCells)
    {
        if (sameRowCells.Any(c =>
                ReferenceEquals(c.Item, grid.CurrentCell.Item)
                && ReferenceEquals(c.Column, grid.CurrentCell.Column)))
            return grid.CurrentCell;

        return sameRowCells[^1];
    }

    private void RestrictSelection(DataGrid grid, IReadOnlyList<DataGridCellInfo> keep)
    {
        _isRestrictingSelection = true;
        try
        {
            grid.SelectedCells.Clear();
            foreach (var cell in keep)
                grid.SelectedCells.Add(cell);
        }
        finally
        {
            _isRestrictingSelection = false;
        }
    }

    private static IEnumerable<DataGridCellInfo> GetValidDayCellInfos(DataGrid grid) =>
        grid.SelectedCells.Where(IsValidDayCell);

    private static bool IsValidDayCell(DataGridCellInfo cell) =>
        cell.Item is GrafikRowViewModel vm
        && !vm.IsSummaryRow
        && !vm.IsNotesRow
        && vm.FunkcjonariuszId.HasValue
        && cell.Column?.Header is DayHeaderViewModel;

    private static List<(GrafikRowViewModel Vm, int Month, int Day)> GetSelectedDayCells(DataGrid grid)
    {
        var month = (int)grid.Tag;
        return GetValidDayCellInfos(grid)
            .Select(c => (
                Vm: (GrafikRowViewModel)c.Item,
                Month: month,
                Day: ((DayHeaderViewModel)c.Column.Header!).Day))
            .Distinct()
            .OrderBy(c => c.Day)
            .ToList();
    }

    private List<(GrafikRowViewModel Vm, int Month, int Day)> ResolveActionTargets(DataGrid dataGrid)
    {
        var targets = GetSelectedDayCells(dataGrid);
        if (targets.Count == 0 && _selectedCell is not null)
            targets = [_selectedCell.Value];
        return targets;
    }

    private async void OnPreviewUndoKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Z || Keyboard.Modifiers != ModifierKeys.Control)
            return;

        e.Handled = true;
        await UndoLastChangeAsync();
    }

    private async void OnUndoClick(object sender, RoutedEventArgs e) =>
        await UndoLastChangeAsync();

    private void UpdateUndoButtonState()
    {
        if (UndoButton is not null)
            UndoButton.IsEnabled = _undoStack.CanUndo && !_isUndoing;
    }

    private static GrafikUndoCell CaptureUndoCell(GrafikRowViewModel vm, int month, int day) =>
        new()
        {
            FunkcjonariuszId = vm.FunkcjonariuszId!.Value,
            Month = month,
            Day = day,
            PreviousTyp = vm.GetCell(day),
            PreviousFromUrlopPlan = vm.FromUrlopPlan[day]
        };

    private void CommitUndoEntry(List<GrafikUndoCell> cells)
    {
        if (_isUndoing || cells.Count == 0)
            return;

        _undoStack.Push(new GrafikUndoEntry { Cells = cells });
    }

    private async Task UndoLastChangeAsync()
    {
        if (_controller is null || _isUndoing || !_undoStack.TryPop(out var entry))
            return;

        _isUndoing = true;
        UpdateUndoButtonState();

        try
        {
            var months = new HashSet<int>();

            foreach (var cell in entry.Cells)
            {
                var dataGrid = FindMonthGrid(cell.Month);
                var vm = FindRowByFunkcjonariuszId(dataGrid, cell.FunkcjonariuszId);
                if (vm is null)
                    continue;

                if (string.IsNullOrEmpty(cell.PreviousTyp))
                {
                    await _controller.ClearWpisAsync(cell.FunkcjonariuszId, _year, cell.Month, cell.Day);
                    vm.ClearCell(cell.Day);
                }
                else
                {
                    var fromPlan = cell.PreviousFromUrlopPlan
                        && GrafikWpisTypy.JestUrlopem(cell.PreviousTyp);
                    await _controller.SetWpisAsync(
                        cell.FunkcjonariuszId, _year, cell.Month, cell.Day,
                        cell.PreviousTyp, isAuto: fromPlan);
                    vm.SetCell(cell.Day, cell.PreviousTyp, fromUrlopPlan: fromPlan);
                }

                months.Add(cell.Month);
            }

            foreach (var month in months)
            {
                var dataGrid = FindMonthGrid(month);
                if (dataGrid is null)
                    continue;

                dataGrid.Items.Refresh();
                await RefreshSummaryRowAsync(dataGrid, month);
            }
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Nie udało się cofnąć zmiany w grafiku.");
        }
        finally
        {
            _isUndoing = false;
            UpdateUndoButtonState();
        }
    }

    private static GrafikRowViewModel? FindRowByFunkcjonariuszId(DataGrid? dataGrid, int funkcjonariuszId)
    {
        if (dataGrid?.ItemsSource is not IEnumerable<GrafikRowViewModel> rows)
            return null;

        return rows.FirstOrDefault(r => r.FunkcjonariuszId == funkcjonariuszId);
    }

    private async void OnDataGridKeyDown(object sender, KeyEventArgs e)
    {
        if (_controller is null || sender is not DataGrid dataGrid)
            return;

        var targets = ResolveActionTargets(dataGrid);
        if (targets.Count == 0)
            return;

        if (e.Key == Key.O)
        {
            e.Handled = true;
            await ApplyOddalToCellsAsync(dataGrid, targets);
            return;
        }

        if (e.Key is Key.OemPeriod or Key.Decimal)
        {
            e.Handled = true;
            await ApplyKropkaToCellsAsync(dataGrid, targets);
            return;
        }

        // „/” — znaczek „?” (potrzebuje wolne); na klawiaturze PL/US: Oem2 / OemQuestion / Divide
        if (e.Key is Key.Oem2 or Key.OemQuestion or Key.Divide)
        {
            e.Handled = true;
            await ApplyPytajnikToCellsAsync(dataGrid, targets);
            return;
        }

        var typWpisu = e.Key switch
        {
            Key.D => GrafikWpisTypy.Dyzur,
            Key.W => GrafikWpisTypy.WolnaSluzba,
            Key.U => GrafikWpisTypy.Urlop,
            Key.E => GrafikWpisTypy.Delegacja,
            Key.S => GrafikWpisTypy.Szkolenie,
            Key.C => GrafikWpisTypy.Chory,
            Key.Space => "",
            _ => null
        };
        if (typWpisu is null)
            return;

        e.Handled = true;
        await ApplyWpisToCellsAsync(dataGrid, targets, typWpisu);
    }

    private void OnDataGridLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is GrafikRowViewModel row)
        {
            e.Row.Background = row.RowBackground;
            if (row.IsSummaryRow)
            {
                e.Row.FontWeight = FontWeights.SemiBold;
                e.Row.Foreground = UrlopPlanPalette.ForegroundBrush;
                e.Row.MinHeight = 88;
            }
            else if (row.IsNotesRow)
            {
                e.Row.MinHeight = 28;
                e.Row.Height = 28;
                e.Row.Background = UrlopPlanPalette.SurfaceVariantBrush;
                e.Row.IsHitTestVisible = true;
                e.Row.Focusable = false;
            }
            else
            {
                e.Row.Foreground = row.RowForeground;
            }
        }
    }

    private void OnDataGridRightClick(object? sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        var hit = VisualTreeHelper.HitTest(grid, e.GetPosition(grid));
        if (hit?.VisualHit is null)
        {
            return;
        }

        var cell = FindVisualParent<DataGridCell>(hit.VisualHit);
        if (cell is null)
        {
            return;
        }

        var row = FindVisualParent<DataGridRow>(cell);
        if (row?.Item is not GrafikRowViewModel vm || vm.IsSummaryRow || vm.IsNotesRow || !vm.FunkcjonariuszId.HasValue)
        {
            return;
        }

        var month = (int)grid.Tag;

        if (IsUwagiColumn(cell.Column))
        {
            e.Handled = true;
            _ = EditUwagaMiesiecznaAsync(grid, vm, month);
            return;
        }

        if (cell.Column.Header is not DayHeaderViewModel dayHeader)
        {
            return;
        }

        var cellInfo = new DataGridCellInfo(vm, cell.Column);
        var alreadyInSelection = grid.SelectedCells.Any(c =>
            ReferenceEquals(c.Item, vm) && ReferenceEquals(c.Column, cell.Column));

        if (!alreadyInSelection)
        {
            RestrictSelection(grid, [cellInfo]);
            grid.CurrentCell = cellInfo;
        }

        _selectedCell = (vm, month, dayHeader.Day);
        grid.Focus();
        Keyboard.Focus(grid);

        var targets = ResolveActionTargets(grid);
        if (targets.Count == 0)
            targets = [(vm, month, dayHeader.Day)];

        ShowCellContextMenu(grid, targets);
        e.Handled = true;
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        var hit = VisualTreeHelper.HitTest(grid, e.GetPosition(grid));
        if (hit?.VisualHit is null)
            return;

        var cell = FindVisualParent<DataGridCell>(hit.VisualHit);
        if (cell is null || !IsUwagiColumn(cell.Column))
            return;

        var row = FindVisualParent<DataGridRow>(cell);
        if (row?.Item is not GrafikRowViewModel vm
            || vm.IsSummaryRow
            || vm.IsNotesRow
            || !vm.FunkcjonariuszId.HasValue)
            return;

        e.Handled = true;
        _ = EditUwagaMiesiecznaAsync(grid, vm, (int)grid.Tag);
    }

    private static bool IsUwagiColumn(DataGridColumn? column) =>
        column?.Header is string header
        && header.Equals(GrafikGridBuilder.UwagiColumnHeader, StringComparison.Ordinal);

    private void ShowCellContextMenu(
        DataGrid grid,
        List<(GrafikRowViewModel Vm, int Month, int Day)> targets)
    {
        var darkBg = UrlopPlanPalette.CardBrush;
        var lightFg = UrlopPlanPalette.ForegroundBrush;
        var labelSuffix = targets.Count > 1 ? $" ({targets.Count} dni)" : string.Empty;

        var menu = new ContextMenu
        {
            Background = darkBg,
            Foreground = lightFg,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Template = CreateContextMenuTemplate()
        };

        var menuItems = new (string Label, string Akcja, string Gesture)[]
        {
            ($"D — Dyżur{labelSuffix}", GrafikWpisTypy.Dyzur, "D"),
            ($"WS — Wolna służba{labelSuffix}", GrafikWpisTypy.WolnaSluzba, "W"),
            ($"U — Urlop{labelSuffix}", GrafikWpisTypy.Urlop, "U"),
            ($"Del — Delegacja{labelSuffix}", GrafikWpisTypy.Delegacja, "E"),
            ($"S — Szkolenie{labelSuffix}", GrafikWpisTypy.Szkolenie, "S"),
            ($"C — Chory{labelSuffix}", GrafikWpisTypy.Chory, "C"),
            ($"O — Oddaje{labelSuffix}", "ODDAJE", "O"),
            ($". — Osoba chętna oddać{labelSuffix}", "KROPKA", "."),
            ($"? — Osoba potrzebuje wolne{labelSuffix}", "PYTAJNIK", "/"),
            ($"— Wyczyść{labelSuffix}", "", "Spacja"),
            ("Notatka", "NOTATKA", ""),
            ("Uwagi", "UWAGI", "")
        };

        var primary = targets[0];
        foreach (var (label, akcja, gesture) in menuItems)
        {
            if (akcja == "NOTATKA")
            {
                menu.Items.Add(new Separator
                {
                    Margin = new Thickness(4, 4, 4, 4),
                    Background = UrlopPlanPalette.BorderBrush
                });
            }

            var item = new MenuItem
            {
                Header = label,
                InputGestureText = gesture,
                Tag = (targets, akcja, primary.Month, primary.Day, primary.Vm),
                Background = darkBg,
                Foreground = lightFg,
                FontSize = 14,
                Template = CreateMenuItemTemplate()
            };
            item.Click += OnMenuItemClick;
            menu.Items.Add(item);
        }

        menu.PlacementTarget = grid;
        grid.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private async void OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (_controller is null || sender is not MenuItem item)
            return;

        var (targets, akcja, month, day, primaryVm) =
            ((List<(GrafikRowViewModel Vm, int Month, int Day)>, string, int, int, GrafikRowViewModel))item.Tag;

        var dataGrid = FindMonthGrid(month);
        if (dataGrid is null)
            return;

        switch (akcja)
        {
            case "ODDAJE":
                await ApplyOddalToCellsAsync(dataGrid, targets);
                return;
            case "KROPKA":
                await ApplyKropkaToCellsAsync(dataGrid, targets);
                return;
            case "PYTAJNIK":
                await ApplyPytajnikToCellsAsync(dataGrid, targets);
                return;
            case "NOTATKA":
                await EditNotatkaAsync(dataGrid, month, day);
                return;
            case "UWAGI":
                await EditUwagaMiesiecznaAsync(dataGrid, primaryVm, month);
                return;
            default:
                await ApplyWpisToCellsAsync(dataGrid, targets, akcja);
                return;
        }
    }

    private async Task EditUwagaMiesiecznaAsync(DataGrid dataGrid, GrafikRowViewModel vm, int month)
    {
        if (_controller is null || !vm.FunkcjonariuszId.HasValue)
            return;

        var dialog = new GrafikNotatkaDialog
        {
            Owner = OwnerWindow,
            DialogTitle = "Uwagi",
            NoteText = vm.UwagaMiesieczna
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var tresc = dialog.NoteText;
            await _controller.SetUwagaMiesiecznaAsync(vm.FunkcjonariuszId.Value, _year, month, tresc);
            vm.UwagaMiesieczna = tresc?.Trim() ?? string.Empty;
            dataGrid.Items.Refresh();
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Nie udało się zapisać uwagi.");
        }
    }

    private async Task EditNotatkaAsync(DataGrid dataGrid, int month, int day)
    {
        if (_controller is null || dataGrid.ItemsSource is not IEnumerable<GrafikRowViewModel> rows)
            return;

        var notesRow = rows.FirstOrDefault(r => r.IsNotesRow);
        if (notesRow is null)
            return;

        var dialog = new GrafikNotatkaDialog
        {
            Owner = OwnerWindow,
            NoteText = notesRow.GetCell(day)
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var tresc = dialog.NoteText;
            await _controller.SetNotatkaAsync(_year, month, day, tresc);
            _controller.UpdateNotesRowCell(notesRow, day, tresc);
            dataGrid.Items.Refresh();
            dataGrid.ScrollIntoView(notesRow);
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Nie udało się zapisać notatki.");
        }
    }

    private async Task ApplyOddalToCellsAsync(
        DataGrid dataGrid,
        IReadOnlyList<(GrafikRowViewModel Vm, int Month, int Day)> cells)
    {
        if (_controller is null || cells.Count == 0)
            return;

        try
        {
            var hasForbidden = false;
            var applied = false;
            var undoCells = new List<GrafikUndoCell>();

            foreach (var (vm, month, day) in cells)
            {
                if (!vm.FunkcjonariuszId.HasValue)
                    continue;

                var biezacy = vm.GetCell(day);
                if (GrafikWpisTypy.NieMoznaOddacBoZakazanyTyp(biezacy))
                {
                    hasForbidden = true;
                    continue;
                }

                var nowy = GrafikWpisTypy.PrzelaczOddal(biezacy);
                if (nowy is null)
                    continue;

                undoCells.Add(CaptureUndoCell(vm, month, day));
                await ApplyWpisSilentAsync(vm, month, day, nowy);
                applied = true;
            }

            if (hasForbidden && !applied)
            {
                BoberMessageBox.Show(
                    OwnerWindow,
                    "Tej służby nie można oddać.\nOznaczenia S (szkolenie), C (chory) i Del (delegacja) nie podlegają oddaniu.",
                    "Oddaje");
                return;
            }

            if (applied)
            {
                CommitUndoEntry(undoCells);
                dataGrid.Items.Refresh();
                await RefreshSummaryRowAsync(dataGrid, cells[0].Month);
            }
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd zapisu wpisu grafiku");
        }
    }

    private async Task ApplyKropkaToCellsAsync(
        DataGrid dataGrid,
        IReadOnlyList<(GrafikRowViewModel Vm, int Month, int Day)> cells)
    {
        if (_controller is null || cells.Count == 0)
            return;

        try
        {
            var applied = false;
            var anyInvalid = false;
            var undoCells = new List<GrafikUndoCell>();

            foreach (var (vm, month, day) in cells)
            {
                if (!vm.FunkcjonariuszId.HasValue)
                    continue;

                var nowy = GrafikWpisTypy.PrzelaczKropke(vm.GetCell(day));
                if (nowy is null)
                {
                    anyInvalid = true;
                    continue;
                }

                undoCells.Add(CaptureUndoCell(vm, month, day));
                await ApplyWpisSilentAsync(vm, month, day, nowy);
                applied = true;
            }

            if (!applied && anyInvalid)
            {
                BoberMessageBox.Show(
                    OwnerWindow,
                    "Znak „.” (osoba chętna oddać) można ustawić tylko przy oznaczeniu D (dyżur), U (urlop), U+WS lub WS (wolna służba).",
                    "Grafik");
                return;
            }

            if (applied)
            {
                CommitUndoEntry(undoCells);
                dataGrid.Items.Refresh();
                await RefreshSummaryRowAsync(dataGrid, cells[0].Month);
            }
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd zapisu wpisu grafiku");
        }
    }

    private async Task ApplyPytajnikToCellsAsync(
        DataGrid dataGrid,
        IReadOnlyList<(GrafikRowViewModel Vm, int Month, int Day)> cells)
    {
        if (_controller is null || cells.Count == 0)
            return;

        try
        {
            var applied = false;
            var anyInvalid = false;
            var undoCells = new List<GrafikUndoCell>();

            foreach (var (vm, month, day) in cells)
            {
                if (!vm.FunkcjonariuszId.HasValue)
                    continue;

                var nowy = GrafikWpisTypy.PrzelaczPytajnik(vm.GetCell(day));
                if (nowy is null)
                {
                    anyInvalid = true;
                    continue;
                }

                undoCells.Add(CaptureUndoCell(vm, month, day));
                await ApplyWpisSilentAsync(vm, month, day, nowy);
                applied = true;
            }

            if (!applied && anyInvalid)
            {
                BoberMessageBox.Show(
                    OwnerWindow,
                    "Znak „?” (osoba potrzebuje wolne) można ustawić tylko gdy osoba jest w pracy (pusta komórka).",
                    "Grafik");
                return;
            }

            if (applied)
            {
                CommitUndoEntry(undoCells);
                dataGrid.Items.Refresh();
                await RefreshSummaryRowAsync(dataGrid, cells[0].Month);
            }
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd zapisu wpisu grafiku");
        }
    }

    private async Task ApplyWpisToCellsAsync(
        DataGrid dataGrid,
        IReadOnlyList<(GrafikRowViewModel Vm, int Month, int Day)> cells,
        string typWpisu)
    {
        if (_controller is null || cells.Count == 0)
            return;

        try
        {
            var undoCells = new List<GrafikUndoCell>();

            foreach (var (vm, month, day) in cells)
            {
                if (!vm.FunkcjonariuszId.HasValue)
                    continue;

                var resolved = string.IsNullOrEmpty(typWpisu)
                    ? typWpisu
                    : GrafikWpisTypy.ResolvePoNalozeniu(vm.GetCell(day), typWpisu);

                undoCells.Add(CaptureUndoCell(vm, month, day));
                await ApplyWpisSilentAsync(vm, month, day, resolved);
            }

            CommitUndoEntry(undoCells);
            dataGrid.Items.Refresh();
            await RefreshSummaryRowAsync(dataGrid, cells[0].Month);
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd zapisu wpisu grafiku");
        }
    }

    private async Task ApplyWpisSilentAsync(
        GrafikRowViewModel vm,
        int month,
        int day,
        string typWpisu)
    {
        if (_controller is null || !vm.FunkcjonariuszId.HasValue)
            return;

        if (string.IsNullOrEmpty(typWpisu))
        {
            await _controller.ClearWpisAsync(vm.FunkcjonariuszId.Value, _year, month, day);
            vm.ClearCell(day);
        }
        else
        {
            var toSave = GrafikWpisTypy.ResolveDelSDlaZapisu(vm.GetCell(day), typWpisu);
            await _controller.SetWpisAsync(vm.FunkcjonariuszId.Value, _year, month, day, toSave);
            vm.SetCell(day, toSave);
        }
    }

    private Task RefreshSummaryRowAsync(DataGrid dataGrid, int month)
    {
        if (_controller is null || dataGrid.ItemsSource is not IEnumerable<GrafikRowViewModel> rows)
        {
            return Task.CompletedTask;
        }

        var allRows = rows.ToList();
        var summaryRow = allRows.FirstOrDefault(r => r.IsSummaryRow);
        if (summaryRow is null)
        {
            return Task.CompletedTask;
        }

        _controller.RefreshSummaryRow(summaryRow, allRows, month);
        dataGrid.Items.Refresh();
        return Task.CompletedTask;
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (_controller is null || sender is not Button btn)
        {
            return;
        }

        var month = (int)btn.Tag;
        var initialDir = await _controller.GetExportPathGrafikSluzbAsync();

        var dlg = new SaveFileDialog
        {
            Title = $"Eksport grafiku — {MonthNames[month]} {_year}",
            Filter = "Plik Excel (*.xlsx)|*.xlsx",
            FileName = $"{SanitizeFileName(_controller.NazwaZmiany)}_{MonthNames[month]}_{_year}.xlsx",
            InitialDirectory = Directory.Exists(initialDir) ? initialDir : string.Empty
        };

        if (dlg.ShowDialog(OwnerWindow) != true)
        {
            return;
        }

        try
        {
            await _controller.ExportMonthAsync(dlg.FileName, _year, month);
            BoberMessageBox.Show(OwnerWindow, $"Eksport zakończony sukcesem:\n{dlg.FileName}", "BOBER");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd eksportu do Excel");
        }
    }

    private async void OnGenerateGrafikNurkowyClick(object? sender, RoutedEventArgs e)
    {
        if (_controller is null || sender is not Button btn)
            return;

        var month = (int)btn.Tag;
        try
        {
            var result = await _controller.GenerateGrafikNurkowyAsync(_year, month);
            BoberMessageBox.Show(
                OwnerWindow,
                $"{result.Message}\n\n{result.FilePath}",
                "Grafik nurkowy");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd generowania grafiku nurkowego");
        }
    }

    private async void OnExportYearClick(object sender, RoutedEventArgs e)
    {
        if (_controller is null)
        {
            return;
        }

        var initialDir = await _controller.GetExportPathGrafikSluzbAsync();
        var dlg = new SaveFileDialog
        {
            Title = $"Eksport grafiku — cały rok {_year}",
            Filter = "Plik Excel (*.xlsx)|*.xlsx",
            FileName = $"{SanitizeFileName(_controller.NazwaZmiany)}_CalyRok_{_year}.xlsx",
            InitialDirectory = Directory.Exists(initialDir) ? initialDir : string.Empty
        };

        if (dlg.ShowDialog(OwnerWindow) != true)
        {
            return;
        }

        try
        {
            await _controller.ExportYearAsync(dlg.FileName, _year);
            BoberMessageBox.Show(OwnerWindow, $"Eksport zakończony sukcesem:\n{dlg.FileName}", "BOBER");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd eksportu do Excel");
        }
    }

    private async void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (_controller is null)
        {
            return;
        }

        var settingsWindow = new SettingsWindow(_controller.CreateSettingsController());
        if (OwnerWindow is not null)
        {
            settingsWindow.Owner = OwnerWindow;
        }

        if (settingsWindow.ShowDialog() == true)
        {
            try
            {
                await _controller.LoadAsync();
            }
            catch (Exception ex)
            {
                UiErrorReporter.Show(OwnerWindow, ex, "Błąd odświeżania danych po ustawieniach");
            }

            Array.Fill(_monthLoaded, false);
            _undoStack.Clear();
            var selected = MonthTabControl.SelectedIndex + 1;
            await LoadMonthAsync(selected);
        }
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e) => LogoutRequested?.Invoke(this, EventArgs.Empty);

    private DataGrid? FindMonthGrid(int month)
    {
        if (MonthTabControl.Items.Count < month)
        {
            return null;
        }

        var tab = (TabItem)MonthTabControl.Items[month - 1];
        if (tab.Content is not Grid outerGrid)
        {
            return null;
        }

        foreach (var child in outerGrid.Children)
        {
            if (child is ScrollViewer sv && sv.Content is DataGrid dg)
            {
                return dg;
            }
        }

        return null;
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Zmiana";
        }

        var invalid = System.IO.Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c)).Trim();
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        while (true)
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent is null)
            {
                return null;
            }

            if (parent is T typed)
            {
                return typed;
            }

            child = parent;
        }
    }

    private static ControlTemplate CreateContextMenuTemplate()
    {
        var template = new ControlTemplate(typeof(ContextMenu));

        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.BackgroundProperty, UrlopPlanPalette.CardBrush);
        borderFactory.SetValue(Border.BorderBrushProperty, UrlopPlanPalette.BorderBrush);
        borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(0, 3, 0, 3));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
        borderFactory.SetValue(UIElement.EffectProperty, null);

        var itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
        borderFactory.AppendChild(itemsPresenter);

        template.VisualTree = borderFactory;
        return template;
    }

    private static ControlTemplate CreateMenuItemTemplate()
    {
        var template = new ControlTemplate(typeof(MenuItem));

        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.Name = "Bd";
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(14, 6, 14, 6));
        borderFactory.SetBinding(Border.BackgroundProperty,
            new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });

        var dockFactory = new FrameworkElementFactory(typeof(DockPanel));
        dockFactory.SetValue(DockPanel.LastChildFillProperty, true);

        var gestureText = new FrameworkElementFactory(typeof(TextBlock));
        gestureText.SetBinding(TextBlock.TextProperty,
            new Binding(nameof(MenuItem.InputGestureText))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });
        gestureText.SetValue(DockPanel.DockProperty, Dock.Right);
        gestureText.SetValue(TextBlock.MarginProperty, new Thickness(24, 0, 0, 0));
        gestureText.SetValue(TextBlock.OpacityProperty, 0.65);
        gestureText.SetValue(TextBlock.FontSizeProperty, 12.0);
        gestureText.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        dockFactory.AppendChild(gestureText);

        var headerPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        headerPresenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        headerPresenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        headerPresenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        dockFactory.AppendChild(headerPresenter);

        borderFactory.AppendChild(dockFactory);

        template.VisualTree = borderFactory;

        var highlightTrigger = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
        highlightTrigger.Setters.Add(new Setter(
            Border.BackgroundProperty,
            UrlopPlanPalette.SurfaceVariantBrush,
            "Bd"));
        template.Triggers.Add(highlightTrigger);

        return template;
    }
}
