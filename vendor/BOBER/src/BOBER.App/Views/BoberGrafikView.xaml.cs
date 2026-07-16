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

    private static readonly string[] MonthNames =
    [
        "", "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec",
        "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień"
    ];

    public bool IsEmbedded { get; set; }

    public event EventHandler? LogoutRequested;

    public BoberGrafikView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        IsVisibleChanged += OnIsVisibleChanged;
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
        var outerGrid = new Grid();
        outerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 6, 0, 6)
        };

        var exportBtn = new Button
        {
            Content = $"Eksportuj {MonthNames[month]} do Excel",
            Style = (Style)FindResource("SecondaryButton"),
            Margin = new Thickness(0, 0, 8, 0),
            Tag = month
        };
        exportBtn.Click += OnExportClick;
        btnPanel.Children.Add(exportBtn);

        if (IsEmbedded && _controller is { IsShiftScoped: true })
        {
            var nurkowyBtn = new Button
            {
                Content = $"Generuj / aktualizuj grafik nurkowy — {MonthNames[month]}",
                Style = (Style)FindResource("PrimaryButton"),
                Margin = new Thickness(0, 0, 8, 0),
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
            Style = (Style)FindResource("BoberDataGrid"),
            Tag = month,
            Name = $"MonthGrid_{month}"
        };

        dataGrid.LoadingRow += OnDataGridLoadingRow;
        dataGrid.MouseRightButtonDown += OnDataGridRightClick;
        dataGrid.SelectedCellsChanged += OnSelectedCellsChanged;
        dataGrid.KeyDown += OnDataGridKeyDown;

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

    private void OnSelectedCellsChanged(object? sender, SelectedCellsChangedEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        var hasInvalid = e.AddedCells.Any(c =>
            c.Column.DisplayIndex == 0 ||
            (c.Item is GrafikRowViewModel vm && (vm.IsSummaryRow || !vm.FunkcjonariuszId.HasValue)));

        if (hasInvalid)
        {
            grid.UnselectAllCells();
            _selectedCell = null;
            return;
        }

        var cell = grid.SelectedCells.FirstOrDefault();
        if (cell.Item is GrafikRowViewModel row
            && row.FunkcjonariuszId.HasValue
            && cell.Column?.Header is DayHeaderViewModel dayHeader)
        {
            _selectedCell = (row, (int)grid.Tag, dayHeader.Day);
        }
        else
        {
            _selectedCell = null;
        }
    }

    private async void OnDataGridKeyDown(object sender, KeyEventArgs e)
    {
        if (_controller is null || _selectedCell is null)
        {
            return;
        }

        var (vm, month, day) = _selectedCell.Value;

        var typWpisu = e.Key switch
        {
            Key.D => "D",
            Key.W => "WS",
            Key.U => "U",
            Key.E => "Del",
            Key.Space => "",
            _ => null
        };
        if (typWpisu is null)
        {
            return;
        }

        e.Handled = true;
        if (sender is not DataGrid dataGrid)
        {
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(typWpisu))
            {
                await _controller.ClearWpisAsync(vm.FunkcjonariuszId!.Value, _year, month, day);
                vm.ClearCell(day);
            }
            else
            {
                await _controller.SetWpisAsync(vm.FunkcjonariuszId!.Value, _year, month, day, typWpisu);
                vm.SetCell(day, typWpisu);
            }

            dataGrid.Items.Refresh();
            await RefreshSummaryRowAsync(dataGrid, month);
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd zapisu wpisu grafiku");
        }
    }

    private void OnDataGridLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is GrafikRowViewModel row)
        {
            e.Row.Background = row.RowBackground;
            if (row.IsSummaryRow)
            {
                e.Row.FontWeight = FontWeights.SemiBold;
                e.Row.Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x28, 0x18));
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
        if (row?.Item is not GrafikRowViewModel vm || vm.IsSummaryRow || !vm.FunkcjonariuszId.HasValue)
        {
            return;
        }

        if (cell.Column.Header is not DayHeaderViewModel dayHeader)
        {
            return;
        }

        var month = (int)grid.Tag;
        ShowCellContextMenu(grid, vm, month, dayHeader.Day);
        e.Handled = true;
    }

    private void ShowCellContextMenu(DataGrid grid, GrafikRowViewModel vm, int month, int day)
    {
        var darkBg = new SolidColorBrush(Color.FromRgb(0xC2, 0xB2, 0x80));
        var lightFg = new SolidColorBrush(Color.FromRgb(0x2C, 0x28, 0x18));

        var menu = new ContextMenu
        {
            Background = darkBg,
            Foreground = lightFg,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Template = CreateContextMenuTemplate()
        };

        var menuItems = new[]
        {
            ("D — Dyżur", "D"),
            ("WS — Wolna służba", "WS"),
            ("U — Urlop", "U"),
            ("Del — Delegacja", "Del"),
            ("— Wyczyść", "")
        };

        foreach (var (label, wpis) in menuItems)
        {
            var item = new MenuItem
            {
                Header = label,
                Tag = (vm.FunkcjonariuszId!.Value, month, day, wpis),
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
        {
            return;
        }

        var (fid, month, day, typWpisu) = ((int, int, int, string))item.Tag;

        var dataGrid = FindMonthGrid(month);
        if (dataGrid?.ItemsSource is not IEnumerable<GrafikRowViewModel> rows)
        {
            return;
        }

        var vm = rows.FirstOrDefault(r => r.FunkcjonariuszId == fid);
        if (vm is null)
        {
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(typWpisu))
            {
                await _controller.ClearWpisAsync(fid, _year, month, day);
                vm.ClearCell(day);
            }
            else
            {
                await _controller.SetWpisAsync(fid, _year, month, day, typWpisu);
                vm.SetCell(day, typWpisu);
            }

            dataGrid.Items.Refresh();
            await RefreshSummaryRowAsync(dataGrid, month);
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(OwnerWindow, ex, "Błąd zapisu wpisu grafiku");
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
        borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xC2, 0xB2, 0x80)));
        borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0xA8, 0x98, 0x68)));
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
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(14, 6, 18, 6));
        borderFactory.SetBinding(Border.BackgroundProperty,
            new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });

        var headerPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        headerPresenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        headerPresenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        headerPresenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        borderFactory.AppendChild(headerPresenter);

        template.VisualTree = borderFactory;

        var highlightTrigger = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
        highlightTrigger.Setters.Add(new Setter(
            Border.BackgroundProperty,
            new SolidColorBrush(Color.FromRgb(0x8B, 0x7D, 0x56)),
            "Bd"));
        template.Triggers.Add(highlightTrigger);

        return template;
    }
}
