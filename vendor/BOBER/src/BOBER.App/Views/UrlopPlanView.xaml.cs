using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BOBER.App.Controllers;
using BOBER.App.Helpers;
using BOBER.App.Logging;
using BOBER.App.ViewModels;
using BOBER.App.Views.Chrome;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Services.Urlop;
using Microsoft.Win32;

namespace BOBER.App.Views;

public partial class UrlopPlanView : UserControl
{
    private UrlopPlanController? _controller;
    private int _year;
    private readonly bool[] _monthLoaded = new bool[13];
    private readonly Dictionary<int, DataGrid> _monthGrids = new();
    private (UrlopPlanRowViewModel Vm, int Month, int Day)? _selectedCell;
    private bool _initializeCalled;
    private bool _isSettingUp;
    private bool _isRestrictingSelection;
    private int _setupGeneration;

    public bool IsEmbedded { get; set; }

    /// <summary>Gdy true — Gość nie może edytować planu (blokada Zmiany).</summary>
    public bool IsReadOnlyMode { get; set; }

    private static readonly string[] MonthNames =
    [
        "", "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec",
        "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień"
    ];

    public UrlopPlanView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    public void Initialize(UrlopPlanController controller)
    {
        _controller = controller;
        _year = controller.DefaultPlanYear;
        PopulateYearCombo();
        ApplyReadOnlyModeUi();
        _initializeCalled = true;
        _ = SetupUiAsync();
    }

    private void ApplyReadOnlyModeUi()
    {
        ImportButton.IsEnabled = !IsReadOnlyMode;
        ClearYearButton.IsEnabled = !IsReadOnlyMode;
        ApplyToGrafikButton.IsEnabled = !IsReadOnlyMode;
        ShortcutsStatusTextBlock.Text = IsReadOnlyMode
            ? "Plan urlopów jest zablokowany — tylko podgląd (Gość nie może edytować)."
            : "Skróty: Shift/Ctrl+klik — zaznacz dni w wierszu, potem W — wypoczynkowy  |  D — dodatkowy  |  Spacja — wyczyść  |  Prawy przycisk — menu";
    }

