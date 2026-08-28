using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BOBER.App.Helpers;
using BOBER.App.Views.Chrome;
using Tukan.App.Controllers;
using Tukan.App.ViewModels;
using Tukan.App.Views.Chrome;

namespace Tukan.App.Views;

public partial class DutyAssignmentsView : UserControl
{
    private readonly bool[] _monthLoaded = new bool[13];
    private DutyAssignmentsController? _controller;
    private int _year;
    private bool _initialized;
    private int _setupGeneration;

    private static readonly string[] MonthNames =
    [
        "", "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec",
        "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień"
    ];

    public DutyAssignmentsView()
    {
        Resources = UrlopPlanPalette.CreateResources();
        InitializeComponent();
        Loaded += OnLoaded;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    public void Initialize(DutyAssignmentsController controller)
    {
        _controller = controller;
        _year = controller.CurrentYear;
        _initialized = true;

        if (IsLoaded)
        {
            _ = SetupAsync();
        }
    }

    public async Task RefreshAsync()
    {
        if (!_initialized || _controller is null)
        {
            return;
        }

        try
        {
            await SetupAsync();
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(Window.GetWindow(this), $"Nie udało się załadować obsady funkcji:\n\n{ex.Message}", "TUKAN");
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            await SetupAsync();
        }
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            await RefreshAsync();
        }
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

    private async Task SetupAsync()
    {
        if (_controller is null)
        {
            return;
        }

        var generation = ++_setupGeneration;
        MonthTabControl.Items.Clear();
        Array.Fill(_monthLoaded, false);

        ShiftTextBlock.Text = _controller.ShiftName;
        YearTextBlock.Text = $"Rok {_year}";

        for (var month = 1; month <= 12; month++)
        {
            MonthTabControl.Items.Add(new TabItem
            {
                Header = MonthNames[month],
                Tag = month,
                Content = CreateMonthGrid(month)
            });
        }

        var selectedMonth = DateTime.Today.Year == _year ? DateTime.Today.Month : 1;
        MonthTabControl.SelectedIndex = selectedMonth - 1;

        if (generation != _setupGeneration)
        {
            return;
        }

        await LoadMonthAsync(selectedMonth);
    }

    private FrameworkElement CreateMonthGrid(int month)
    {
        var dataGrid = new DataGrid
        {
            Style = (Style)FindResource("UrlopPlanDataGrid"),
            Tag = month,
            Name = $"DutyAssignmentsGrid_{month}",
            IsReadOnly = true
        };
        dataGrid.PreviewMouseLeftButtonDown += OnDataGridPreviewMouseLeftButtonDown;
        dataGrid.MouseDoubleClick += OnDataGridDoubleClick;

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = dataGrid
        };
    }

    private async Task LoadMonthAsync(int month)
    {
        if (_controller is null)
        {
            return;
        }

        var grid = FindMonthGrid(month);
        if (grid is null)
        {
            return;
        }

        try
        {
            var workDays = await _controller.GetWorkDaysForMonthAsync(_year, month);
            DutyAssignmentsGridBuilder.BuildColumns(grid, _year, month, workDays);
            grid.ItemsSource = await _controller.BuildRowsAsync(_year, month);
            _monthLoaded[month] = true;
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(Window.GetWindow(this), $"Nie udało się wczytać danych dla miesiąca:\n\n{ex.Message}", "TUKAN");
        }
    }

    private DataGrid? FindMonthGrid(int month)
    {
        var tab = MonthTabControl.Items
            .OfType<TabItem>()
            .FirstOrDefault(item => Equals(item.Tag, month));

        return (tab?.Content as ScrollViewer)?.Content as DataGrid;
    }

    private void OnDataGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid || _controller is null)
        {
            return;
        }

        var hit = VisualTreeHelper.HitTest(grid, e.GetPosition(grid));
        if (hit?.VisualHit is null)
        {
            return;
        }

        var cell = FindVisualParent<DataGridCell>(hit.VisualHit);
        if (cell is null || !IsUwagiColumn(cell.Column))
        {
            return;
        }

        if (cell.DataContext is not DutyAssignmentsRowViewModel vm)
        {
            return;
        }

        e.Handled = true;
        _ = EditUwagaMiesiecznaAsync(grid, vm, (int)grid.Tag);
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid || _controller is null)
        {
            return;
        }

        var hit = VisualTreeHelper.HitTest(grid, e.GetPosition(grid));
        if (hit?.VisualHit is null)
        {
            return;
        }

        var cell = FindVisualParent<DataGridCell>(hit.VisualHit);
        if (cell is null || !IsUwagiColumn(cell.Column))
        {
            return;
        }

        if (cell.DataContext is not DutyAssignmentsRowViewModel vm)
        {
            return;
        }

        e.Handled = true;
        _ = EditUwagaMiesiecznaAsync(grid, vm, (int)grid.Tag);
    }

    private async Task EditUwagaMiesiecznaAsync(DataGrid grid, DutyAssignmentsRowViewModel vm, int month)
    {
        if (_controller is null)
        {
            return;
        }

        var dialog = new GrafikNotatkaDialog
        {
            Owner = Window.GetWindow(this),
            DialogTitle = "Uwagi",
            NoteText = vm.UwagaMiesieczna
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var tresc = dialog.NoteText;
            await _controller.SetUwagaMiesiecznaAsync(vm.FunkcjonariuszId, _year, month, tresc);
            vm.UwagaMiesieczna = tresc?.Trim() ?? string.Empty;
            grid.Items.Refresh();
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(Window.GetWindow(this), $"Nie udało się zapisać uwagi:\n\n{ex.Message}", "TUKAN");
        }
    }

    private static bool IsUwagiColumn(DataGridColumn? column) =>
        column?.Header is string header
        && header.Equals(DutyAssignmentsGridBuilder.UwagiColumnHeader, StringComparison.Ordinal);

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        while (true)
        {
            if (child is T match)
            {
                return match;
            }

            var parent = VisualTreeHelper.GetParent(child);
            if (parent is null)
            {
                return null;
            }

            child = parent;
        }
    }
}
