using System.Windows;
using System.Windows.Controls;

namespace BOBER.App.Views.Chrome;

public partial class ClearHalfYearDialog : Window
{
    public ClearHalfYearDialog(string polroczeLabel)
    {
        InitializeComponent();
        MessageTextBlock.Text =
            $"Czy wyczyścić wszystkie wpisy z {polroczeLabel}?";
    }

    public bool AlsoClearUrlopPlan => AlsoUrlopCheckBox.IsChecked == true;

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
