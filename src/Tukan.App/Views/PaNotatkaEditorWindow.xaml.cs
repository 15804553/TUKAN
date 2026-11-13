using System.Windows;
using Tukan.App.Models;
using Tukan.App.Views.Chrome;

namespace Tukan.App.Views;

public partial class PaNotatkaEditorWindow : Window
{
    public PaNotatkaEditorWindow(string tytul, string naglowek, string tresc, string? informacjaODacie)
    {
        InitializeComponent();
        Title = tytul;
        TitleBar.Title = tytul;
        NaglowekTextBox.Text = naglowek;
        TrescTextBox.Text = tresc;
        if (!string.IsNullOrWhiteSpace(informacjaODacie))
        {
            DataInfoTextBlock.Text = informacjaODacie;
        }
    }

    public string Naglowek => NaglowekTextBox.Text;

    public string Tresc => TrescTextBox.Text;

    private void OnZapiszClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NaglowekTextBox.Text))
        {
            TukanMessageBox.Show(this, "Podaj nagłówek notatki — temat, telefon albo krótki opis.", "Notatki");
            NaglowekTextBox.Focus();
            return;
        }

        if (NaglowekTextBox.Text.Trim().Length > PaNotatka.MaksDlugoscNaglowka)
        {
            TukanMessageBox.Show(
                this,
                $"Nagłówek może mieć najwyżej {PaNotatka.MaksDlugoscNaglowka} znaków.",
                "Notatki");
            return;
        }

        DialogResult = true;
    }
}
