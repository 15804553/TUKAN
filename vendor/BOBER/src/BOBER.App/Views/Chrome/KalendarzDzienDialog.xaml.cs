using System.Windows;
using System.Windows.Input;
using BOBER.App.ViewModels;

namespace BOBER.App.Views.Chrome;

public partial class KalendarzDzienDialog : Window
{
    public enum DialogAction
    {
        Cancel,
        Open,
        DeleteSelected,
        AddPrivate
    }

    public KalendarzDzienDialog()
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this, canResize: true);
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public DialogAction ResultAction { get; private set; } = DialogAction.Cancel;

    public IReadOnlyList<KalendarzDzienWpisViewModel> Entries
    {
        set => EntriesListBox.ItemsSource = value;
    }

    public IReadOnlyList<KalendarzDzienWpisViewModel> SelectedEntries =>
        EntriesListBox.SelectedItems.Cast<KalendarzDzienWpisViewModel>().ToList();

    public KalendarzDzienWpisViewModel? SelectedEntry => EntriesListBox.SelectedItem as KalendarzDzienWpisViewModel;

    public void Configure(
        DateOnly data,
        bool canAddPrivate,
        bool canDeleteVisibleEntries,
        string? addButtonText = null)
    {
        HeaderTextBlock.Text = $"Notatki z dnia {data:dd.MM.yyyy}";
        AddPrivateButton.Visibility = canAddPrivate ? Visibility.Visible : Visibility.Collapsed;
        if (!string.IsNullOrWhiteSpace(addButtonText))
            AddPrivateButton.Content = addButtonText;
        DeleteSelectedButton.Visibility = canDeleteVisibleEntries ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        ResultAction = DialogAction.Cancel;
        DialogResult = false;
        e.Handled = true;
    }

    private void OnAddPrivateClick(object sender, RoutedEventArgs e)
    {
        ResultAction = DialogAction.AddPrivate;
        DialogResult = true;
    }

    private void OnOpenClick(object sender, RoutedEventArgs e) => TryOpenSelected();

    private void OnEntriesDoubleClick(object sender, MouseButtonEventArgs e) => TryOpenSelected();

    private void TryOpenSelected()
    {
        if (SelectedEntry is null)
        {
            BoberMessageBox.Show(this, "Wybierz notatkę do otwarcia.", "Kalendarz");
            return;
        }

        ResultAction = DialogAction.Open;
        DialogResult = true;
    }

    private void OnDeleteSelectedClick(object sender, RoutedEventArgs e)
    {
        if (SelectedEntries.Count == 0)
        {
            BoberMessageBox.Show(this, "Zaznacz co najmniej jedną notatkę do usunięcia.", "Kalendarz");
            return;
        }

        ResultAction = DialogAction.DeleteSelected;
        DialogResult = true;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        ResultAction = DialogAction.Cancel;
        DialogResult = false;
    }
}
