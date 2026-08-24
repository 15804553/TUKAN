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
    private readonly BoberSettingsSection _section;
    private readonly ObservableCollection<FunkcjonariuszListItem> _kolejnoscLista = new();
    private readonly ObservableCollection<KolorRoliViewModel> _kolory = new();
    private readonly ObservableCollection<KolorRoliViewModel> _koloryZmian = new();
    private readonly ObservableCollection<KolorRoliViewModel> _koloryEksportu = new();
    private int _loadGeneration;

    public event EventHandler? SettingsSaved;
    public event EventHandler? CancelRequested;

    public bool ShowCancelButton
    {
        get => CancelButton.Visibility == Visibility.Visible;
        set => CancelButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    public void CollapseExpanders()
    {
        EksportGrafikuExpander.IsExpanded = false;
        KolorowanieGrafikowExpander.IsExpanded = false;
        KoloryZmianExpander.IsExpanded = false;
    }

    private bool IncludesParametry =>
        _section is BoberSettingsSection.All or BoberSettingsSection.ParametryZmiany;

    private bool IncludesKolejnosc =>
        _section is BoberSettingsSection.All or BoberSettingsSection.Kolejnosc;

    private bool IncludesKolory =>
        _section is BoberSettingsSection.All or BoberSettingsSection.Grafik;

    public BoberSettingsView(
        SettingsController controller,
        BoberSettingsSection section = BoberSettingsSection.All)
    {
        InitializeComponent();
        _controller = controller;
        _section = section;

        FunkcjonariuszeListBox.ItemsSource = _kolejnoscLista;
        KoloryItemsControl.ItemsSource = _kolory;
        KoloryZmianItemsControl.ItemsSource = _koloryZmian;
        KoloryEksportuItemsControl.ItemsSource = _koloryEksportu;

        ApplySectionLayout();
        Loaded += OnLoaded;
    }

    private static string EtykietaKoloruZmiany(string klucz, string etykieta) => klucz switch
    {
        RoleKeys.KalendarzZmiana1 => "Zmiana 1",
        RoleKeys.KalendarzZmiana2 => "Zmiana 2",
        RoleKeys.KalendarzZmiana3 => "Zmiana 3",
        _ => etykieta
    };

    private ObservableCollection<KolorRoliViewModel> ListaKoloru(string klucz)
    {
        if (RoleKeys.KalendarzKolory.Contains(klucz))
            return _koloryZmian;
        if (RoleKeys.KoloryEksportu.Contains(klucz))
            return _koloryEksportu;
        return _kolory;
    }

    private void ApplySectionLayout()
    {
        if (_section == BoberSettingsSection.All)
            return;

        ParametryZmianySection.Visibility = IncludesParametry ? Visibility.Visible : Visibility.Collapsed;
        KolejnoscSection.Visibility = IncludesKolejnosc ? Visibility.Visible : Visibility.Collapsed;
        KolorySection.Visibility = IncludesKolory ? Visibility.Visible : Visibility.Collapsed;
        if (!IncludesKolory)
            GrafikManagementSection.Visibility = Visibility.Collapsed;

        if (_section is BoberSettingsSection.ParametryZmiany or BoberSettingsSection.Kolejnosc)
        {
            ParametryZmianyHeader.Visibility = Visibility.Collapsed;
            KolejnoscHeader.Visibility = Visibility.Collapsed;
            SectionsPanel.Margin = new Thickness(0, 4, 0, 4);
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var generation = ++_loadGeneration;
        _kolejnoscLista.Clear();
        _kolory.Clear();
        _koloryZmian.Clear();
        _koloryEksportu.Clear();

        try
        {
            if (IncludesKolejnosc)
            {
                var kolejnosc = await _controller.GetFunkcjonariuszeAsync();
                if (generation != _loadGeneration)
                    return;

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
            }

            if (IncludesKolory)
            {
                var kolory = await _controller.GetKoloryAsync();
                if (generation != _loadGeneration)
                    return;

                var koloryDict = kolory
                    .GroupBy(k => k.KluczRoli)
                    .ToDictionary(g => g.Key, g => g.First().KolorHex);
                foreach (var (klucz, etykieta) in _controller.GetKolorKeys())
                {
                    var lista = ListaKoloru(klucz);
                    if (lista.Any(k => k.KluczRoli == klucz))
                        continue;

                    var domyslny = RoleKeys.GetDefaultKolorHex(klucz);
                    var zapisanyHex = koloryDict.TryGetValue(klucz, out var zapisany) ? zapisany : domyslny;
                    var allowEmpty = RoleKeys.KoloryOpcjonalneWypelnienia.Contains(klucz);

                    lista.Add(new KolorRoliViewModel
                    {
                        KluczRoli = klucz,
                        Etykieta = EtykietaKoloruZmiany(klucz, etykieta),
                        AllowEmpty = allowEmpty,
                        KolorHex = allowEmpty
                            ? RoleKeys.NormalizeKolorHex(zapisanyHex, klucz)
                            : (RoleKeys.IsBrakWypelnienia(zapisanyHex) ? domyslny : zapisanyHex)
                    });
                }

                LessColorCheckBox.IsChecked = await _controller.GetLessColorAsync();

                var rowColors = await _controller.GetGrafikRowColorSettingsAsync();
                if (generation != _loadGeneration)
                    return;

                AlternatingColorsCheckBox.IsChecked = rowColors.Mode == GrafikRowColorMode.Alternating;
                AltColorATextBox.Text = rowColors.ColorA;
                AltColorBTextBox.Text = rowColors.ColorB;
                UpdateAlternatingColorsPanel();
                RefreshAltColorPreview(AltColorAPreview, AltColorATextBox.Text);
                RefreshAltColorPreview(AltColorBPreview, AltColorBTextBox.Text);

                var showGrafikMgmt = await _controller.CanShowGrafikManagementAsync();
                if (generation != _loadGeneration)
                    return;
                GrafikManagementSection.Visibility = showGrafikMgmt
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            if (IncludesParametry)
            {
                NazwaZmianyHeader.Text = _controller.NazwaZmiany;
                StanZmianyTextBox.Text = (await _controller.GetStanZmianyAsync(_controller.ZmianaId)).ToString();
                StanMinimalnyTextBox.Text = (await _controller.GetStanMinimalnyAsync(_controller.ZmianaId)).ToString();
                MaxUrlopowNaSluzbieTextBox.Text =
                    (await _controller.GetMaxUrlopowNaSluzbieAsync(_controller.ZmianaId)).ToString();
            }
        }
        catch (Exception ex)
        {
            if (generation == _loadGeneration)
            {
                UiErrorReporter.Show(GetOwnerWindow(), ex, "Błąd ładowania ustawień");
            }
        }
    }

    private void OnAlternatingColorsChanged(object sender, RoutedEventArgs e) =>
        UpdateAlternatingColorsPanel();

    private void UpdateAlternatingColorsPanel() =>
        AlternatingColorsPanel.IsEnabled = AlternatingColorsCheckBox.IsChecked == true;

    private void OnAltColorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender == AltColorATextBox)
            RefreshAltColorPreview(AltColorAPreview, AltColorATextBox.Text);
        else if (sender == AltColorBTextBox)
            RefreshAltColorPreview(AltColorBPreview, AltColorBTextBox.Text);
    }

    private void OnAltColorPreviewClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag })
            return;

        var textBox = tag == "B" ? AltColorBTextBox : AltColorATextBox;
        var preview = tag == "B" ? AltColorBPreview : AltColorAPreview;
        var chosen = PickColor(textBox.Text);
        if (chosen is null)
            return;

        textBox.Text = chosen;
        RefreshAltColorPreview(preview, chosen);
    }

    private string? PickColor(string currentHex)
    {
        var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            SolidColorOnly = true
        };

        try
        {
            var color = (MediaColor)WpfColorConverter.ConvertFromString(currentHex)!;
            dialog.Color = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
        }
        catch
        {
            dialog.Color = System.Drawing.Color.FromArgb(0x2D, 0x2D, 0x2D);
        }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return null;

        var wybrany = dialog.Color;
        return $"#{wybrany.R:X2}{wybrany.G:X2}{wybrany.B:X2}";
    }

    private static void RefreshAltColorPreview(Border preview, string hex)
    {
        try
        {
            var color = (MediaColor)WpfColorConverter.ConvertFromString(hex)!;
            preview.Background = new SolidColorBrush(color);
        }
        catch
        {
            preview.Background = new SolidColorBrush(MediaColor.FromRgb(0x2D, 0x2D, 0x2D));
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

        var startHex = vm.HasFill ? vm.KolorHex : RoleKeys.GetDefaultKolorHex(RoleKeys.WolnaSluzba);
        var chosen = PickColor(startHex);
        if (chosen is null)
            return;

        vm.KolorHex = chosen;
    }

    private void OnClearColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KolorRoliViewModel vm })
            vm.ClearFill();
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (IncludesParametry)
            {
                if (int.TryParse(StanZmianyTextBox.Text, out var stanZmiany))
                    await _controller.SetStanZmianyAsync(_controller.ZmianaId, stanZmiany);

                if (int.TryParse(StanMinimalnyTextBox.Text, out var stanMin))
                    await _controller.SetStanMinimalnyAsync(_controller.ZmianaId, stanMin);

                if (int.TryParse(MaxUrlopowNaSluzbieTextBox.Text, out var maxUrlopow))
                    await _controller.SetMaxUrlopowNaSluzbieAsync(_controller.ZmianaId, maxUrlopow);
            }

            if (IncludesKolejnosc)
            {
                var kolejnosc = _kolejnoscLista.Select(f => f.Id).ToList();
                await _controller.SaveKolejnoscAsync(kolejnosc);

                var chomikWarning = await _controller.TrySyncNrToChomikAsync(kolejnosc);
                if (chomikWarning is not null)
                    BoberMessageBox.Show(GetOwnerWindow(), chomikWarning, "BOBER — ostrzeżenie");
            }

            if (IncludesKolory)
            {
                var kolory = _kolory.Concat(_koloryZmian).Concat(_koloryEksportu).Select(k => new KolorStanowiska
                {
                    KluczRoli = k.KluczRoli,
                    KolorHex = RoleKeys.NormalizeKolorHex(k.KolorHex, k.KluczRoli)
                }).ToList();
                await _controller.SaveKoloryAsync(kolory);
                await _controller.SetLessColorAsync(LessColorCheckBox.IsChecked == true);
                await _controller.SetGrafikRowColorSettingsAsync(new GrafikRowColorSettings
                {
                    Mode = AlternatingColorsCheckBox.IsChecked == true
                        ? GrafikRowColorMode.Alternating
                        : GrafikRowColorMode.Role,
                    ColorA = string.IsNullOrWhiteSpace(AltColorATextBox.Text)
                        ? GrafikRowColorSettings.DefaultColorA
                        : AltColorATextBox.Text.Trim(),
                    ColorB = string.IsNullOrWhiteSpace(AltColorBTextBox.Text)
                        ? GrafikRowColorSettings.DefaultColorB
                        : AltColorBTextBox.Text.Trim()
                });
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
        if (!await EnsureGuestGrafikManagementAllowedAsync())
            return;

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
        if (!await EnsureGuestGrafikManagementAllowedAsync())
            return;

        var dialog = new ClearHalfYearDialog("I półrocza (Styczeń–Czerwiec)")
        {
            Owner = GetOwnerWindow()
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await _controller.ClearHalfYearAsync(1, dialog.AlsoClearUrlopPlan);
            BoberMessageBox.Show(GetOwnerWindow(), "I półrocze zostało wyczyszczone.", "BOBER");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(GetOwnerWindow(), ex, "Błąd czyszczenia półrocza");
        }
    }

    private async void OnClearH2Click(object sender, RoutedEventArgs e)
    {
        if (!await EnsureGuestGrafikManagementAllowedAsync())
            return;

        var dialog = new ClearHalfYearDialog("II półrocza (Lipiec–Grudzień)")
        {
            Owner = GetOwnerWindow()
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await _controller.ClearHalfYearAsync(2, dialog.AlsoClearUrlopPlan);
            BoberMessageBox.Show(GetOwnerWindow(), "II półrocze zostało wyczyszczone.", "BOBER");
        }
        catch (Exception ex)
        {
            UiErrorReporter.Show(GetOwnerWindow(), ex, "Błąd czyszczenia półrocza");
        }
    }

    private async Task<bool> EnsureGuestGrafikManagementAllowedAsync()
    {
        if (await _controller.CanShowGrafikManagementAsync())
            return true;

        BoberMessageBox.Show(
            GetOwnerWindow(),
            "Zarządzanie grafikiem jest wyłączone dla użytkownika Gość.",
            "BOBER");
        return false;
    }

    private Window? GetOwnerWindow() => Window.GetWindow(this);
}
