using System.Windows;

namespace BOBER.App.Views.Chrome;

public partial class ClearUrlopYearDialog : Window
{
    public ClearUrlopYearDialog(int rok, string nazwaZmiany)
    {
        InitializeComponent();
        MessageTextBlock.Text =
            $"Czy na pewno usunąć cały plan urlopów na rok {rok} dla {nazwaZmiany}?";
    }

    private void OnYesClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnNoClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
