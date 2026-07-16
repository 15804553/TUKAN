using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BOBER.App.Controllers;
using BOBER.App.Views.Chrome;
using BOBER.Core.Constants;
using BOBER.Core.Models;

namespace BOBER.App.Views;

public partial class GrafikNurkowyView : UserControl
{
    private GrafikNurkowyController? _controller;
    private int _year;
    private int _month;
    private bool _canApprove;
    private string _approverLogin = string.Empty;
    private bool _isLoading;

    public bool IsEmbedded { get; set; }

    public GrafikNurkowyView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void Initialize(GrafikNurkowyController controller, bool canApprove, string approverLogin)
    {
        _controller = controller;
        _canApprove = canApprove;
        _approverLogin = approverLogin;
        _year = controller.DefaultYear;
        _month = DateTime.Today.Month;
        PopulateCombos();
        ApproveButton.Visibility = canApprove ? Visibility.Visible : Visibility.Collapsed;
        UnlockButton.Visibility = Visibility.Collapsed;
        _ = ReloadAsync();
    }

    private void PopulateCombos()
    {
        YearComboBox.Items.Clear();
        var current = DateTime.Today.Year;
        for (var y = current - 1; y <= current + 1; y++)
            YearComboBox.Items.Add(y);
        YearComboBox.SelectedItem = _year;

        MonthComboBox.Items.Clear();
        for (var m = 1; m <= 12; m++)
            MonthComboBox.Items.Add(new MonthItem(m, GrafikNurkowyConstants.MonthNames[m]));
        MonthComboBox.SelectedIndex = _month - 1;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (_controller is not null)
            await ReloadAsync();
    }

