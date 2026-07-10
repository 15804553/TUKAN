using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BOBER.App.Controllers;
using BOBER.App.Logging;
using BOBER.App.ViewModels;
using BOBER.App.Views.Chrome;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using MediaColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace BOBER.App.Views;

public partial class BoberSettingsView : UserControl
{
    private readonly SettingsController _controller;
    private readonly ObservableCollection<FunkcjonariuszListItem> _kolejnoscLista = new();
    private readonly ObservableCollection<KolorRoliViewModel> _kolory = new();
    private int _loadGeneration;

    public event EventHandler? SettingsSaved;
    public event EventHandler? CancelRequested;

    public bool ShowCancelButton
    {
        get => CancelButton.Visibility == Visibility.Visible;
        set => CancelButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    public BoberSettingsView(SettingsController controller)
    {
        InitializeComponent();
        _controller = controller;

        FunkcjonariuszeListBox.ItemsSource = _kolejnoscLista;
        KoloryItemsControl.ItemsSource = _kolory;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var generation = ++_loadGeneration;
        _kolejnoscLista.Clear();
        _kolory.Clear();

        try
        {
            var kolejnosc = await _controller.GetFunkcjonariuszeAsync();
            if (generation != _loadGeneration)
            {
                return;
            }

            foreach (var f in kolejnosc)
            {
                _kolejnoscLista.Add(new FunkcjonariuszListItem
                {
                    Id = f.Id,
                    ImieNazwisko = f.PelneImieNazwisko,
                    Stanowisko = f.Stanowisko
                });
            }
            UpdateNumbers();

            var kolory = await _controller.GetKoloryAsync();
            if (generation != _loadGeneration)
            {
                return;
            }

            var koloryDict = kolory
                .GroupBy(k => k.KluczRoli)
                .ToDictionary(g => g.Key, g => g.First().KolorHex);
            foreach (var (klucz, etykieta) in _controller.GetKolorKeys())
            {
                if (_kolory.Any(k => k.KluczRoli == klucz))
                {
                    continue;
                }

                var domyslny = RoleKeys.GetDefaultKolorHex(klucz);

                _kolory.Add(new KolorRoliViewModel
                {
                    KluczRoli = klucz,
                    Etykieta = etykieta,
                    KolorHex = koloryDict.TryGetValue(klucz, out var zapisany) ? zapisany : domyslny
                });
            }

            NazwaZmianyHeader.Text = _controller.NazwaZmiany;
            StanZmianyTextBox.Text = (await _controller.GetStanZmianyAsync(_controller.ZmianaId)).ToString();
            StanMinimalnyTextBox.Text = (await _controller.GetStanMinimalnyAsync(_controller.ZmianaId)).ToString();
        }
        catch (Exception ex)
        {
            if (generation == _loadGeneration)
            {
                UiErrorReporter.Show(GetOwnerWindow(), ex, "Błąd ładowania ustawień");
            }
        }
    }

    private void OnMoveUpClick(object sender, RoutedEventArgs e)
    {
        if (FunkcjonariuszeListBox.SelectedItem is not FunkcjonariuszListItem item)
        {
            return;
        }

        var index = _kolejnoscLista.IndexOf(item);
        if (index <= 0)
        {
            return;
        }

        _kolejnoscLista.Move(index, index - 1);
        UpdateNumbers();
        FunkcjonariuszeListBox.SelectedItem = item;
    }

    private void OnMoveDownClick(object sender, RoutedEventArgs e)
    {
        if (FunkcjonariuszeListBox.SelectedItem is not FunkcjonariuszListItem item)
        {
            return;
        }

        var index = _kolejnoscLista.IndexOf(item);
        if (index < 0 || index >= _kolejnoscLista.Count - 1)
        {
            return;
        }

        _kolejnoscLista.Move(index, index + 1);
        UpdateNumbers();
        FunkcjonariuszeListBox.SelectedItem = item;
    }

    private void UpdateNumbers()
    {
        for (var i = 0; i < _kolejnoscLista.Count; i++)
        {
            _kolejnoscLista[i].Numer = i + 1;
        }
    }

    private void OnColorPreviewClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not KolorRoliViewModel vm)
        {
            return;
        }

