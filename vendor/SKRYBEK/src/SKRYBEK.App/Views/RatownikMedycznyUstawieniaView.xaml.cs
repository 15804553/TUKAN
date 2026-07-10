using System.Windows;
using System.Windows.Controls;
using SKRYBEK.App.ViewModels;

namespace SKRYBEK.App.Views;

public partial class RatownikMedycznyUstawieniaView : UserControl
{
    private readonly RatownikMedycznyUstawieniaViewModel _viewModel;

    public event EventHandler? SettingsSaved;

    public RatownikMedycznyUstawieniaView(int zmianaId)
    {
        InitializeComponent();
        _viewModel = new RatownikMedycznyUstawieniaViewModel(zmianaId);
        _viewModel.SettingsSaved += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.LoadAsync();
    }
}
