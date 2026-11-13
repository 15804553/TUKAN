using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Chomik.App.Controllers;
using Chomik.App.ViewModels;
using Chomik.App.Views.Chrome;
using Chomik.Core.GeneralView;
using Chomik.Core.Models;
namespace Chomik.App.Views.Pages;

public partial class GeneralPersonnelView : UserControl
{
    private const int SearchDebounceMilliseconds = 350;

    public static readonly DependencyProperty CanEditGeneralViewDatesProperty =
        DependencyProperty.Register(
            nameof(CanEditGeneralViewDates),
            typeof(bool),
            typeof(GeneralPersonnelView),
            new PropertyMetadata(false));

    public static readonly DependencyProperty CanEditGeneralViewShiftProperty =
        DependencyProperty.Register(
            nameof(CanEditGeneralViewShift),
            typeof(bool),
            typeof(GeneralPersonnelView),
            new PropertyMetadata(false));

    private readonly DashboardController _controller;
    private bool _isLoading;
    private bool _isSaving;
    private bool _suppressGridEditEvents;
    private bool _isInitializingFilters;
    private bool _suppressPermissionFilterEvents;
    private bool _stopnieLoaded;
    private List<PermissionFilterOption> _permissionFilterOptions = [];
    private CancellationTokenSource? _searchCancellation;
    private int _loadGeneration;
    private GeneralViewColumnPreferences _columnPreferences = GeneralViewColumnPreferences.DefaultVisible;

    public event EventHandler<int>? PersonnelEditRequested;

    public event EventHandler<int>? PersonnelProfileRequested;

    public event EventHandler<string>? LoadFailed;

    public bool CanEditGeneralViewDates
    {
        get => (bool)GetValue(CanEditGeneralViewDatesProperty);
        set => SetValue(CanEditGeneralViewDatesProperty, value);
    }

    public bool CanEditGeneralViewShift
    {
        get => (bool)GetValue(CanEditGeneralViewShiftProperty);
        set => SetValue(CanEditGeneralViewShiftProperty, value);
    }

    public GeneralPersonnelView(DashboardController controller)
    {
        InitializeComponent();
        Resources["StopnieList"] = Array.Empty<SlownikItem>();
        _controller = controller;
        CanEditGeneralViewDates = _controller.CanEditGeneralViewDates;
        CanEditGeneralViewShift = _controller.CanEditGeneralViewShift;
        ShiftFilterComboBox.IsEnabled = _controller.CanFilterByShift;
        ApplyAllColumnVisibility();
        Loaded += OnViewLoaded;
        Unloaded += OnViewUnloaded;
    }

    public void ApplyColumnPreferences(GeneralViewColumnPreferences preferences)
    {
        _columnPreferences = preferences;
        ApplyAllColumnVisibility();
    }

    public async Task LoadColumnPreferencesAsync()
    {
        if (!_controller.CanCustomizeGeneralViewColumns)
        {
            return;
        }

        _columnPreferences = await _controller.GetGeneralViewColumnPreferencesAsync();
        ApplyAllColumnVisibility();
    }