        var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            SolidColorOnly = true
        };

        try
        {
            var color = (MediaColor)WpfColorConverter.ConvertFromString(vm.KolorHex)!;
            dialog.Color = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
        }
        catch
        {
            dialog.Color = System.Drawing.Color.FromArgb(0x2D, 0x2D, 0x2D);
        }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var wybrany = dialog.Color;
        vm.KolorHex = $"#{wybrany.R:X2}{wybrany.G:X2}{wybrany.B:X2}";
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (int.TryParse(StanZmianyTextBox.Text, out var stanZmiany))
            {
                await _controller.SetStanZmianyAsync(_controller.ZmianaId, stanZmiany);
            }

            if (int.TryParse(StanMinimalnyTextBox.Text, out var stanMin))
            {
                await _controller.SetStanMinimalnyAsync(_controller.ZmianaId, stanMin);
            }

            var kolejnosc = _kolejnoscLista.Select(f => f.Id).ToList();
            await _controller.SaveKolejnoscAsync(kolejnosc);

            var kolory = _kolory.Select(k => new KolorStanowiska
            {
                KluczRoli = k.KluczRoli,
                KolorHex = k.KolorHex
            }).ToList();
            await _controller.SaveKoloryAsync(kolory);

            var chomikWarning = await _controller.TrySyncNrToChomikAsync(kolejnosc);
            if (chomikWarning is not null)
            {
                BoberMessageBox.Show(GetOwnerWindow(), chomikWarning, "BOBER — ostrzeżenie");
            }

            if (!ShowCancelButton)
            {
                BoberMessageBox.Show(GetOwnerWindow(), "Ustawienia zostały zapisane.", "BOBER");
            }

            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(GetOwnerWindow(), ex, "Błąd zapisu ustawień");
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void OnGenerateScheduleClick(object sender, RoutedEventArgs e)
    {
        var result = BoberMessageBox.Show(
            GetOwnerWindow(),
            "Zostanie utworzony nowy grafik (nowe daty służby) ale wpisy w grafiku pozostają niezmienione. Jeżeli chcesz je wyczyścić użyj przycisków Wyczyść półrocze.\n\nCzy kontynuować?",
            "BOBER",
            BoberMessageButtons.YesNo);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _controller.GenerateBaseScheduleAsync(DateTime.Today.Year);
            BoberMessageBox.Show(GetOwnerWindow(), "Grafik bazowy został przygotowany.", "BOBER");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(GetOwnerWindow(), ex, "Błąd przygotowania grafiku bazowego");
        }
    }

    private async void OnClearH1Click(object sender, RoutedEventArgs e)
    {
        var result = BoberMessageBox.Show(
            GetOwnerWindow(),
            "Czy wyczyścić wszystkie wpisy z I półrocza (Styczeń–Czerwiec)?",
            "BOBER",
            BoberMessageButtons.YesNo);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _controller.ClearHalfYearAsync(1);
            BoberMessageBox.Show(GetOwnerWindow(), "I półrocze zostało wyczyszczone.", "BOBER");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(GetOwnerWindow(), ex, "Błąd czyszczenia półrocza");
        }
    }

    private async void OnClearH2Click(object sender, RoutedEventArgs e)
    {
        var result = BoberMessageBox.Show(
            GetOwnerWindow(),
            "Czy wyczyścić wszystkie wpisy z II półrocza (Lipiec–Grudzień)?",
            "BOBER",
            BoberMessageButtons.YesNo);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _controller.ClearHalfYearAsync(2);
            BoberMessageBox.Show(GetOwnerWindow(), "II półrocze zostało wyczyszczone.", "BOBER");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(GetOwnerWindow(), ex, "Błąd czyszczenia półrocza");
        }
    }

    private Window? GetOwnerWindow() => Window.GetWindow(this);
}
