using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BOBER.App.Controllers;
using BOBER.App.Logging;
using BOBER.App.ViewModels;
using BOBER.App.Views.Chrome;
using BOBER.Core.Models;

namespace BOBER.App.Views;

public partial class GrafikZliczanieSettingsView : UserControl
{
    private readonly SettingsController _controller;
    private readonly ObservableCollection<GrafikZliczanieListItem> _items = [];
    private IReadOnlyList<GrafikZliczanieSlownikPozycja> _uprawnienia = [];
    private IReadOnlyList<GrafikZliczanieSlownikPozycja> _stanowiska = [];
    private Dictionary<int, string> _etykietyUprawnien = [];
    private Dictionary<int, string> _etykietyStanowisk = [];

    public event EventHandler? SettingsSaved;

    public GrafikZliczanieSettingsView(SettingsController controller)
    {
        InitializeComponent();
        _controller = controller;
        WierszeGrid.ItemsSource = _items;
        WierszeGrid.MouseDoubleClick += (_, _) => OnEditClick(this, new RoutedEventArgs());
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _uprawnienia = await _controller.GetTypyUprawnienZliczaniaAsync();
            _stanowiska = await _controller.GetStanowiskaZliczaniaAsync();
            _etykietyUprawnien = _uprawnienia.ToDictionary(p => p.Id, p => p.Etykieta);
            _etykietyStanowisk = _stanowiska.ToDictionary(p => p.Id, p => p.Etykieta);

            var wiersze = await _controller.GetZliczanieAsync();
            RebuildList(wiersze);
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(GetOwnerWindow(), ex, "Błąd wczytywania zliczania grafiku");
        }
    }

    private void RebuildList(IEnumerable<GrafikZliczanieWiersz> wiersze)
    {
        _items.Clear();
        _items.Add(GrafikZliczanieListItem.WolneMiejsca());
        foreach (var wiersz in wiersze.OrderBy(w => w.Kolejnosc))
            _items.Add(GrafikZliczanieListItem.ZWiersza(wiersz, _etykietyUprawnien, _etykietyStanowisk));
    }

    private IEnumerable<GrafikZliczanieWiersz> Edytowalne() =>
        _items.Where(i => !i.IsFixed && i.Wiersz is not null).Select(i => i.Wiersz!);

    private void OnAddClick(object sender, RoutedEventArgs e) => OpenEditor(existing: null);

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (WierszeGrid.SelectedItem is not GrafikZliczanieListItem item)
        {
            BoberMessageBox.Show(GetOwnerWindow(), "Zaznacz wiersz do edycji.", "Zliczanie grafiku");
            return;
        }

        if (item.IsFixed)
        {
            BoberMessageBox.Show(GetOwnerWindow(), "„Wolne miejsca” nie podlega edycji.", "Zliczanie grafiku");
            return;
        }

        OpenEditor(item.Wiersz);
    }

    private void OpenEditor(GrafikZliczanieWiersz? existing)
    {
        var dialog = new GrafikZliczanieEditorDialog(
            existing,
            _uprawnienia,
            _stanowiska,
            Edytowalne().Select(w => w.Nazwa))
        {
            Owner = GetOwnerWindow()
        };
        if (dialog.ShowDialog() != true || dialog.Result is null)
            return;

        if (existing is null)
        {
            dialog.Result.Kolejnosc = (short)(Edytowalne().Count() + 1);
            _items.Add(GrafikZliczanieListItem.ZWiersza(dialog.Result, _etykietyUprawnien, _etykietyStanowisk));
            return;
        }

        var index = _items.ToList().FindIndex(i => ReferenceEquals(i.Wiersz, existing));
        if (index < 0)
            return;
        dialog.Result.Kolejnosc = existing.Kolejnosc;
        _items[index] = GrafikZliczanieListItem.ZWiersza(dialog.Result, _etykietyUprawnien, _etykietyStanowisk);
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (WierszeGrid.SelectedItem is not GrafikZliczanieListItem item || item.IsFixed)
        {
            BoberMessageBox.Show(GetOwnerWindow(), "Zaznacz wiersz zliczania do usunięcia.", "Zliczanie grafiku");
            return;
        }

        var confirm = BoberMessageBox.Show(
            GetOwnerWindow(),
            $"Usunąć wiersz „{item.Nazwa}”?",
            "Zliczanie grafiku",
            BoberMessageButtons.YesNo);
        if (confirm != MessageBoxResult.Yes)
            return;

        _items.Remove(item);
    }

    private void OnMoveUpClick(object sender, RoutedEventArgs e) => MoveSelected(-1);

    private void OnMoveDownClick(object sender, RoutedEventArgs e) => MoveSelected(1);

    private void MoveSelected(int delta)
    {
        if (WierszeGrid.SelectedItem is not GrafikZliczanieListItem item || item.IsFixed)
            return;

        var index = _items.IndexOf(item);
        var target = index + delta;
        if (target <= 0 || target >= _items.Count)
            return;

        _items.Move(index, target);
        WierszeGrid.SelectedItem = item;
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await _controller.SaveZliczanieAsync(Edytowalne().ToList());
            BoberMessageBox.Show(GetOwnerWindow(), "Zliczanie grafiku zostało zapisane.", "Zliczanie grafiku");
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(GetOwnerWindow(), ex, "Błąd zapisu zliczania grafiku");
        }
    }

    private Window? GetOwnerWindow() => Window.GetWindow(this);
}
