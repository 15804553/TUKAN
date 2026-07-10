using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SKRYBEK.App.ViewModels;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.Views;

public partial class SkrybekSettingsView : UserControl
{
    public SettingsViewModel ViewModel { get; }

    public SkrybekSettingsView(
        SessionInfo session,
        SkrybekSettingsSection section = SkrybekSettingsSection.All,
        SettingsViewModel? sharedViewModel = null)
    {
        InitializeComponent();
        ViewModel = sharedViewModel ?? new SettingsViewModel(session);
        DataContext = ViewModel;

        if (sharedViewModel is null)
        {
            _ = ViewModel.LoadAsync();
        }

        ApplySectionLayout(section);
    }

    private void ApplySectionLayout(SkrybekSettingsSection section)
    {
        if (section == SkrybekSettingsSection.All)
        {
            return;
        }

        if (section == SkrybekSettingsSection.OgolneZBackupem)
        {
            ApplyCombinedSectionLayout(OgolneTabItem, BackupTabItem);
            return;
        }

        var tabItem = section switch
        {
            SkrybekSettingsSection.Ogolne => OgolneTabItem,
            SkrybekSettingsSection.Pojazdy => PojazdyTabItem,
            SkrybekSettingsSection.Backup => BackupTabItem,
            _ => null
        };

        if (tabItem?.Content is not UIElement sectionContent)
        {
            return;
        }

        tabItem.Content = null;
        FlatSectionHost.Content = sectionContent;
        FlatSectionHost.Visibility = Visibility.Visible;
        NestedTabControl.Visibility = Visibility.Collapsed;
    }

    private void ApplyCombinedSectionLayout(params TabItem[] tabItems)
    {
        var combined = new StackPanel();

        foreach (var tabItem in tabItems)
        {
            if (tabItem.Content is not UIElement sectionContent)
            {
                continue;
            }

            tabItem.Content = null;
            combined.Children.Add(sectionContent);
        }

        if (combined.Children.Count == 0)
        {
            return;
        }

        FlatSectionHost.Content = combined;
        FlatSectionHost.Visibility = Visibility.Visible;
        NestedTabControl.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Anuluje edycję komórki przed akcją Zapisz/Usuń — zapobiega błędom bindingu WPF
    /// („operacja podawania wartości elementu…”) przy zatwierdzaniu niepoprawnych danych w siatce.
    /// </summary>
    private void OnSamochodGridActionPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        SamochodyDataGrid.CancelEdit(DataGridEditingUnit.Row);
    }
}
