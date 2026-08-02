using System.Windows;

namespace Tukan.App.Views.Chrome;

public partial class TukanMessageWindow : Window
{
    public TukanMessageWindow()
    {
        InitializeComponent();
    }

    public void Configure(string message, string title)
    {
        Title = title;
        TitleBar.Title = title;
        MessageTextBlock.Text = message;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