    private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true || !IsEmbedded || !_initializeCalled)
            return;

        await OdswiezPoAktywacjiAsync();
    }

    public async Task OdswiezPoAktywacjiAsync()
    {
        if (_controller is null || !IsLoaded)
            return;

        await _controller.LoadAsync();
        Array.Fill(_monthLoaded, false);

        var month = MonthTabControl.SelectedItem is TabItem { Tag: int m } ? m : (int?)null;
        if (month is >= 1 and <= 12)
            await LoadMonthAsync(month.Value, force: true);

        UpdateEmptyPersonnelBanner();
        await RefreshValidationAsync();
    }

    private void PopulateYearCombo()
    {
        YearComboBox.Items.Clear();
        var current = DateTime.Today.Year;
        for (var y = current; y <= current + 2; y++)
            YearComboBox.Items.Add(y);

        YearComboBox.SelectedItem = _year;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (_initializeCalled && MonthTabControl.Items.Count == 0)
            await SetupUiAsync();
    }

    private async Task SetupUiAsync()
    {
        if (_controller is null)
            return;

        var generation = ++_setupGeneration;
        _isSettingUp = true;

        try
        {
            MonthTabControl.Items.Clear();
            _monthGrids.Clear();
            Array.Fill(_monthLoaded, false);
            _selectedCell = null;
            ZmianaTextBlock.Text = $"{_controller.NazwaZmiany} (zmiana {_controller.ZmianaId})";

            await _controller.LoadAsync();
            if (generation != _setupGeneration)
                return;

            UpdateEmptyPersonnelBanner();

            for (var m = 1; m <= 12; m++)
            {
                MonthTabControl.Items.Add(new TabItem
                {
                    Header = MonthNames[m],
                    Tag = m,
                    Content = CreateMonthGrid(m)
                });
            }

            MonthTabControl.Items.Add(new TabItem
            {
                Header = "Instrukcja",
                Tag = "Instrukcja",
                Content = CreateInstructionPanel()
            });

            if (generation != _setupGeneration)
                return;

            var currentMonth = DateTime.Today.Month;
            MonthTabControl.SelectedIndex = currentMonth - 1;
            await LoadMonthAsync(currentMonth, force: true);
            await RefreshValidationAsync();
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd ładowania planu urlopów");
        }
        finally
        {
            _isSettingUp = false;
        }
    }

    private void UpdateEmptyPersonnelBanner()
    {
        if (_controller is null)
            return;

        var count = _controller.GetFunkcjonariusze().Count;
        if (count == 0)
        {
            EmptyPersonnelTextBlock.Text =
                $"Brak funkcjonariuszy przypisanych do {_controller.NazwaZmiany}. "
                + "Sprawdź edycję personelu i ustawienia kolejności w module Grafik.";
            EmptyPersonnelTextBlock.Visibility = Visibility.Visible;
            return;
        }

        EmptyPersonnelTextBlock.Visibility = Visibility.Collapsed;
        EmptyPersonnelTextBlock.Text = string.Empty;
    }

    private Grid CreateMonthGrid(int month)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var dataGrid = new DataGrid
        {
            Name = $"UrlopMonthGrid_{month}",
            Style = (Style)FindResource("BoberDataGrid"),
            Tag = month,
            IsReadOnly = true,
            Focusable = true,
            SelectionUnit = DataGridSelectionUnit.Cell,
            SelectionMode = DataGridSelectionMode.Extended
        };
        _monthGrids[month] = dataGrid;
        dataGrid.SelectedCellsChanged += OnSelectedCellsChanged;
        dataGrid.PreviewKeyDown += OnDataGridPreviewKeyDown;
        dataGrid.PreviewMouseLeftButtonDown += OnDataGridPreviewMouseLeftButtonDown;
        dataGrid.LoadingRow += OnDataGridLoadingRow;
        dataGrid.MouseRightButtonDown += OnDataGridRightClick;

        scroll.Content = dataGrid;
        grid.Children.Add(scroll);
        return grid;
    }

    private static ScrollViewer CreateInstructionPanel()
    {
        var panel = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };

        panel.Children.Add(new TextBlock
        {
            Text = "Wytyczne planowania urlopów",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x28, 0x18)),
            Margin = new Thickness(0, 0, 0, 12)
        });

        foreach (var rule in UrlopPlanInstructions.Rules)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"• {rule}",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x28, 0x18)),
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        panel.Children.Add(new TextBlock
        {
            Text = "Oznaczenia w siatce",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x28, 0x18)),
            Margin = new Thickness(0, 12, 0, 8)
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"• w — urlop wypoczynkowy (kolumna W, {UrlopPlanInstructions.LimitWypoczynkowy} dni/os. w roku; zielony = zaplanowano w całości, czerwony = przekroczono limit)",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 6)
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"• d — urlop dodatkowy (kolumna D, {UrlopPlanInstructions.LimitDodatkowy} dni/os. w roku; zielony = zaplanowano w całości, czerwony = przekroczono limit)",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 6)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "• Żółte tło w nagłówku kolumny — dzień służby zmiany (kolor można zmienić w Ustawieniach)",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 12)
        });

        panel.Children.Add(new TextBlock
        {
            Text = "• Wiersz „Na urlopie”: zielony = max−2 osoby, pomarańczowy = max−1, czerwony = max osób na urlopie w dniu służby",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 12)
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Skróty klawiszowe",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x28, 0x18)),
            Margin = new Thickness(0, 0, 0, 8)
        });

        foreach (var skrot in UrlopPlanInstructions.Skroty)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"• {skrot}",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 6)
            });
        }

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = panel
        };
    }

    private async Task LoadMonthAsync(int month, bool force = false)
    {
        if (_controller is null || (!force && _monthLoaded[month]))
            return;

        var dataGrid = FindMonthGrid(month);
        if (dataGrid is null)
            return;

        try
        {
            var yearWpisy = await _controller.GetYearAsync(_year);
            var yearCounts = BuildYearCountsByPerson(yearWpisy);
            var wpisy = await _controller.GetMonthAsync(_year, month);
            var wpisyLookup = wpisy
                .GroupBy(w => (w.FunkcjonariuszId, w.Dzien))
                .ToDictionary(g => g.Key, g => g.Last().TypUrlopu);

            var funkcjonariusze = _controller.GetFunkcjonariusze();
            var daysInMonth = DateTime.DaysInMonth(_year, month);
            var rows = new List<UrlopPlanRowViewModel>();

            for (var i = 0; i < funkcjonariusze.Count; i++)
            {
                var f = funkcjonariusze[i];
                var row = new UrlopPlanRowViewModel
                {
                    FunkcjonariuszId = f.Id,
                    Numer = i + 1,
                    ImieNazwisko = UrlopNameMatcher.ToExcelFormat(f.Imie, f.Nazwisko)
                };

                for (var day = 1; day <= daysInMonth; day++)
                {
                    if (wpisyLookup.TryGetValue((f.Id, day), out var typ))
                        row.SetCell(day, typ);
                }

                ApplyYearCountsToRow(row, yearCounts);
                rows.Add(row);
            }

            var summaryRow = BuildSummaryRow(rows, daysInMonth);
            rows.Add(summaryRow);

            var workDays = await _controller.GetWorkDaysForMonthAsync(_year, month);
            var dzienSluzbyBrush = _controller.GetDzienSluzbyBrush();
            UrlopPlanGridBuilder.BuildColumns(
                dataGrid, _year, month, _controller.MaxUrlopowNaSluzbie, workDays, dzienSluzbyBrush);
            dataGrid.ItemsSource = rows;
            _monthLoaded[month] = true;
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd ładowania planu urlopów");
        }
    }

    private static UrlopPlanRowViewModel BuildSummaryRow(IReadOnlyList<UrlopPlanRowViewModel> rows, int daysInMonth)
    {
        var summary = new UrlopPlanRowViewModel
        {
            IsSummaryRow = true,
            ImieNazwisko = "Na urlopie"
        };

        for (var day = 1; day <= daysInMonth; day++)
        {
            var count = rows.Count(r => r.GetCell(day) is "w" or "d");
            if (count > 0)
                summary.SetCell(day, count.ToString());
        }

        return summary;
    }

    private static Dictionary<int, (int Wypoczynkowy, int Dodatkowy)> BuildYearCountsByPerson(
        IEnumerable<UrlopPlanWpis> yearWpisy) =>
        yearWpisy
            .GroupBy(w => w.FunkcjonariuszId)
            .ToDictionary(
                g => g.Key,
                g => (
                    g.Count(w => w.TypUrlopu == UrlopTypy.Wypoczynkowy),
                    g.Count(w => w.TypUrlopu == UrlopTypy.Dodatkowy)));

    private static void ApplyYearCountsToRow(
        UrlopPlanRowViewModel row,
        IReadOnlyDictionary<int, (int Wypoczynkowy, int Dodatkowy)> yearCounts)
    {
        if (!row.FunkcjonariuszId.HasValue)
            return;

        if (yearCounts.TryGetValue(row.FunkcjonariuszId.Value, out var counts))
        {
            row.WypoczynkowyCount = counts.Wypoczynkowy;
            row.DodatkowyCount = counts.Dodatkowy;
        }
        else
        {
            row.WypoczynkowyCount = 0;
            row.DodatkowyCount = 0;
        }
    }

    private async Task RefreshPersonYearCountsInAllGridsAsync(int funkcjonariuszId)
    {
        if (_controller is null)
            return;

        var yearWpisy = await _controller.GetYearAsync(_year);
        var yearCounts = BuildYearCountsByPerson(yearWpisy);
        if (!yearCounts.TryGetValue(funkcjonariuszId, out var counts))
            counts = (0, 0);

        foreach (var (month, grid) in _monthGrids)
        {
            if (!_monthLoaded[month] || grid.ItemsSource is not IEnumerable<UrlopPlanRowViewModel> rows)
                continue;

            var personRow = rows.FirstOrDefault(r => r.FunkcjonariuszId == funkcjonariuszId);
            if (personRow is null)
                continue;

            personRow.WypoczynkowyCount = counts.Wypoczynkowy;
            personRow.DodatkowyCount = counts.Dodatkowy;
            grid.Items.Refresh();
        }
    }

    private async void OnMonthTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingUp || _controller is null)
            return;

        if (e.Source != MonthTabControl || e.AddedItems.Count == 0)
            return;

        if (e.AddedItems[0] is not TabItem { Tag: int month })
            return;

        if (_monthLoaded[month])
            return;

        await LoadMonthAsync(month);
    }

    private async void OnYearChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSettingUp || _controller is null)
            return;

        if (YearComboBox.SelectedItem is not int year || year == _year)
            return;

        _year = year;
        Array.Fill(_monthLoaded, false);

        if (MonthTabControl.SelectedItem is TabItem { Tag: int month })
            await LoadMonthAsync(month, force: true);

        await RefreshValidationAsync();
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
        if (primary.Item is UrlopPlanRowViewModel row
            && row.FunkcjonariuszId.HasValue
            && primary.Column?.Header is DayHeaderViewModel dayHeader)
        {
            _selectedCell = (row, (int)grid.Tag, dayHeader.Day);
            grid.Focus();
            Keyboard.Focus(grid);
        }
        else
        {
            _selectedCell = null;
        }
    }

    private static UrlopPlanRowViewModel? ResolveAnchorRow(
        SelectedCellsChangedEventArgs e,
        IReadOnlyList<DataGridCellInfo> validCells)
    {
        for (var i = e.AddedCells.Count - 1; i >= 0; i--)
        {
            if (e.AddedCells[i].Item is UrlopPlanRowViewModel added)
                return added;
        }

        return validCells[0].Item as UrlopPlanRowViewModel;
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
        cell.Column is { DisplayIndex: > 3 }
        && cell.Item is UrlopPlanRowViewModel vm
        && !vm.IsSummaryRow
        && vm.FunkcjonariuszId.HasValue
        && cell.Column.Header is DayHeaderViewModel;

    private static List<(UrlopPlanRowViewModel Vm, int Month, int Day)> GetSelectedDayCells(DataGrid grid)
    {
        var month = (int)grid.Tag;
        return GetValidDayCellInfos(grid)
            .Select(c => (
                Vm: (UrlopPlanRowViewModel)c.Item,
                Month: month,
                Day: ((DayHeaderViewModel)c.Column.Header!).Day))
            .Distinct()
            .OrderBy(c => c.Day)
            .ToList();
    }

    private void OnDataGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            grid.Focus();
            Keyboard.Focus(grid);
        }
    }

    private async void OnViewPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var dataGrid = _selectedCell is not null
            ? FindMonthGrid(_selectedCell.Value.Month)
            : null;
        if (dataGrid is null)
            return;

        await TryHandleShortcutKeyAsync(dataGrid, e);
    }

    private async void OnDataGridPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid dataGrid)
            return;

        await TryHandleShortcutKeyAsync(dataGrid, e);
    }

    private async Task TryHandleShortcutKeyAsync(DataGrid dataGrid, KeyEventArgs e)
    {
        if (_controller is null || IsReadOnlyMode)
            return;

        var typ = e.Key switch
        {
            Key.W => UrlopTypy.Wypoczynkowy,
            Key.D => UrlopTypy.Dodatkowy,
            Key.Space => "",
            _ => null
        };

        if (typ is null)
            return;

        var targets = GetSelectedDayCells(dataGrid);
        if (targets.Count == 0 && _selectedCell is not null)
            targets = [_selectedCell.Value];

        if (targets.Count == 0)
            return;

        e.Handled = true;
        await ApplyCellsValueAsync(dataGrid, targets, typ);
    }

    private void OnDataGridRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        var hit = VisualTreeHelper.HitTest(grid, e.GetPosition(grid));
        var cell = FindVisualParent<DataGridCell>(hit?.VisualHit);
        var row = cell is not null ? FindVisualParent<DataGridRow>(cell) : null;

        if (row?.Item is not UrlopPlanRowViewModel vm || vm.IsSummaryRow || !vm.FunkcjonariuszId.HasValue)
            return;

        if (cell?.Column.Header is not DayHeaderViewModel dayHeader)
            return;

        var month = (int)grid.Tag;
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

        var targets = GetSelectedDayCells(grid);
        if (targets.Count == 0)
            targets = [(vm, month, dayHeader.Day)];

        var labelSuffix = targets.Count > 1 ? $" ({targets.Count} dni)" : string.Empty;
        var menu = new ContextMenu();
        AddMenuItem(menu, $"w — Wypoczynkowy{labelSuffix}", UrlopTypy.Wypoczynkowy, grid, targets);
        AddMenuItem(menu, $"d — Dodatkowy{labelSuffix}", UrlopTypy.Dodatkowy, grid, targets);
        AddMenuItem(menu, $"— Wyczyść{labelSuffix}", "", grid, targets);
        menu.PlacementTarget = cell;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void AddMenuItem(
        ContextMenu menu,
        string label,
        string typ,
        DataGrid grid,
        List<(UrlopPlanRowViewModel Vm, int Month, int Day)> targets)
    {
        var item = new MenuItem { Header = label };
        item.Click += async (_, _) => await ApplyCellsValueAsync(grid, targets, typ);
        menu.Items.Add(item);
    }

    private async Task ApplyCellsValueAsync(
        DataGrid? dataGrid,
        IReadOnlyList<(UrlopPlanRowViewModel Vm, int Month, int Day)> cells,
        string typ)
    {
        if (_controller is null || dataGrid is null || cells.Count == 0)
            return;

        try
        {
            var affectedPersons = new HashSet<int>();
            var month = cells[0].Month;

            foreach (var (vm, cellMonth, day) in cells)
            {
                if (!vm.FunkcjonariuszId.HasValue)
                    continue;

                if (string.IsNullOrEmpty(typ))
                {
                    await _controller.ClearWpisAsync(vm.FunkcjonariuszId.Value, _year, cellMonth, day);
                    vm.ClearCell(day);
                }
                else
                {
                    await _controller.SetWpisAsync(vm.FunkcjonariuszId.Value, _year, cellMonth, day, typ);
                    vm.SetCell(day, typ);
                }

                affectedPersons.Add(vm.FunkcjonariuszId.Value);
            }

            if (dataGrid.ItemsSource is IEnumerable<UrlopPlanRowViewModel> rows)
            {
                var summary = rows.LastOrDefault(r => r.IsSummaryRow);
                if (summary is not null)
                {
                    var daysInMonth = DateTime.DaysInMonth(_year, month);
                    for (var d = 1; d <= daysInMonth; d++)
                    {
                        var count = rows.Where(r => !r.IsSummaryRow).Count(r => r.GetCell(d) is "w" or "d");
                        summary.SetCell(d, count > 0 ? count.ToString() : "");
                    }
                }
            }

            dataGrid.Items.Refresh();
            foreach (var personId in affectedPersons)
                await RefreshPersonYearCountsInAllGridsAsync(personId);
            await RefreshValidationAsync();
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd zapisu planu urlopów");
        }
    }

    private void OnDataGridLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is UrlopPlanRowViewModel row && row.IsSummaryRow)
            e.Row.FontWeight = FontWeights.SemiBold;
    }

    private async void OnImportClick(object sender, RoutedEventArgs e)
    {
        if (IsReadOnlyMode || _controller is null)
            return;

        var dialog = new OpenFileDialog
        {
            Filter = "Plik Excel (*.xlsx)|*.xlsx",
            Title = "Import planu urlopów"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await _controller.ImportFromExcelAsync(_year, dialog.FileName);
            Array.Fill(_monthLoaded, false);
            if (MonthTabControl.SelectedItem is TabItem { Tag: int month })
                await LoadMonthAsync(month, force: true);
            await RefreshValidationAsync();
            BoberMessageBox.Show(OwnerWindow, "Plan urlopów został zaimportowany.", "Plan urlopów");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd importu planu urlopów");
        }
    }

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        if (_controller is null)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "Plik Excel (*.xlsx)|*.xlsx",
            FileName = $"Program URLOPY {_year} Zm{_controller.ZmianaId}.xlsx",
            Title = "Eksport planu urlopów"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await _controller.ExportToExcelAsync(_year, dialog.FileName);
            BoberMessageBox.Show(OwnerWindow, "Plan urlopów został wyeksportowany.", "Plan urlopów");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd eksportu planu urlopów");
        }
    }

    private async void OnClearYearClick(object sender, RoutedEventArgs e)
    {
        if (IsReadOnlyMode || _controller is null)
            return;

        var dialog = new ClearUrlopYearDialog(_year, _controller.NazwaZmiany)
        {
            Owner = OwnerWindow
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await _controller.ClearYearAsync(_year);
            Array.Fill(_monthLoaded, false);
            for (var month = 1; month <= 12; month++)
                await LoadMonthAsync(month, force: true);
            await RefreshValidationAsync();
            BoberMessageBox.Show(OwnerWindow, $"Plan urlopów na rok {_year} został wyczyszczony.", "Plan urlopów");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd czyszczenia planu urlopów");
        }
    }

    private async void OnApplyToGrafikClick(object sender, RoutedEventArgs e)
    {
        if (IsReadOnlyMode || _controller is null)
            return;

        var issues = await _controller.ValidateAsync(_year);
        var warning = issues.Count > 0
            ? $"\n\nUwaga: wykryto {issues.Count} naruszeń reguł planowania."
            : string.Empty;

        var confirm = BoberMessageBox.Show(
            OwnerWindow,
            $"Czy zastosować plan urlopów {_year} na grafiku służb (wpis „U”)?{warning}",
            "Plan urlopów",
            BoberMessageButtons.YesNo);

        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            var result = await _controller.ApplyToGrafikAsync(_year);
            var message = $"Zastosowano na grafiku:\n"
                + $"• nowe wpisy: {result.AppliedCount}\n"
                + $"• zaktualizowane: {result.UpdatedCount}\n"
                + $"• pominięte (ręczne): {result.SkippedManualCount}";

            if (result.SkippedManualDetails.Count > 0)
            {
                message += "\n\nPominięte:\n" + string.Join("\n", result.SkippedManualDetails.Take(10));
                if (result.SkippedManualDetails.Count > 10)
                    message += $"\n… i {result.SkippedManualDetails.Count - 10} więcej";
            }

            BoberMessageBox.Show(OwnerWindow, message, "Plan urlopów");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd synchronizacji z grafikiem");
        }
    }

    private async Task RefreshValidationAsync()
    {
        if (_controller is null)
            return;

        try
        {
            var issues = await _controller.ValidateAsync(_year);
            if (issues.Count == 0)
            {
                ValidationTextBlock.Visibility = Visibility.Collapsed;
                ValidationTextBlock.Text = string.Empty;
                return;
            }

            ValidationTextBlock.Text = string.Join("  |  ", issues.Take(5).Select(i => i.Message))
                + (issues.Count > 5 ? $"  |  … (+{issues.Count - 5})" : "");
            ValidationTextBlock.Visibility = Visibility.Visible;
        }
        catch
        {
            ValidationTextBlock.Visibility = Visibility.Collapsed;
        }
    }

    private DataGrid? FindMonthGrid(int month) =>
        _monthGrids.TryGetValue(month, out var grid) ? grid : null;

    private Window? OwnerWindow => Window.GetWindow(this);

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;
            var result = FindVisualChild<T>(child);
            if (result is not null)
                return result;
        }

        return null;
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T parent)
                return parent;
            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}