    private async void OnYearOrMonthChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || _controller is null)
            return;

        if (YearComboBox.SelectedItem is int year)
            _year = year;
        if (MonthComboBox.SelectedItem is MonthItem month)
            _month = month.Number;

        await ReloadAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async void OnApproveClick(object sender, RoutedEventArgs e)
    {
        if (_controller is null || !_canApprove)
            return;

        var confirm = BoberMessageBox.Show(
            OwnerWindow,
            $"Zatwierdzić grafik nurkowy za {GrafikNurkowyConstants.MonthNames[_month]} {_year}?\n\n"
            + "Po zatwierdzeniu zmiany nie będą mogły go modyfikować.",
            "Zatwierdzenie grafiku nurkowego",
            BoberMessageButtons.YesNo);
        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            await _controller.ZatwierdzAsync(_year, _month, _approverLogin);
            await ReloadAsync();
            BoberMessageBox.Show(OwnerWindow, "Grafik nurkowy został zatwierdzony i zablokowany.", "Informacja");
        }
        catch (Exception ex)
        {
            BoberMessageBox.Show(OwnerWindow, ex.Message, "Błąd");
        }
    }

    private async void OnUnlockClick(object sender, RoutedEventArgs e)
    {
        if (_controller is null || !_canApprove)
            return;

        var confirm = BoberMessageBox.Show(
            OwnerWindow,
            $"Cofnąć zatwierdzenie grafiku za {GrafikNurkowyConstants.MonthNames[_month]} {_year}?\n\n"
            + "Zmiany będą mogły ponownie aktualizować dokument.",
            "Cofnięcie zatwierdzenia",
            BoberMessageButtons.YesNo);
        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            await _controller.CofnijZatwierdzenieAsync(_year, _month);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            BoberMessageBox.Show(OwnerWindow, ex.Message, "Błąd");
        }
    }

    private async Task ReloadAsync()
    {
        if (_controller is null)
            return;

        _isLoading = true;
        try
        {
            var rows = await _controller.LoadPreviewAsync(_year, _month);
            var status = await _controller.GetZatwierdzenieAsync(_year, _month);
            var exportPath = await _controller.GetExportPathAsync();

            BuildColumns();
            PreviewGrid.ItemsSource = rows.Select(r => new PreviewRow(r)).ToList();

            var locked = status?.Zatwierdzony == true;
            ApproveButton.Visibility = _canApprove && !locked ? Visibility.Visible : Visibility.Collapsed;
            UnlockButton.Visibility = _canApprove && locked ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(exportPath))
            {
                StatusTextBlock.Text =
                    "Nie ustawiono katalogu eksportu grafiku nurkowego (Ustawienia → Ścieżki eksportu).";
            }
            else if (rows.Count == 0)
            {
                StatusTextBlock.Text =
                    $"Brak danych za {GrafikNurkowyConstants.MonthNames[_month]} {_year}. "
                    + "Wygeneruj dokument przyciskiem w Grafiku służb (Zmiana 1/2/3).";
            }
            else if (locked)
            {
                StatusTextBlock.Text =
                    $"ZATWIERDZONY — {status!.ZatwierdzonyPrzez} "
                    + $"({status.DataZatwierdzenia:yyyy-MM-dd HH:mm}). Plik zablokowany przed modyfikacjami.";
            }
            else
            {
                StatusTextBlock.Text =
                    $"Podgląd: {rows.Count} osób. Katalog: {exportPath}";
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
            PreviewGrid.ItemsSource = null;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void BuildColumns()
    {
        PreviewGrid.Columns.Clear();
        PreviewGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Imię i nazwisko",
            Binding = new Binding(nameof(PreviewRow.ImieNazwisko)),
            Width = new DataGridLength(180)
        });
        PreviewGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Funkcja",
            Binding = new Binding(nameof(PreviewRow.Funkcja)),
            Width = new DataGridLength(90)
        });

        var days = DateTime.DaysInMonth(_year, _month);
        for (var day = 1; day <= days; day++)
        {
            var d = day;
            PreviewGrid.Columns.Add(new DataGridTextColumn
            {
                Header = d.ToString(),
                Binding = new Binding($"Day{d}"),
                Width = new DataGridLength(32),
                ElementStyle = CreateCenteredStyle()
            });
        }
    }

    private static Style CreateCenteredStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
        style.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center));
        return style;
    }

    private Window? OwnerWindow => Window.GetWindow(this);

    private sealed record MonthItem(int Number, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed class PreviewRow
    {
        public PreviewRow(GrafikNurkowyWiersz source)
        {
            ImieNazwisko = source.ImieNazwisko;
            Funkcja = source.Funkcja;
            for (var d = 1; d <= 31; d++)
            {
                if (source.Dni.TryGetValue(d, out var v))
                    SetDay(d, v);
            }
        }

        public string ImieNazwisko { get; }
        public string Funkcja { get; }
        public string Day1 { get; private set; } = "";
        public string Day2 { get; private set; } = "";
        public string Day3 { get; private set; } = "";
        public string Day4 { get; private set; } = "";
        public string Day5 { get; private set; } = "";
        public string Day6 { get; private set; } = "";
        public string Day7 { get; private set; } = "";
        public string Day8 { get; private set; } = "";
        public string Day9 { get; private set; } = "";
        public string Day10 { get; private set; } = "";
        public string Day11 { get; private set; } = "";
        public string Day12 { get; private set; } = "";
        public string Day13 { get; private set; } = "";
        public string Day14 { get; private set; } = "";
        public string Day15 { get; private set; } = "";
        public string Day16 { get; private set; } = "";
        public string Day17 { get; private set; } = "";
        public string Day18 { get; private set; } = "";
        public string Day19 { get; private set; } = "";
        public string Day20 { get; private set; } = "";
        public string Day21 { get; private set; } = "";
        public string Day22 { get; private set; } = "";
        public string Day23 { get; private set; } = "";
        public string Day24 { get; private set; } = "";
        public string Day25 { get; private set; } = "";
        public string Day26 { get; private set; } = "";
        public string Day27 { get; private set; } = "";
        public string Day28 { get; private set; } = "";
        public string Day29 { get; private set; } = "";
        public string Day30 { get; private set; } = "";
        public string Day31 { get; private set; } = "";

        private void SetDay(int day, string value)
        {
            switch (day)
            {
                case 1: Day1 = value; break;
                case 2: Day2 = value; break;
                case 3: Day3 = value; break;
                case 4: Day4 = value; break;
                case 5: Day5 = value; break;
                case 6: Day6 = value; break;
                case 7: Day7 = value; break;
                case 8: Day8 = value; break;
                case 9: Day9 = value; break;
                case 10: Day10 = value; break;
                case 11: Day11 = value; break;
                case 12: Day12 = value; break;
                case 13: Day13 = value; break;
                case 14: Day14 = value; break;
                case 15: Day15 = value; break;
                case 16: Day16 = value; break;
                case 17: Day17 = value; break;
                case 18: Day18 = value; break;
                case 19: Day19 = value; break;
                case 20: Day20 = value; break;
                case 21: Day21 = value; break;
                case 22: Day22 = value; break;
                case 23: Day23 = value; break;
                case 24: Day24 = value; break;
                case 25: Day25 = value; break;
                case 26: Day26 = value; break;
                case 27: Day27 = value; break;
                case 28: Day28 = value; break;
                case 29: Day29 = value; break;
                case 30: Day30 = value; break;
                case 31: Day31 = value; break;
            }
        }
    }
}
