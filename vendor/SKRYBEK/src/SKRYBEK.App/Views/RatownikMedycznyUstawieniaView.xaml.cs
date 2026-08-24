using System.Windows;
using System.Windows.Controls;
using SKRYBEK.App.ViewModels;

namespace SKRYBEK.App.Views;

public partial class RatownikMedycznyUstawieniaView : UserControl
{
    private readonly RatownikMedycznyUstawieniaViewModel _viewModel;

    public event EventHandler? SettingsSaved;

    public RatownikMedycznyUstawieniaView(int zmianaId, bool showTitle = true)
    {
        InitializeComponent();
        _viewModel = new RatownikMedycznyUstawieniaViewModel(zmianaId);
        _viewModel.SettingsSaved += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
        DataContext = _viewModel;
        if (!showTitle)
        {
            TytulTextBlock.Visibility = Visibility.Collapsed;
            RootBorder.Background = System.Windows.Media.Brushes.Transparent;
            RootBorder.Padding = new Thickness(0);
        }
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.LoadAsync();
    }

    /// <summary>Ponownie wczytuje listę pojazdów (np. po zmianie nazwy lub liczby miejsc).</summary>
    public Task ReloadAsync() => _viewModel.LoadAsync();
}
