using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Chomik.App.Controllers;
using Chomik.App.Views.Chrome;
using Chomik.Core;
using Chomik.Core.Models;
using Chomik.Services.Personnel;

namespace Chomik.App.Views;

public partial class PersonnelEditWindow : Window
{
    private readonly PersonnelManagementController _controller;
    private readonly Funkcjonariusz _entity;
    private readonly List<UprawnienieEditorRow> _uprawnieniaRows = [];
    private readonly List<OdznaczenieEditorRow> _odznaczeniaRows = [];

    public PersonnelEditWindow(
        PersonnelManagementController controller,
        PersonnelDictionaries dictionaries,
        Funkcjonariusz entity)
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this);
        _controller = controller;
        _entity = entity;

        StopienComboBox.ItemsSource = dictionaries.Stopnie;
        StanowiskoComboBox.ItemsSource = dictionaries.Stanowiska;
        BuildUprawnieniaUi(dictionaries.TypyUprawnien, entity);
        BuildOdznaczeniaUi(dictionaries.TypyOdznaczen, entity);

        var chromeTitle = entity.Id == 0
            ? "Nowy funkcjonariusz"
            : entity.PelneImieNazwisko;
        var appTitle = ChomikMessageBox.ApplicationTitleOverride ?? "Chomik";
        Title = $"{appTitle} — {chromeTitle}";
        TitleBar.Title = chromeTitle;
        BindEntity(entity);
        WstepienieDoSluzbyDatePicker.SelectedDateChanged += (_, _) => UpdateStazFromServiceStartDate();
        ImieTextBox.LostFocus += (_, _) => ImieTextBox.Text = CapitalizePersonName(ImieTextBox.Text);
        NazwiskoTextBox.LostFocus += (_, _) => NazwiskoTextBox.Text = CapitalizePersonName(NazwiskoTextBox.Text);
    }

    private void BuildUprawnieniaUi(IReadOnlyList<TypUprawnienia> typy, Funkcjonariusz entity)
    {
        var assigned = entity.Uprawnienia
            .GroupBy(u => $"{u.Nazwa}|{u.Podtyp}")
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var typ in typy)
        {
            var key = $"{typ.Nazwa}|{typ.Podtyp}";
            assigned.TryGetValue(key, out var existing);

            var row = new UprawnienieEditorRow(typ);
            _uprawnieniaRows.Add(row);

            var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
            var check = new CheckBox { Content = typ.Etykieta, IsChecked = existing is not null };
            panel.Children.Add(check);

            if (typ.WymagaDaty)
            {
                var datePanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(24, 4, 0, 0),
                    Visibility = existing is not null ? Visibility.Visible : Visibility.Collapsed
                };
                datePanel.Children.Add(new TextBlock
                {
                    Text = "Ważne do:",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                });
                var datePicker = new DatePicker { SelectedDate = existing?.WazneDo, Width = 150 };
                datePanel.Children.Add(datePicker);
                panel.Children.Add(datePanel);
                row.DatePicker = datePicker;

                check.Checked += (_, _) => datePanel.Visibility = Visibility.Visible;
                check.Unchecked += (_, _) =>
                {
                    datePanel.Visibility = Visibility.Collapsed;
                    datePicker.SelectedDate = null;
                };
            }

            row.CheckBox = check;
            UprawnieniaItemsControl.Items.Add(panel);
        }
    }

    private void BuildOdznaczeniaUi(IReadOnlyList<TypOdznaczenia> typy, Funkcjonariusz entity)
    {
        var assigned = entity.Odznaczenia.ToDictionary(o => o.TypOdznaczeniaId);

        foreach (var typ in typy)
        {
            assigned.TryGetValue(typ.Id, out var existing);
            var row = new OdznaczenieEditorRow(typ);
            _odznaczeniaRows.Add(row);

            var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 8) };
            var check = new CheckBox
            {
                Content = typ.Nazwa,
                IsChecked = existing is not null
            };
            panel.Children.Add(check);

            var datePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(24, 4, 0, 0),
                Visibility = existing is not null ? Visibility.Visible : Visibility.Collapsed
            };
            datePanel.Children.Add(new TextBlock
            {
                Text = "Data nadania:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });
            var datePicker = new DatePicker
            {
                SelectedDate = existing?.DataNadania,
                Width = 150
            };
            datePanel.Children.Add(datePicker);
            panel.Children.Add(datePanel);

            check.Checked += (_, _) => datePanel.Visibility = Visibility.Visible;
            check.Unchecked += (_, _) =>
            {
                datePanel.Visibility = Visibility.Collapsed;
                datePicker.SelectedDate = null;
            };

            row.CheckBox = check;
            row.DatePicker = datePicker;
            row.DatePanel = datePanel;
            OdznaczeniaItemsControl.Items.Add(panel);
        }
    }

    private void BindEntity(Funkcjonariusz entity)
    {
        NumerPorzadkowyTextBox.Text = entity.NumerPorzadkowy > 0
            ? entity.NumerPorzadkowy.ToString()
            : string.Empty;
        StopienComboBox.SelectedValue = entity.StopienId > 0 ? entity.StopienId : null;
        StanowiskoComboBox.SelectedValue = entity.StanowiskoId > 0 ? entity.StanowiskoId : null;
        ImieTextBox.Text = entity.Imie;
        NazwiskoTextBox.Text = entity.Nazwisko;
        TelefonTextBox.Text = entity.Telefon ?? string.Empty;
        StazTextBox.Text = entity.StazLat?.ToString() ?? string.Empty;
        InfoTextBox.Text = entity.InformacjaDodatkowa ?? string.Empty;
        WstepienieDoSluzbyDatePicker.SelectedDate = entity.DataWstepieniaDoSluzby;
        UpdateStazFromServiceStartDate();
        BadaniaDatePicker.SelectedDate = entity.BadaniaOkresoweDo;
        KomoraDatePicker.SelectedDate = entity.KomoraDymowaDo;
        KppDatePicker.SelectedDate = entity.KppDo;
        AwansStopienDatePicker.SelectedDate = entity.DataAwansuStopien;
        AwansGrupaDatePicker.SelectedDate = entity.DataAwansuGrupa;
        DodatekTextBox.Text = entity.DodatekMotywacyjny?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (StopienComboBox.SelectedValue is not int stopienId ||
            StanowiskoComboBox.SelectedValue is not int stanowiskoId)
        {
            ChomikMessageBox.Show(this, "Wybierz stopień i stanowisko.", "Informacja");
            return;
        }

        if (string.IsNullOrWhiteSpace(ImieTextBox.Text) || string.IsNullOrWhiteSpace(NazwiskoTextBox.Text))
        {
            ChomikMessageBox.Show(this, "Imię i nazwisko są wymagane.", "Informacja");
            return;
        }

        if (!int.TryParse(NumerPorzadkowyTextBox.Text.Trim(), out var numerPorzadkowy) || numerPorzadkowy < 1)
        {
            ChomikMessageBox.Show(this, "Podaj prawidłowy numer na liście (liczba całkowita od 1).", "Informacja");
            return;
        }

        var selectedOdznaczenia = new Dictionary<int, DateTime>();
        foreach (var row in _odznaczeniaRows)
        {
            if (row.CheckBox?.IsChecked != true)
            {
                continue;
            }

            if (row.DatePicker?.SelectedDate is not DateTime dataNadania)
            {
                ChomikMessageBox.Show(this, $"Podaj datę nadania dla: {row.Typ.Nazwa}", "Informacja");
                return;
            }

            selectedOdznaczenia[row.Typ.Id] = dataNadania;
        }

        _entity.NumerPorzadkowy = numerPorzadkowy;
        _entity.StopienId = stopienId;
        _entity.StanowiskoId = stanowiskoId;
        _entity.Imie = CapitalizePersonName(ImieTextBox.Text);
        _entity.Nazwisko = CapitalizePersonName(NazwiskoTextBox.Text);
        ImieTextBox.Text = _entity.Imie;
        NazwiskoTextBox.Text = _entity.Nazwisko;
        _entity.Telefon = string.IsNullOrWhiteSpace(TelefonTextBox.Text) ? null : TelefonTextBox.Text.Trim();
        _entity.DataWstepieniaDoSluzby = WstepienieDoSluzbyDatePicker.SelectedDate;
        _entity.StazLat = StazCalculator.CalculateServiceYears(_entity.DataWstepieniaDoSluzby);
        _entity.InformacjaDodatkowa = string.IsNullOrWhiteSpace(InfoTextBox.Text) ? null : InfoTextBox.Text.Trim();
        _entity.BadaniaOkresoweDo = BadaniaDatePicker.SelectedDate;
        _entity.KomoraDymowaDo = KomoraDatePicker.SelectedDate;
        _entity.KppDo = KppDatePicker.SelectedDate;
        _entity.DataAwansuStopien = AwansStopienDatePicker.SelectedDate;
        _entity.DataAwansuGrupa = AwansGrupaDatePicker.SelectedDate;
        _entity.DodatekMotywacyjny = decimal.TryParse(DodatekTextBox.Text, out var dodatek) ? dodatek : null;
        _entity.NumerZmiany = _controller.ShiftNumber;

        var selectedTypes = new List<int>();
        var datyUprawnien = new Dictionary<int, DateTime?>();
        foreach (var row in _uprawnieniaRows)
        {
            if (row.CheckBox?.IsChecked != true)
            {
                continue;
            }

            selectedTypes.Add(row.Typ.Id);
            datyUprawnien[row.Typ.Id] = row.DatePicker?.SelectedDate;
        }

        try
        {
            await _controller.SaveAsync(_entity, selectedTypes, datyUprawnien, selectedOdznaczenia);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(this, ex.Message, "Chomik");
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UpdateStazFromServiceStartDate()
    {
        var years = StazCalculator.CalculateServiceYears(WstepienieDoSluzbyDatePicker.SelectedDate);
        StazTextBox.Text = years?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Kapitalizuje imię/nazwisko wg reguł kultury pl-PL (pierwsza litera każdego członu wielka).
    /// </summary>
    private static string CapitalizePersonName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var culture = CultureInfo.GetCultureInfo("pl-PL");
        return culture.TextInfo.ToTitleCase(value.Trim().ToLower(culture));
    }

    private sealed class UprawnienieEditorRow(TypUprawnienia typ)
    {
        public TypUprawnienia Typ { get; } = typ;
        public CheckBox? CheckBox { get; set; }
        public DatePicker? DatePicker { get; set; }
    }

    private sealed class OdznaczenieEditorRow(TypOdznaczenia typ)
    {
        public TypOdznaczenia Typ { get; } = typ;
        public CheckBox? CheckBox { get; set; }
        public DatePicker? DatePicker { get; set; }
        public UIElement? DatePanel { get; set; }
    }
}
