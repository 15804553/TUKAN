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
    private readonly ObservableCollection<KolorRoliViewModel> _koloryPelne = new();
    private readonly ObservableCollection<KolorRoliViewModel> _koloryZmian = new();
    private readonly ObservableCollection<KolorRoliViewModel> _koloryEksportu = new();
    private readonly ObservableCollection<KolorRoliViewModel> _koloryKomorek = new();
    private readonly ObservableCollection<OznaczenieGrafikuViewModel> _oznaczenia = new();
    private int _loadGeneration;

    private IEnumerable<KolorRoliViewModel> WszystkieKoloryVm =>
        _koloryPelne.Concat(_koloryZmian).Concat(_koloryEksportu).Concat(_koloryKomorek);

    public event EventHandler? SettingsSaved;
    public event EventHandler? CancelRequested;

    public bool ShowCancelButton
    {
        get => CancelButton.Visibility == Visibility.Visible;
        set => CancelButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    public void CollapseExpanders()
    {
        // Brak expanderów na karcie Kolorowanie.
    }

    private bool IncludesParametry =>
        _section is BoberSettingsSection.All or BoberSettingsSection.ParametryZmiany;

    private bool IncludesKolejnosc =>
        _section is BoberSettingsSection.All or BoberSettingsSection.Kolejnosc;

    private bool IncludesKolory =>
        _section is BoberSettingsSection.All or BoberSettingsSection.Grafik;

    private bool IncludesOznaczenia =>
        _section is BoberSettingsSection.All or BoberSettingsSection.Oznaczenia;

    private bool IncludesZarzadzanieGrafikiem =>
        _section is BoberSettingsSection.All or BoberSettingsSection.ZarzadzanieGrafikiem;

    private bool IncludesZliczanie =>
        _section is BoberSettingsSection.All or BoberSettingsSection.Zliczanie;

    public BoberSettingsView(
        SettingsController controller,
        BoberSettingsSection section = BoberSettingsSection.All)
    {
        InitializeComponent();
        _controller = controller;
        _section = section;

        FunkcjonariuszeListBox.ItemsSource = _kolejnoscLista;
        KoloryPelneItemsControl.ItemsSource = _koloryPelne;
        KoloryZmianItemsControl.ItemsSource = _koloryZmian;
        KoloryEksportuItemsControl.ItemsSource = _koloryEksportu;
        KoloryKomorekItemsControl.ItemsSource = _koloryKomorek;
        OznaczeniaDataGrid.ItemsSource = _oznaczenia;
        if (IncludesZliczanie)
        {
            var zliczanieView = new GrafikZliczanieSettingsView(_controller);
            zliczanieView.SettingsSaved += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
            ZliczanieHost.Content = zliczanieView;
        }

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

    private static ObservableCollection<KolorRoliViewModel> KolekcjaDlaKlucza(
        string klucz,
        ObservableCollection<KolorRoliViewModel> pelne,
        ObservableCollection<KolorRoliViewModel> zmian,
        ObservableCollection<KolorRoliViewModel> eksportu,
        ObservableCollection<KolorRoliViewModel> komorek)
    {
        if (RoleKeys.KalendarzKolory.Contains(klucz))
            return zmian;
        if (RoleKeys.KoloryEksportu.Contains(klucz))
            return eksportu;
        if (klucz is RoleKeys.Delegacja or RoleKeys.Szkolenie)
            return komorek;
        return pelne;
    }

    private void ClearKoloryCollections()
    {
        _koloryPelne.Clear();
        _koloryZmian.Clear();
        _koloryEksportu.Clear();
        _koloryKomorek.Clear();
    }

    private void ApplySectionLayout()
    {
        if (_section == BoberSettingsSection.All)
            return;

        ParametryZmianySection.Visibility = IncludesParametry ? Visibility.Visible : Visibility.Collapsed;
        KolejnoscSection.Visibility = IncludesKolejnosc ? Visibility.Visible : Visibility.Collapsed;
        KolorySection.Visibility = IncludesKolory ? Visibility.Visible : Visibility.Collapsed;
        OznaczeniaSection.Visibility = IncludesOznaczenia ? Visibility.Visible : Visibility.Collapsed;
        GrafikManagementSection.Visibility = IncludesZarzadzanieGrafikiem
            ? Visibility.Visible
            : Visibility.Collapsed;
        ZliczanieSection.Visibility = IncludesZliczanie ? Visibility.Visible : Visibility.Collapsed;

        if (_section is BoberSettingsSection.ParametryZmiany
            or BoberSettingsSection.Kolejnosc
            or BoberSettingsSection.ZarzadzanieGrafikiem
            or BoberSettingsSection.Oznaczenia)
        {
            ParametryZmianyHeader.Visibility = Visibility.Collapsed;
            KolejnoscHeader.Visibility = Visibility.Collapsed;
            GrafikManagementHeader.Visibility = Visibility.Collapsed;
            OznaczeniaHeader.Visibility = Visibility.Collapsed;
            SaveBar.Visibility = _section is BoberSettingsSection.ZarzadzanieGrafikiem
                ? Visibility.Collapsed
                : Visibility.Visible;
            SectionsPanel.Margin = new Thickness(0, 4, 0, 4);
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var generation = ++_loadGeneration;
        _kolejnoscLista.Clear();
        ClearKoloryCollections();
        _oznaczenia.Clear();

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
                    .ToDictionary(g => g.Key, g => g.First());
                foreach (var (klucz, etykieta) in _controller.GetKolorKeys())
                {
                    // D/WS konfiguruje się w oznaczeniach grafiku — nie pokazujemy go na liście kolorów.
                    if (klucz == RoleKeys.WolnaSluzba)
                        continue;

                    if (WszystkieKoloryVm.Any(k => k.KluczRoli == klucz))
                        continue;

                    var domyslny = RoleKeys.GetDefaultKolorHex(klucz);
                    koloryDict.TryGetValue(klucz, out var zapisany);
                    var zapisanyHex = zapisany?.KolorHex ?? domyslny;
                    var allowEmpty = RoleKeys.KoloryOpcjonalneWypelnienia.Contains(klucz);
                    var target = KolekcjaDlaKlucza(
                        klucz, _koloryPelne, _koloryZmian, _koloryEksportu, _koloryKomorek);

                    target.Add(new KolorRoliViewModel
                    {
                        KluczRoli = klucz,
                        Etykieta = EtykietaKoloruZmiany(klucz, etykieta),
                        AllowEmpty = allowEmpty,
                        Aktywny = zapisany?.Aktywny ?? true,
                        KolorHex = allowEmpty
                            ? RoleKeys.NormalizeKolorHex(zapisanyHex, klucz)
                            : (RoleKeys.IsBrakWypelnienia(zapisanyHex) ? domyslny : zapisanyHex)
                    });
                }

                LessColorCheckBox.IsChecked = await _controller.GetLessColorAsync();
                KolorowanieEdycjaPersoneluCheckBox.IsChecked =
                    await _controller.GetKolorowanieEdycjaPersoneluAsync();

                var rowColors = await _controller.GetGrafikRowColorSettingsAsync();
                if (generation != _loadGeneration)
                    return;

                if (rowColors.Mode == GrafikRowColorMode.Alternating)
                    AlternatingColorsRadio.IsChecked = true;
                else
                    RoleColorsRadio.IsChecked = true;
                AltColorATextBox.Text = rowColors.ColorA;
                AltColorBTextBox.Text = rowColors.ColorB;
                UpdateGrafikColorModePanels();
                RefreshAltColorPreview(AltColorAPreview, AltColorATextBox.Text);
                RefreshAltColorPreview(AltColorBPreview, AltColorBTextBox.Text);

                var exportAlt = await _controller.GetGrafikExportAlternatingSettingsAsync();
                if (generation != _loadGeneration)
                    return;

                ExportAlternatingColorsCheckBox.IsChecked = exportAlt.Enabled;
                ExportAltColorATextBox.Text = exportAlt.ColorA;
                ExportAltColorBTextBox.Text = exportAlt.ColorB;
                UpdateExportAlternatingColorsPanel();
                RefreshAltColorPreview(ExportAltColorAPreview, ExportAltColorATextBox.Text);
                RefreshAltColorPreview(ExportAltColorBPreview, ExportAltColorBTextBox.Text);
            }

            if (IncludesOznaczenia)
            {
                var oznaczenia = await _controller.GetOznaczeniaAsync();
                if (generation != _loadGeneration)
                    return;

                OznaczeniaHeader.Text = $"OZNACZENIA GRAFIKU — {_controller.NazwaZmiany}";
                OznaczeniaHintText.Text =
                    $"Konfiguracja tylko dla {_controller.NazwaZmiany}. Zmiany 1, 2 i 3 mają osobne oznaczenia.";

                foreach (var o in oznaczenia.OrderBy(x => x.Kolejnosc))
                    _oznaczenia.Add(OznaczenieGrafikuViewModel.FromModel(o));
            }

            if (IncludesZarzadzanieGrafikiem)
            {
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

    private void OnGrafikColorModeChanged(object sender, RoutedEventArgs e) =>
        UpdateGrafikColorModePanels();

    private void UpdateGrafikColorModePanels()
    {
        if (AlternatingColorsPanel is null || PelneKolorowaniePanel is null)
            return;

        var alternating = AlternatingColorsRadio.IsChecked == true;
        AlternatingColorsPanel.IsEnabled = alternating;
        PelneKolorowaniePanel.IsEnabled = !alternating;
    }

    private void OnExportAlternatingColorsChanged(object sender, RoutedEventArgs e) =>
        UpdateExportAlternatingColorsPanel();

    private void UpdateExportAlternatingColorsPanel()
    {
        if (ExportAlternatingColorsPanel is null)
            return;

        ExportAlternatingColorsPanel.IsEnabled = ExportAlternatingColorsCheckBox.IsChecked == true;
    }

    private void OnAltColorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender == AltColorATextBox)
            RefreshAltColorPreview(AltColorAPreview, AltColorATextBox.Text);
        else if (sender == AltColorBTextBox)
            RefreshAltColorPreview(AltColorBPreview, AltColorBTextBox.Text);
        else if (sender == ExportAltColorATextBox)
            RefreshAltColorPreview(ExportAltColorAPreview, ExportAltColorATextBox.Text);
        else if (sender == ExportAltColorBTextBox)
            RefreshAltColorPreview(ExportAltColorBPreview, ExportAltColorBTextBox.Text);
    }

    private void OnAltColorPreviewClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag })
            return;

        var (textBox, preview) = tag switch
        {
            "B" => (AltColorBTextBox, AltColorBPreview),
            "ExportA" => (ExportAltColorATextBox, ExportAltColorAPreview),
            "ExportB" => (ExportAltColorBTextBox, ExportAltColorBPreview),
            _ => (AltColorATextBox, AltColorAPreview)
        };
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
                var istniejace = await _controller.GetKoloryAsync();
                var kolory = WszystkieKoloryVm.Select(k => new KolorStanowiska
                {
                    KluczRoli = k.KluczRoli,
                    KolorHex = RoleKeys.NormalizeKolorHex(k.KolorHex, k.KluczRoli),
                    Aktywny = k.Aktywny
                }).ToList();

                // D/WS nie jest edytowane tutaj — zachowaj dotychczasowy rekord przy zapisie pełnej tabeli.
                var wolnaSluzba = istniejace.FirstOrDefault(k =>
                    k.KluczRoli.Equals(RoleKeys.WolnaSluzba, StringComparison.OrdinalIgnoreCase));
                if (wolnaSluzba is not null)
                    kolory.Add(wolnaSluzba);

                await _controller.SaveKoloryAsync(kolory);
                await _controller.SetLessColorAsync(LessColorCheckBox.IsChecked == true);
                await _controller.SetKolorowanieEdycjaPersoneluAsync(
                    KolorowanieEdycjaPersoneluCheckBox.IsChecked == true);
                await _controller.SetGrafikRowColorSettingsAsync(new GrafikRowColorSettings
                {
                    Mode = AlternatingColorsRadio.IsChecked == true
                        ? GrafikRowColorMode.Alternating
                        : GrafikRowColorMode.Role,
                    ColorA = string.IsNullOrWhiteSpace(AltColorATextBox.Text)
                        ? GrafikRowColorSettings.DefaultColorA
                        : AltColorATextBox.Text.Trim(),
                    ColorB = string.IsNullOrWhiteSpace(AltColorBTextBox.Text)
                        ? GrafikRowColorSettings.DefaultColorB
                        : AltColorBTextBox.Text.Trim()
                });
                await _controller.SetGrafikExportAlternatingSettingsAsync(new GrafikExportAlternatingSettings
                {
                    Enabled = ExportAlternatingColorsCheckBox.IsChecked == true,
                    ColorA = string.IsNullOrWhiteSpace(ExportAltColorATextBox.Text)
                        ? GrafikRowColorSettings.DefaultColorA
                        : ExportAltColorATextBox.Text.Trim(),
                    ColorB = string.IsNullOrWhiteSpace(ExportAltColorBTextBox.Text)
                        ? GrafikRowColorSettings.DefaultColorB
                        : ExportAltColorBTextBox.Text.Trim()
                });
            }

            if (IncludesOznaczenia)
            {
                var validationError = ValidateOznaczenia();
                if (validationError is not null)
                {
                    BoberMessageBox.Show(GetOwnerWindow(), validationError, "BOBER");
                    return;
                }

                short order = 0;
                foreach (var o in _oznaczenia)
                    o.Kolejnosc = ++order;

                await _controller.SaveOznaczeniaAsync(_oznaczenia.Select(o => o.ToModel()).ToList());
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

    private void OnAddOznaczenieClick(object sender, RoutedEventArgs e)
    {
        var next = (short)(_oznaczenia.Count + 1);
        var vm = new OznaczenieGrafikuViewModel
        {
            Kod = "X",
            Nazwa = "Nowe oznaczenie",
            KolorHex = RoleKeys.BrakWypelnienia,
            KolorExcelHex = RoleKeys.BrakWypelnienia,
            WPracy = false,
            EksportDoExcela = true,
            AdnotacjaRozkazu = string.Empty,
            Kolejnosc = next
        };
        _oznaczenia.Add(vm);
        // ComboBox w DataGrid czasem gubi SelectedValue przy tworzeniu wiersza.
        vm.EksportDoExcela = true;
        OznaczeniaDataGrid.SelectedIndex = _oznaczenia.Count - 1;
        OznaczeniaDataGrid.ScrollIntoView(OznaczeniaDataGrid.SelectedItem);
    }

    private async void OnRemoveOznaczenieClick(object sender, RoutedEventArgs e)
    {
        if (OznaczeniaDataGrid.SelectedItem is not OznaczenieGrafikuViewModel selected)
        {
            BoberMessageBox.Show(GetOwnerWindow(), "Zaznacz oznaczenie do usunięcia.", "BOBER");
            return;
        }

        try
        {
            var used = await _controller.CountWpisowZKodemAsync(selected.Kod);
            if (used > 0)
            {
                var confirm = BoberMessageBox.Show(
                    GetOwnerWindow(),
                    $"Kod „{selected.Kod}” jest użyty w {used} wpisach grafiku. Usunąć mimo to?",
                    "BOBER",
                    BoberMessageButtons.YesNo);
                if (confirm != MessageBoxResult.Yes)
                    return;
            }
        }
        catch
        {
            /* brak tabeli / błąd — pozwól usunąć */
        }

        _oznaczenia.Remove(selected);
    }

    private void OnOznaczenieClearColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: OznaczenieGrafikuViewModel vm })
            vm.ClearFill();
    }

    private void OnOznaczenieClearExcelColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: OznaczenieGrafikuViewModel vm })
            vm.ClearExcelFill();
    }

    private void OnOznaczenieColorPreviewClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: OznaczenieGrafikuViewModel vm })
            return;

        var chosen = PickColor(
            vm.JestFlaga
                ? (vm.HasFill ? vm.KolorHex : OznaczenieGrafiku.DomyslnyKolorCzcionkiFlagi)
                : (vm.HasFill ? vm.KolorHex : "#FFFF00"));
        if (chosen is not null)
            vm.KolorHex = chosen;
    }

    private void OnOznaczenieExcelColorPreviewClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: OznaczenieGrafikuViewModel vm })
            return;

        var start = vm.HasExcelFill
            ? vm.KolorExcelHex
            : (vm.HasFill ? vm.KolorHex : "#FFFF00");
        var chosen = PickColor(start);
        if (chosen is not null)
            vm.SetExcelFill(chosen);
    }

    private void OnOznaczenieSkrotKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: OznaczenieGrafikuViewModel vm })
            return;

        if (e.Key is Key.Tab or Key.Escape or Key.Enter)
            return;

        e.Handled = true;
        if (e.Key is Key.Back or Key.Delete)
        {
            vm.SkrotKlawiszowy = string.Empty;
            return;
        }

        var next = Helpers.SkrotKlawiszowyCapture.FromKey(e.Key);
        if (string.IsNullOrEmpty(next))
            return;

        if (FindSkrotConflict(vm, next) is { } konflikt)
        {
            BoberMessageBox.Show(
                GetOwnerWindow(),
                $"Skrót „{BOBER.Core.Oznaczenia.SkrotKlawiszowyRules.FormatForDisplay(next)}” jest już użyty przez „{konflikt.Kod} — {konflikt.Nazwa}”.",
                "BOBER");
            return;
        }

        vm.SkrotKlawiszowy = next;
    }

    private OznaczenieGrafikuViewModel? FindSkrotConflict(OznaczenieGrafikuViewModel current, string skrot)
    {
        foreach (var o in _oznaczenia)
        {
            if (ReferenceEquals(o, current) || string.IsNullOrWhiteSpace(o.SkrotKlawiszowy))
                continue;
            if (BOBER.Core.Oznaczenia.SkrotKlawiszowyRules.Matches(o.SkrotKlawiszowy, skrot))
                return o;
        }

        return null;
    }

    private string? ValidateOznaczenia()
    {
        var kody = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var zajeteSkroty = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var o in _oznaczenia)
        {
            var kod = o.Kod.Trim();
            if (string.IsNullOrWhiteSpace(kod))
                return "Symbol oznaczenia nie może być pusty.";
            if (kod.Contains(GrafikWpisTypy.FlagaSeparator))
                return $"Symbol „{kod}” zawiera niedozwolony znak sterujący.";
            if (!o.JestFlaga && kod.IndexOfAny(['*', '\u001E']) >= 0)
                return $"Symbol „{kod}” nie może zawierać znaku *.";
            if (kod.Length > 20)
                return $"Symbol „{kod}” jest za długi (max 20).";
            if (!kody.Add(kod))
                return $"Zduplikowany symbol oznaczenia: {kod}.";

            var skrot = o.SkrotKlawiszowy.Trim();
            if (!string.IsNullOrEmpty(skrot))
            {
                var aliases = BOBER.Core.Oznaczenia.SkrotKlawiszowyRules.Expand(skrot);
                if (aliases.Any(a => zajeteSkroty.Contains(a)))
                {
                    var display = BOBER.Core.Oznaczenia.SkrotKlawiszowyRules.FormatForDisplay(skrot);
                    return $"Zduplikowany skrót klawiszowy: {display} (koliduje z innym skrótem).";
                }

                foreach (var a in aliases)
                    zajeteSkroty.Add(a);
            }

            if (!o.JestFlaga && !o.WPracy && o.SekcjaRozkazu is null)
                return $"Oznaczenie „{kod}” (nieobecność) wymaga grupy w rozkazie.";
        }

        return null;
    }

    private Window? GetOwnerWindow() => Window.GetWindow(this);
}
