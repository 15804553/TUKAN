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

    public void ConfigureConfirm(string message, string title, string acceptText)
    {
        Configure(message, title);
        OkButton.Content = acceptText;
        CancelButton.Visibility = Visibility.Visible;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
