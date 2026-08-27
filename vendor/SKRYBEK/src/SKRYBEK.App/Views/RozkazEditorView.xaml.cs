using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SKRYBEK.App.Helpers;
using SKRYBEK.App.ViewModels;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App.Views;

public partial class RozkazEditorView : UserControl
{
    private string _loggedInLogin = string.Empty;
    private bool _canEditAll;
    private INotifyPropertyChanged? _subscribedVm;

    public RozkazEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyLoggedInUserAccess(_loggedInLogin, _canEditAll, DataContext as RozkazEditorViewModel);
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is RozkazEditorViewModel vm)
            {
                SubscribeEditorVm(vm);
                ApplyLoggedInUserAccess(_loggedInLogin, _canEditAll, vm);
            }
        };
    }

    private void UsunNieobecnego_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: NieobecnyViewModel nieobecny })
        {
            return;
        }

        var group = FindDataContext<NieobecniGroupViewModel>(sender as DependencyObject);
        group?.UsunNieobecnegoCommand.Execute(nieobecny);
    }

    private void ZatwierdzDropdown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } btn)
        {
            return;
        }

        menu.PlacementTarget = btn;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void ZatwierdzMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is RozkazEditorViewModel { MozeAkceptowac: true } vm)
        {
            _ = vm.AkceptujRozkazCommand.ExecuteAsync(null);
        }
    }

    private void ZatwierdzWszystkieMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is RozkazEditorViewModel vm)
        {
            _ = vm.AkceptujWszystkieCommand.ExecuteAsync(null);
        }
    }

    private static T? FindDataContext<T>(DependencyObject? start) where T : class
    {
        while (start is not null)
        {
            if (start is FrameworkElement { DataContext: T match })
            {
                return match;
            }

            start = VisualTreeHelper.GetParent(start);
        }

        return null;
    }

    private void SubscribeEditorVm(RozkazEditorViewModel vm)
    {
        if (_subscribedVm is not null)
        {
            _subscribedVm.PropertyChanged -= OnEditorVmChanged;
        }

        _subscribedVm = vm;
        vm.PropertyChanged += OnEditorVmChanged;
    }

    private void OnEditorVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is RozkazEditorViewModel vm)
        {
            ApplyLoggedInUserAccess(_loggedInLogin, _canEditAll, vm);
        }
    }

    /// <summary>Ustawia widoczność panelu personelu i przycisków wg loginu z sesji.</summary>
    public void ApplyLoggedInUserAccess(string loggedInLogin, bool canEditAll, RozkazEditorViewModel? editorVm)
    {
        _loggedInLogin = loggedInLogin ?? string.Empty;
        _canEditAll = canEditAll;

        if (editorVm is not null)
        {
            SubscribeEditorVm(editorVm);
        }

        var showPersonnel = SkrybekLoginAccess.ShowPersonnelPanel(_loggedInLogin);
        var showSave = SkrybekLoginAccess.ShowSaveButton(_loggedInLogin, _canEditAll);
        var showApproveCombo = SkrybekLoginAccess.ShowApproveCombo(_loggedInLogin, _canEditAll);
        var showUnlock = SkrybekLoginAccess.ShowUnlockButton(
            _loggedInLogin, _canEditAll, editorVm?.MozeOdblokować ?? false);
        var showExport = SkrybekLoginAccess.ShowExportWordButton(_canEditAll);

        PersonelPanelHost.Visibility = showPersonnel ? Visibility.Visible : Visibility.Collapsed;
        PersonelColumn.Width = showPersonnel ? GridLength.Auto : new GridLength(0);
        BtnZapiszRozkaz.Visibility = showSave ? Visibility.Visible : Visibility.Collapsed;
        BtnZatwierdzCombo.Visibility = showApproveCombo ? Visibility.Visible : Visibility.Collapsed;
        BtnOdblokujRozkaz.Visibility = showUnlock ? Visibility.Visible : Visibility.Collapsed;
        BtnEksportWord.Visibility = showExport ? Visibility.Visible : Visibility.Collapsed;

        SkrybekLog.Info(
            $"RozkazEditorView.ApplyLoggedInUserAccess: login='{_loggedInLogin}', " +
            $"personel={showPersonnel}, zapisz={showSave}, zatwierdzCombo={showApproveCombo}, " +
            $"odblokuj={showUnlock}, eksport={showExport}");
    }
}
