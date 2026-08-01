using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Chomik.App.Controllers;
using Chomik.App.Views;
using Chomik.App.Views.Chrome;
using Chomik.Core;
using Chomik.Core.Models;
using Chomik.Services.Personnel;

namespace Chomik.App.Views.Pages;

public partial class PersonnelManagementView : UserControl
{
    private readonly PersonnelManagementController _controller;
    private PersonnelDictionaries? _dictionaries;
    private bool _hasCompletedInitialLoad;

    public event EventHandler? PersonnelChanged;

    public PersonnelManagementView(PersonnelManagementController controller)
    {
        InitializeComponent();
        _controller = controller;
        Loaded += async (_, _) => await LoadAsync();
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private Window? OwnerWindow => Window.GetWindow(this);

    private async Task LoadAsync()
    {
        try
        {
            _dictionaries = await _controller.GetDictionariesAsync();
            var list = await _controller.LoadPersonnelAsync();
            PersonnelGrid.ItemsSource = list.Select(f => new PersonnelGridRow(f)).ToList();
            _hasCompletedInitialLoad = true;
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }

    public Task ReloadAsync() => LoadAsync();

    private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true || !_hasCompletedInitialLoad) return;
        await ReloadAsync();
    }

    private Funkcjonariusz? GetSelected() =>
        (PersonnelGrid.SelectedItem as PersonnelGridRow)?.Entity;

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (_dictionaries is null || OwnerWindow is null)
        {
            return;
        }

        var nextNumer = await _controller.GetNextNumerPorzadkowyAsync();
        var entity = new Funkcjonariusz
        {
            NumerZmiany = _controller.ShiftNumber,
            NumerPorzadkowy = nextNumer
        };
        var window = new PersonnelEditWindow(_controller, _dictionaries, entity);
        AppWindowLayout.ApplyDialog(window, OwnerWindow!);
        if (window.ShowDialog() == true)
        {
            await LoadAsync();
            PersonnelChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        var selected = GetSelected();
        if (selected is null)
        {
            ChomikMessageBox.Show(OwnerWindow, "Wybierz funkcjonariusza z listy.", "Informacja");
            return;
        }

        await OpenEditDialogAsync(selected);
    }

    private async void OnPersonnelGridPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source
            && FindVisualParent<Button>(source) is not null)
        {
            return;
        }

        var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject)?.Item as PersonnelGridRow
            ?? PersonnelGrid.SelectedItem as PersonnelGridRow;
        if (row is null)
        {
            return;
        }

        PersonnelGrid.SelectedItem = row;
        e.Handled = true;
        await OpenEditDialogAsync(row.Entity);
    }

    private async Task OpenEditDialogAsync(Funkcjonariusz selected)
    {
        var entity = await _controller.GetForEditAsync(selected.Id);
        if (entity is null || _dictionaries is null || OwnerWindow is null)
        {
            return;
        }

        var window = new PersonnelEditWindow(_controller, _dictionaries, entity);
        AppWindowLayout.ApplyDialog(window, OwnerWindow);
        if (window.ShowDialog() == true)
        {
            await LoadAsync();
            PersonnelChanged?.Invoke(this, EventArgs.Empty);
        }
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

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        var selected = GetSelected();
        if (selected is null)
        {
            ChomikMessageBox.Show(OwnerWindow, "Wybierz funkcjonariusza z listy.", "Informacja");
            return;
        }

        if (ChomikMessageBox.Show(
                OwnerWindow,
                $"Usunąć {selected.PelneImieNazwisko}?",
                "Potwierdzenie",
                ChomikMessageButtons.YesNo) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _controller.DeleteAsync(selected.Id);
            await LoadAsync();
            PersonnelChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }

    private sealed class PersonnelGridRow(Funkcjonariusz funkcjonariusz)
    {
        public Funkcjonariusz Entity { get; } = funkcjonariusz;
        public int NumerPorzadkowy => Entity.NumerPorzadkowy;
        public string Stopien => Entity.Stopien;
        public string PelneImieNazwisko => Entity.PelneImieNazwisko;
        public string Nazwisko => Entity.Nazwisko;
        public string Stanowisko => Entity.Stanowisko;
        public string? Telefon => Entity.Telefon;
        public string UprawnieniaSkrot => string.Join(", ", Entity.Uprawnienia.Select(u =>
            string.IsNullOrWhiteSpace(u.Podtyp) ? u.Nazwa : $"{u.Nazwa} {u.Podtyp}"));

        public string OdznaczeniaSkrot => string.Join(", ", Entity.Odznaczenia.Select(o =>
            $"{o.Nazwa} ({DateDisplayFormat.Format(o.DataNadania)})"));
    }
}
