using System.Windows;
using Chomik.App.ViewModels;
using Chomik.App.Views.Chrome;

namespace Chomik.App.Views;

public partial class PersonnelProfileWindow : Window
{
    public PersonnelProfileWindow(PersonnelProfileViewModel profile)
    {
        InitializeComponent();
        DataContext = profile;
        TitleBar.Title = profile.PelneImieNazwisko;
        Title = $"Chomik — {profile.PelneImieNazwisko}";
        ChromeWindowConfigurator.Apply(this, canResize: true);
        EmptyUprawnieniaTextBlock.Visibility = profile.Uprawnienia.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
