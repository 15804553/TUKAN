using System.Windows;
using System.Windows.Controls;
using Chomik.App.Views.Chrome;

namespace Chomik.App.Views.Controls;

/// <summary>Prosty edytor słownika nazwa+Id (stopnie, stanowiska, odznaczenia).</summary>
public partial class NazwaSlownikPanel : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(NazwaSlownikPanel), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(NazwaSlownikPanel), new PropertyMetadata(string.Empty));

    private object? _selectedItem;
    private Func<object, int>? _getId;
    private Func<object, string>? _getNazwa;

    public NazwaSlownikPanel()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public event Func<string, Task>? AddRequested;
    public event Func<int, string, Task>? UpdateRequested;
    public event Func<int, Task>? DeleteRequested;
    public event EventHandler? Changed;

    public void BindItems<T>(
        IReadOnlyList<T> items,
        Func<T, int> getId,
        Func<T, string> getNazwa,
        int? selectedId = null)
        where T : class
    {
        _getId = item => getId((T)item);
        _getNazwa = item => getNazwa((T)item);
        ItemsListBox.ItemsSource = items;

        if (selectedId is int id)
        {
            ItemsListBox.SelectedItem = items.FirstOrDefault(i => getId(i) == id);
        }
        else
        {
            ClearSelection();
        }
    }

    private Window? OwnerWindow => Window.GetWindow(this);

    private void ClearSelection()
    {
        ItemsListBox.SelectedItem = null;
        _selectedItem = null;
        EditNazwaTextBox.Clear();
        EditPanel.Visibility = Visibility.Collapsed;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedItem = ItemsListBox.SelectedItem;
        if (_selectedItem is null || _getNazwa is null)
        {
            EditPanel.Visibility = Visibility.Collapsed;
            return;
        }

        EditNazwaTextBox.Text = _getNazwa(_selectedItem);
        EditPanel.Visibility = Visibility.Visible;
    }

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (AddRequested is null)
        {
            return;
        }

        try
        {
            await AddRequested(NowaNazwaTextBox.Text);
            NowaNazwaTextBox.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }

    private async void OnUpdateClick(object sender, RoutedEventArgs e)
    {
        if (_selectedItem is null || _getId is null || UpdateRequested is null)
        {
            ChomikMessageBox.Show(OwnerWindow, "Wybierz pozycję z listy.", "Informacja");
            return;
        }

        try
        {
            await UpdateRequested(_getId(_selectedItem), EditNazwaTextBox.Text);
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_selectedItem is null || _getId is null || _getNazwa is null || DeleteRequested is null)
        {
            ChomikMessageBox.Show(OwnerWindow, "Wybierz pozycję z listy.", "Informacja");
            return;
        }

        var nazwa = _getNazwa(_selectedItem);
        var confirm = ChomikMessageBox.Show(
            OwnerWindow,
            $"Czy na pewno usunąć „{nazwa}” ze słownika?",
            "Potwierdzenie",
            ChomikMessageButtons.YesNo);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await DeleteRequested(_getId(_selectedItem));
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }
}
