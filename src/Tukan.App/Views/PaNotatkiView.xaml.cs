using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tukan.App.Models;
using Tukan.App.Services;
using Tukan.App.Views.Chrome;

namespace Tukan.App.Views;

public partial class PaNotatkiView : UserControl
{
    private readonly PaNotatkiRepository _repozytorium;
    private bool _zapisTrwa;

    public PaNotatkiView(PaNotatkiRepository repozytorium)
    {
        _repozytorium = repozytorium;
        InitializeComponent();
    }

    public Task OdswiezAsync() => WczytajAsync();

    private async void OnNowaClick(object sender, RoutedEventArgs e) => await NowaAsync();

    private async void OnEdytujClick(object sender, RoutedEventArgs e) => await EdytujZaznaczonaAsync();

    private async void OnUsunClick(object sender, RoutedEventArgs e) => await UsunZaznaczonaAsync();

    private async void OnDwuklik(object sender, MouseButtonEventArgs e)
    {
        if (NotatkiGrid.SelectedItem is PaNotatka)
        {
            await EdytujZaznaczonaAsync();
        }
    }

    private void OnWyborZmieniony(object sender, SelectionChangedEventArgs e)
    {
        var jestWybor = NotatkiGrid.SelectedItem is PaNotatka;
        EdytujButton.IsEnabled = jestWybor;
        UsunButton.IsEnabled = jestWybor;
    }

    private async Task NowaAsync()
    {
        var edytor = UtworzEdytor(
            "Nowa notatka",
            string.Empty,
            string.Empty,
            "Data zostanie ustawiona automatycznie przy zapisie.");
        if (edytor.ShowDialog() != true)
        {
            return;
        }

        await WykonajZapisAsync(() => _repozytorium.DodajAsync(edytor.Naglowek, edytor.Tresc));
    }

    private async Task EdytujZaznaczonaAsync()
    {
        if (NotatkiGrid.SelectedItem is not PaNotatka notatka)
        {
            return;
        }

        var edytor = UtworzEdytor(
            "Edycja notatki",
            notatka.Naglowek,
            notatka.Tresc,
            $"Ostatni zapis: {notatka.DataWyswietlana}. Nowa data ustawi się przy zapisie.");
        if (edytor.ShowDialog() != true)
        {
            return;
        }

        var id = notatka.Id;
        await WykonajZapisAsync(async () =>
        {
            await _repozytorium.AktualizujAsync(id, edytor.Naglowek, edytor.Tresc);
            return id;
        });
    }

    private async Task UsunZaznaczonaAsync()
    {
        if (NotatkiGrid.SelectedItem is not PaNotatka notatka)
        {
            return;
        }

        var potwierdzone = TukanMessageBox.Confirm(
            Window.GetWindow(this),
            $"Usunąć notatkę „{notatka.Naglowek}”?",
            "Notatki",
            "Usuń");
        if (!potwierdzone)
        {
            return;
        }

        await WykonajZapisAsync(async () =>
        {
            await _repozytorium.UsunAsync(notatka.Id);
            return 0;
        });
    }

    private PaNotatkaEditorWindow UtworzEdytor(string tytul, string naglowek, string tresc, string informacja)
    {
        var edytor = new PaNotatkaEditorWindow(tytul, naglowek, tresc, informacja);
        var wlasciciel = Window.GetWindow(this);
        if (wlasciciel is not null)
        {
            edytor.Owner = wlasciciel;
        }

        return edytor;
    }

    private async Task WykonajZapisAsync(Func<Task<int>> zapis)
    {
        if (_zapisTrwa)
        {
            return;
        }

        _zapisTrwa = true;
        try
        {
            await zapis();
            await WczytajAsync();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.Data.OleDb.OleDbException)
        {
            TukanMessageBox.Show(Window.GetWindow(this), ex.Message, "Notatki");
        }
        finally
        {
            _zapisTrwa = false;
        }
    }

    private async Task WczytajAsync()
    {
        try
        {
            var notatki = await _repozytorium.PobierzWszystkieAsync();
            var zaznaczoneId = (NotatkiGrid.SelectedItem as PaNotatka)?.Id;
            NotatkiGrid.ItemsSource = notatki;
            if (zaznaczoneId is int id)
            {
                NotatkiGrid.SelectedItem = notatki.FirstOrDefault(n => n.Id == id);
            }

            StatusTextBlock.Text = notatki.Count == 0
                ? "Brak notatek. Dodaj telefon albo zapisek, który nie powinien umknąć."
                : $"Notatek: {notatki.Count}. Dwuklik otwiera edycję.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Nie udało się odczytać notatek.";
            TukanMessageBox.Show(Window.GetWindow(this), ex.Message, "Notatki");
        }
    }
}
