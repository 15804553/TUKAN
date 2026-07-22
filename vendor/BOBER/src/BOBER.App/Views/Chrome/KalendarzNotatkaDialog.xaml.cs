using System.Windows;
using System.Windows.Input;

namespace BOBER.App.Views.Chrome;

public partial class KalendarzNotatkaDialog : Window
{
    public enum DialogAction
    {
        Cancel,
        Save,
        Delete,
        MarkRead
    }

    public KalendarzNotatkaDialog()
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this, canResize: false);
        Loaded += (_, _) =>
        {
            if (NoteTextBox.IsEnabled)
            {
                NoteTextBox.Focus();
                NoteTextBox.CaretIndex = NoteTextBox.Text.Length;
            }
        };
    }

    public DialogAction ResultAction { get; private set; } = DialogAction.Cancel;

    public string NoteText
    {
        get => NoteTextBox.Text;
        set => NoteTextBox.Text = value ?? string.Empty;
    }

    public bool TargetAllShifts => AllShiftsRadio.IsChecked == true;

    public void ConfigureForEdit(
        DateOnly data,
        int workingShiftId,
        string? existingText,
        string? readStatus,
        bool canDelete)
    {
        TitleTextBlock.Text = string.IsNullOrWhiteSpace(existingText) ? "Dodaj notatkę" : "Edytuj notatkę";
        DateTextBlock.Text = $"{data:dd.MM.yyyy} — służba: zmiana {ToRoman(workingShiftId)}";
        ThisShiftRadio.Content = $"Zmiana {ToRoman(workingShiftId)} (służba tego dnia)";
        NoteText = existingText ?? string.Empty;
        NoteTextBox.IsReadOnly = false;
        NoteTextBox.IsEnabled = true;
        TargetPanel.Visibility = Visibility.Visible;
        TargetLabel.Visibility = Visibility.Visible;
        AcceptButton.Visibility = Visibility.Visible;
        AcceptButton.ToolTip = "Zapisz";
        DeleteButton.Visibility = canDelete && !string.IsNullOrWhiteSpace(existingText)
            ? Visibility.Visible
            : Visibility.Collapsed;
        MarkReadButton.Visibility = Visibility.Collapsed;
        SetStatus(readStatus);
    }

    public void ConfigureForRead(
        DateOnly data,
        int zmianaId,
        string tresc,
        bool alreadyRead,
        string? readInfo)
    {
        TitleTextBlock.Text = "Notatka od DCA";
        DateTextBlock.Text = $"{data:dd.MM.yyyy} — zmiana {ToRoman(zmianaId)}";
        NoteText = tresc;
        NoteTextBox.IsReadOnly = true;
        NoteTextBox.IsEnabled = true;
        TargetPanel.Visibility = Visibility.Collapsed;
        TargetLabel.Visibility = Visibility.Collapsed;
        AcceptButton.Visibility = Visibility.Collapsed;
        DeleteButton.Visibility = Visibility.Collapsed;
        MarkReadButton.Visibility = alreadyRead ? Visibility.Collapsed : Visibility.Visible;
        SetStatus(alreadyRead
            ? (readInfo ?? "Przeczytane")
            : "Nieprzeczytane — potwierdź odczyt przyciskiem poniżej.");
    }

    private void SetStatus(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusTextBlock.Visibility = Visibility.Collapsed;
            StatusTextBlock.Text = string.Empty;
            return;
        }

        StatusTextBlock.Text = text;
        StatusTextBlock.Visibility = Visibility.Visible;
    }

    private static string ToRoman(int zmianaId) => zmianaId switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        _ => zmianaId.ToString()
    };

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnAcceptClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NoteTextBox.Text))
        {
            BoberMessageBox.Show(this, "Treść notatki nie może być pusta.", "Kalendarz");
            return;
        }

        ResultAction = DialogAction.Save;
        DialogResult = true;
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        var confirm = BoberMessageBox.Show(
            this,
            "Usunąć notatkę dla wybranego zakresu zmian?",
            "Kalendarz",
            BoberMessageButtons.YesNo);
        if (confirm != MessageBoxResult.Yes)
            return;

        ResultAction = DialogAction.Delete;
        DialogResult = true;
    }

    private void OnMarkReadClick(object sender, RoutedEventArgs e)
    {
        ResultAction = DialogAction.MarkRead;
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        ResultAction = DialogAction.Cancel;
        DialogResult = false;
    }
}