    private void ApplyAllColumnVisibility()
    {
        var hidden = Visibility.Collapsed;
        var visible = Visibility.Visible;

        StopienColumn.Visibility = visible;
        PelneImieNazwiskoColumn.Visibility = visible;
        ZmianaColumn.Visibility = _controller.HideGeneralViewShiftColumn
            ? hidden
            : ResolveOptionalColumnVisibility(GeneralViewColumnId.Zmiana);

        TelefonColumn.Visibility = _controller.HideTelefonColumn ? hidden : visible;

        UprawnieniaAlertColumn.Visibility = ResolveOptionalColumnVisibility(
            GeneralViewColumnId.UprawnieniaAlert,
            roleAllows: _controller.ShowUprawnieniaAlerts);
        StanowiskoColumn.Visibility = ResolveOptionalColumnVisibility(GeneralViewColumnId.Stanowisko);
        WstepienieColumn.Visibility = ResolveOptionalColumnVisibility(GeneralViewColumnId.Wstepienie);
        BadaniaColumn.Visibility = ResolveOptionalColumnVisibility(GeneralViewColumnId.Badania);
        KomoraColumn.Visibility = ResolveOptionalColumnVisibility(GeneralViewColumnId.Komora);
        KppColumn.Visibility = ResolveOptionalColumnVisibility(GeneralViewColumnId.Kpp);
        UprawnieniaColumn.Visibility = ResolveOptionalColumnVisibility(GeneralViewColumnId.Uprawnienia);
        DodatekColumn.Visibility = ResolveOptionalColumnVisibility(
            GeneralViewColumnId.Dodatek,
            roleAllows: _controller.CanViewSensitiveColumns);
        AwansColumn.Visibility = ResolveOptionalColumnVisibility(
            GeneralViewColumnId.Awans,
            roleAllows: _controller.CanViewSensitiveColumns);
        OdznaczeniaColumn.Visibility = ResolveOptionalColumnVisibility(
            GeneralViewColumnId.Odznaczenia,
            roleAllows: _controller.CanViewSensitiveColumns);
        InneUwagiColumn.Visibility = _controller.ShowInneUwagiColumn
            ? ResolveOptionalColumnVisibility(GeneralViewColumnId.InneUwagi)
            : hidden;

        ApplyPermissionFilterOverrides();
    }

    private Visibility ResolveOptionalColumnVisibility(
        GeneralViewColumnId columnId,
        bool roleAllows = true)
    {
        if (!roleAllows)
        {
            return Visibility.Collapsed;
        }

        if (_controller.CanCustomizeGeneralViewColumns && !_columnPreferences.IsVisible(columnId))
        {
            return Visibility.Collapsed;
        }

        return Visibility.Visible;
    }

    private void ApplyPermissionFilterOverrides()
    {
        var selected = GetSelectedPermissionFilters();
        var hidden = Visibility.Collapsed;
        var visible = Visibility.Visible;
        var singlePermission = selected.Count == 1 ? selected[0] : null;

        SelectedPermissionValidityColumn.Visibility = singlePermission is null ? hidden : visible;
        SelectedPermissionValidityColumn.Header = singlePermission is null
            ? "Ważne do"
            : $"Ważne do ({singlePermission.Label})";

        if (!_controller.IsPaUser || selected.Count == 0)
        {
            return;
        }

        StanowiskoColumn.Visibility = hidden;
        BadaniaColumn.Visibility = hidden;
        KomoraColumn.Visibility = hidden;
        KppColumn.Visibility = hidden;
        WstepienieColumn.Visibility = hidden;
        UprawnieniaColumn.Visibility = hidden;
    }

    private async Task EnsureStopnieResourceAsync()
    {
        if (!_controller.CanEditGeneralViewStopien || _stopnieLoaded)
        {
            return;
        }

        Resources["StopnieList"] = await _controller.GetStopnieAsync();
        _stopnieLoaded = true;
    }

