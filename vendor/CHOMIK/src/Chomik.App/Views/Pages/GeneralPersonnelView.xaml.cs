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
    private bool _stopnieLoaded;
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
        var permissionItem = PermissionFilterComboBox.SelectedItem as PermissionFilterOption;
        var hasSelectedPermission = !string.IsNullOrWhiteSpace(permissionItem?.Nazwa);
        var hidden = Visibility.Collapsed;
        var visible = Visibility.Visible;

        SelectedPermissionValidityColumn.Visibility = hasSelectedPermission ? visible : hidden;
        SelectedPermissionValidityColumn.Header = hasSelectedPermission
            ? $"Ważne do ({permissionItem!.Label})"
            : "Ważne do";

        if (!_controller.IsPaUser || !hasSelectedPermission)
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

            var permissionOptions = await _controller.GetPermissionFilterOptionsAsync();
            PermissionFilterComboBox.ItemsSource = permissionOptions;
            PermissionFilterComboBox.SelectedIndex = permissionOptions.Count > 0 ? 0 : -1;
            UpdateColumnsForPermissionFilter();
        }
        finally
        {
            _isInitializingFilters = false;
        }
    }

    public async Task LoadPersonnelAsync()
    {
        StatusTextBlock.Text = "Ładowanie danych z bazy...";
        _isLoading = true;
        _suppressGridEditEvents = true;
        var totalStopwatch = Stopwatch.StartNew();
        var filter = BuildFilter();
        PersonnelLoadResult? loadResult = null;
        try
        {
            await Task.Yield();
            loadResult = await _controller.LoadPersonnelAsync(filter).ConfigureAwait(true);
            PersonnelDataGrid.ItemsSource = loadResult.Rows;
            PersonnelDataGrid.SelectedIndex = -1;
            await WaitForGridLayoutAsync();
            totalStopwatch.Stop();
            StatusTextBlock.Text = FormatLoadStatusMessage(loadResult, totalStopwatch.Elapsed.TotalSeconds);
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
            _isLoading = false;
            _suppressGridEditEvents = false;
        }
    }

    private static string FormatLoadStatusMessage(PersonnelLoadResult loadResult, double totalSeconds)
    {
        if (loadResult.FromCache)
        {
            return $"Wyświetlono {loadResult.Rows.Count} osób (pamięć podręczna, łącznie {totalSeconds:F1} s).";
        }

        return
            $"Wyświetlono {loadResult.Rows.Count} osób. Czas: baza {loadResult.DatabaseSeconds:F1} s, przygotowanie {loadResult.MappingSeconds:F1} s, łącznie {totalSeconds:F1} s.";
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
        var permissionItem = PermissionFilterComboBox.SelectedItem as PermissionFilterOption;
        return new FunkcjonariuszRowFilter
        {
            NumerZmiany = shiftItem?.Value,
            UprawnienieNazwa = permissionItem?.Nazwa,
            UprawnieniePodtyp = permissionItem?.Podtyp,
            Szukaj = SearchTextBox.Text
        };
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
        if (IsLoaded)
        {
            await LoadPersonnelAsync();
        }
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