    private async void OnViewLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnViewLoaded;
        try
        {
            if (ShiftFilterComboBox.ItemsSource is null)
            {
                await InitializeFiltersAsync();
            }

            await LoadColumnPreferencesAsync();
            await EnsureStopnieResourceAsync();
            await LoadPersonnelAsync();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Błąd: {ex.Message}";
            LoadFailed?.Invoke(this, $"Nie można załadować widoku ogólnego:\n\n{ex.Message}");
        }
    }

    public async Task InitializeFiltersAsync()
    {
        _isInitializingFilters = true;
        try
        {
            var shiftOptions = await _controller.GetShiftFilterOptionsAsync();
            ShiftFilterComboBox.ItemsSource = shiftOptions;
            ShiftFilterComboBox.SelectedIndex = shiftOptions.Count > 0 ? 0 : -1;

            foreach (var option in _permissionFilterOptions)
            {
                option.PropertyChanged -= OnPermissionFilterOptionChanged;
            }

            _permissionFilterOptions = (await _controller.GetPermissionFilterOptionsAsync()).ToList();
            foreach (var option in _permissionFilterOptions)
            {
                option.PropertyChanged += OnPermissionFilterOptionChanged;
            }

            PermissionFilterList.ItemsSource = _permissionFilterOptions;
            UpdatePermissionFilterSummary();
            UpdateColumnsForPermissionFilter();
        }
        finally
        {
            _isInitializingFilters = false;
        }
    }

    public async Task LoadPersonnelAsync(CancellationToken cancellationToken = default)
    {
        var generation = ++_loadGeneration;
        StatusTextBlock.Text = "Ładowanie danych z bazy...";
        _isLoading = true;
        _suppressGridEditEvents = true;
        var totalStopwatch = Stopwatch.StartNew();
        var filter = BuildFilter();
        PersonnelLoadResult? loadResult = null;
        try
        {
            await Task.Yield();
            loadResult = await _controller.LoadPersonnelAsync(filter, cancellationToken).ConfigureAwait(true);
            if (generation != _loadGeneration)
                return;

            PersonnelDataGrid.ItemsSource = loadResult.Rows;
            PersonnelDataGrid.SelectedIndex = -1;
            await WaitForGridLayoutAsync();
            totalStopwatch.Stop();
            StatusTextBlock.Text = FormatLoadStatusMessage(loadResult, totalStopwatch.Elapsed.TotalSeconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Nowsza wartość filtra zastąpiła ten odczyt.
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            _controller.InvalidatePersonnelCache();
            PersonnelDataGrid.ItemsSource = null;
            StatusTextBlock.Text = $"Błąd bazy danych: {ex.Message}";
        }
        finally
        {
            if (generation == _loadGeneration)
            {
                _isLoading = false;
                _suppressGridEditEvents = false;
            }
        }
    }

    private static string FormatLoadStatusMessage(PersonnelLoadResult loadResult, double totalSeconds)
    {
        if (loadResult.FromCache)
        {
            return $"Wyświetlono {loadResult.Rows.Count} osób (pamięć podręczna, łącznie {totalSeconds:F1} s).";
        }

        var uiSeconds = Math.Max(
            0,
            totalSeconds - loadResult.DatabaseSeconds - loadResult.MappingSeconds);
        return
            $"Wyświetlono {loadResult.Rows.Count} osób. Czas: baza {loadResult.DatabaseSeconds:F1} s, " +
            $"przygotowanie {loadResult.MappingSeconds:F1} s, UI {uiSeconds:F1} s, łącznie {totalSeconds:F1} s.";
    }

    private Task WaitForGridLayoutAsync()
    {
        var completion = new TaskCompletionSource();
        Dispatcher.BeginInvoke(() => completion.SetResult(), DispatcherPriority.ApplicationIdle);
        return completion.Task;
    }

    public ShiftFilterOption GetCurrentShiftFilter()
    {
        if (ShiftFilterComboBox.SelectedItem is ShiftFilterOption option)
        {
            return option;
        }

        return new ShiftFilterOption { Label = "Wszystkie zmiany", Value = null };
    }

    private FunkcjonariuszRowFilter BuildFilter()
    {
        var shiftItem = ShiftFilterComboBox.SelectedItem as ShiftFilterOption;
        return new FunkcjonariuszRowFilter
        {
            NumerZmiany = shiftItem?.Value,
            Uprawnienia = GetSelectedPermissionFilters()
                .Select(option => new UprawnienieTypFiltr
                {
                    Nazwa = option.Nazwa!,
                    Podtyp = option.Podtyp
                })
                .ToList(),
            LaczenieUprawnien = PermissionAndRadio.IsChecked == true
                ? SposobLaczeniaUprawnien.I
                : SposobLaczeniaUprawnien.Lub,
            Szukaj = SearchTextBox.Text
        };
    }

    private List<PermissionFilterOption> GetSelectedPermissionFilters() =>
        _permissionFilterOptions
            .Where(option => option.CzyWybrane && !string.IsNullOrWhiteSpace(option.Nazwa))
            .ToList();

    private void UpdatePermissionFilterSummary()
    {
        var selected = GetSelectedPermissionFilters();
        var lacznik = PermissionAndRadio.IsChecked == true ? " i " : " lub ";
        var summary = selected.Count switch
        {
            0 => "Wszystkie uprawnienia",
            1 => selected[0].Label,
            _ => string.Join(lacznik, selected.Select(option => option.Label))
        };

        PermissionFilterToggle.ApplyTemplate();
        if (PermissionFilterToggle.Template.FindName("PermissionFilterSummary", PermissionFilterToggle) is TextBlock summaryText)
        {
            summaryText.Text = summary;
            summaryText.ToolTip = summary;
        }
    }

    private async void OnPermissionFilterOptionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PermissionFilterOption.CzyWybrane)
            || _suppressPermissionFilterEvents
            || _isInitializingFilters)
        {
            return;
        }

        UpdatePermissionFilterSummary();
        UpdateColumnsForPermissionFilter();
        if (IsLoaded)
        {
            await LoadPersonnelAsync();
        }
    }

    private async void OnPermissionJoinChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializingFilters || !IsLoaded || PermissionOrRadio is null || PermissionAndRadio is null)
        {
            return;
        }

        UpdatePermissionFilterSummary();
        if (GetSelectedPermissionFilters().Count > 1)
        {
            await LoadPersonnelAsync();
        }
    }

    private async void OnClearPermissionFilterClick(object sender, RoutedEventArgs e)
    {
        _suppressPermissionFilterEvents = true;
        try
        {
            foreach (var option in _permissionFilterOptions)
            {
                option.CzyWybrane = false;
            }
        }
        finally
        {
            _suppressPermissionFilterEvents = false;
        }

        UpdatePermissionFilterSummary();
        UpdateColumnsForPermissionFilter();
        if (IsLoaded && !_isInitializingFilters)
        {
            await LoadPersonnelAsync();
        }
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        _controller.InvalidatePersonnelCache();
        await LoadPersonnelAsync();
    }

    private async void OnFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateColumnsForPermissionFilter();
        if (IsLoaded && !_isInitializingFilters)
        {
            await LoadPersonnelAsync();
        }
    }

    private void UpdateColumnsForPermissionFilter() => ApplyAllColumnVisibility();

    private void OnPersonnelGridPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        TryOpenPersonnelEdit(e);

    private void TryOpenPersonnelEdit(MouseButtonEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source
            && (FindVisualParent<DatePicker>(source) is not null
                || FindVisualParent<ComboBox>(source) is not null
                || FindVisualParent<Button>(source) is not null))
        {
            return;
        }

        var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject)?.Item as FunkcjonariuszRowViewModel
            ?? PersonnelDataGrid.SelectedItem as FunkcjonariuszRowViewModel;

        if (row is null)
        {
            return;
        }

        PersonnelDataGrid.SelectedItem = row;

        if (_controller.CanOpenPersonnelProfile)
        {
            PersonnelProfileRequested?.Invoke(this, row.FunkcjonariuszId);
            e.Handled = true;
            return;
        }

        if (!_controller.CanEditPersonnel)
        {
            return;
        }

        PersonnelEditRequested?.Invoke(this, row.FunkcjonariuszId);
        e.Handled = true;
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match)
            {
                return match;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }

    private async void OnFilterTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateClearSearchButtonVisibility();
        if (!IsLoaded)
            return;

        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        var cancellationToken = _searchCancellation.Token;

        try
        {
            await Task.Delay(SearchDebounceMilliseconds, cancellationToken);
            await LoadPersonnelAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Kolejny znak uruchomi nowy, pojedynczy odczyt.
        }
    }

    private void OnViewUnloaded(object sender, RoutedEventArgs e)
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = null;
    }

    private void OnClearSearchClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SearchTextBox.Text))
        {
            return;
        }

        SearchTextBox.Clear();
        SearchTextBox.Focus();
    }

    private void UpdateClearSearchButtonVisibility() =>
        ClearSearchButton.Visibility = string.IsNullOrWhiteSpace(SearchTextBox.Text)
            ? Visibility.Collapsed
            : Visibility.Visible;

    private async void OnStopienSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0
            || _isLoading
            || _isSaving
            || _suppressGridEditEvents
            || !_controller.CanEditGeneralViewStopien
            || sender is not ComboBox { DataContext: FunkcjonariuszRowViewModel row })
        {
            return;
        }

        if (!row.CanEditStopien || sender is not ComboBox combo || combo.SelectedValue is not int stopienId || stopienId <= 0)
        {
            return;
        }

        var nazwa = (combo.SelectedItem as SlownikItem)?.Nazwa;
        _isSaving = true;
        try
        {
            await _controller.SaveStopienAsync(row, stopienId);
            row.StopienId = stopienId;
            if (!string.IsNullOrWhiteSpace(nazwa))
            {
                row.Stopien = nazwa;
            }

            StatusTextBlock.Text = "Zapisano stopień.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Błąd zapisu: {ex.Message}";
            _controller.InvalidatePersonnelCache();
            await LoadPersonnelAsync();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async void OnZmianaSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0
            || _isLoading
            || _isSaving
            || _suppressGridEditEvents
            || !_controller.CanEditGeneralViewShift
            || sender is not ComboBox { DataContext: FunkcjonariuszRowViewModel row })
        {
            return;
        }

        if (!row.CanEditZmiana || sender is not ComboBox combo || combo.SelectedItem is not int numerZmiany
            || numerZmiany is < 1 or > 3)
        {
            return;
        }

        _isSaving = true;
        try
        {
            await _controller.SaveNumerZmianyAsync(row, numerZmiany);
            row.NumerZmiany = numerZmiany;
            StatusTextBlock.Text = "Zapisano numer zmiany.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Błąd zapisu: {ex.Message}";
            _controller.InvalidatePersonnelCache();
            await LoadPersonnelAsync();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async void OnBadaniaDateChanged(object sender, SelectionChangedEventArgs e) =>
        await SaveRowDateAsync(sender, e, row => _controller.SaveTerminyMedyczneAsync(row));

    private async void OnKomoraDateChanged(object sender, SelectionChangedEventArgs e) =>
        await SaveRowDateAsync(sender, e, row => _controller.SaveTerminyMedyczneAsync(row));

    private async void OnKppDateChanged(object sender, SelectionChangedEventArgs e) =>
        await SaveRowDateAsync(sender, e, row => _controller.SaveTerminyMedyczneAsync(row));

    private async void OnSelectedPermissionDateChanged(object sender, SelectionChangedEventArgs e) =>
        await SaveRowDateAsync(sender, e, row => _controller.SaveUprawnienieWazneDoAsync(row));

    private async Task SaveRowDateAsync(
        object sender,
        SelectionChangedEventArgs selectionChanged,
        Func<FunkcjonariuszRowViewModel, Task> saveAction)
    {
        if (selectionChanged.AddedItems.Count == 0
            || _isLoading
            || _isSaving
            || _suppressGridEditEvents
            || !_controller.CanEditGeneralViewDates)
        {
            return;
        }

        if (sender is not DatePicker { DataContext: FunkcjonariuszRowViewModel row })
        {
            return;
        }

        _isSaving = true;
        try
        {
            await saveAction(row);
            StatusTextBlock.Text = "Zapisano zmianę daty.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Błąd zapisu: {ex.Message}";
            _controller.InvalidatePersonnelCache();
            await LoadPersonnelAsync();
        }
        finally
        {
            _isSaving = false;
        }
    }
}
