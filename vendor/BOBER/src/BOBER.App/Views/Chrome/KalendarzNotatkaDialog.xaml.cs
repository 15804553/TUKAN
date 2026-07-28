using System.Windows;
using System.Windows.Input;

namespace BOBER.App.Views.Chrome;

public partial class KalendarzNotatkaDialog : Window
{
    private bool _requiresPrivateTargets;

    public enum DialogAction
    {
        Cancel,
        Save,
        Delete,
        MarkRead,
        Reply
    }

    public KalendarzNotatkaDialog()
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this, canResize: false);
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += (_, _) =>
        {
            if (NoteTextBox.IsEnabled && !NoteTextBox.IsReadOnly)
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

    public IReadOnlyList<int> SelectedPrivateTargets =>
        new[]
        {
            (CheckBox: Shift1CheckBox, Shift: 1),
            (CheckBox: Shift2CheckBox, Shift: 2),
            (CheckBox: Shift3CheckBox, Shift: 3)
        }
        .Where(item => item.CheckBox.IsChecked == true)
        .Select(item => item.Shift)
        .ToList();

    public void ConfigureForEdit(
        DateOnly data,
        int workingShiftId,
        string? existingText,
        string? readStatus,
        bool canDelete)
    {
        _requiresPrivateTargets = false;
        TitleTextBlock.Text = string.IsNullOrWhiteSpace(existingText) ? "Dodaj notatkę" : "Edytuj notatkę";
        DateTextBlock.Text = $"{data:dd.MM.yyyy} — służba: zmiana {ToRoman(workingShiftId)}";
        ThisShiftRadio.Content = $"Zmiana {ToRoman(workingShiftId)} (służba tego dnia)";
        NoteText = existingText ?? string.Empty;
        NoteTextBox.IsReadOnly = false;
        NoteTextBox.IsEnabled = true;
        TargetPanel.Visibility = Visibility.Visible;
        PrivateTargetsPanel.Visibility = Visibility.Collapsed;
        TargetLabel.Visibility = Visibility.Visible;
        AcceptButton.Visibility = Visibility.Visible;
        AcceptButton.ToolTip = "Zapisz";
        DeleteButton.Visibility = canDelete && !string.IsNullOrWhiteSpace(existingText)
            ? Visibility.Visible
            : Visibility.Collapsed;
        ReplyButton.Visibility = Visibility.Collapsed;
        MarkReadButton.Visibility = Visibility.Collapsed;
        SetStatus(readStatus);
    }

    public void ConfigureForRead(
        DateOnly data,
        int zmianaId,
        string tresc,
        bool alreadyRead,
        string? readInfo,
        string titleText = "Notatka od DCA",
        bool canConfirmRead = true,
        bool canReply = false)
    {
        _requiresPrivateTargets = false;
        TitleTextBlock.Text = titleText;
        DateTextBlock.Text = $"{data:dd.MM.yyyy} — zmiana {ToRoman(zmianaId)}";
        NoteText = tresc;
        NoteTextBox.IsReadOnly = true;
        NoteTextBox.IsEnabled = true;
        TargetPanel.Visibility = Visibility.Collapsed;
        PrivateTargetsPanel.Visibility = Visibility.Collapsed;
        TargetLabel.Visibility = Visibility.Collapsed;
        AcceptButton.Visibility = Visibility.Collapsed;
        DeleteButton.Visibility = Visibility.Collapsed;
        ReplyButton.Visibility = canReply ? Visibility.Visible : Visibility.Collapsed;
        MarkReadButton.Visibility = canConfirmRead && !alreadyRead
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (canConfirmRead && !alreadyRead)
        {
            SetStatus(readInfo ?? "Nieprzeczytane — potwierdź odczyt przyciskiem poniżej.");
        }
        else if (alreadyRead)
        {
            SetStatus(readInfo ?? "Przeczytane");
        }
        else
        {
            SetStatus(readInfo);
        }
    }

    public void ConfigureForShiftCompose(
        DateOnly data,
        int authorShiftId,
        IReadOnlyList<int>? defaultTargets = null,
        string? titleOverride = null,
        string? initialText = null)
    {
        _requiresPrivateTargets = true;
        TitleTextBlock.Text = titleOverride ?? "Prywatna notatka między zmianami";
        DateTextBlock.Text = $"{data:dd.MM.yyyy} — nadawca: zmiana {ToRoman(authorShiftId)}";
        TargetLabel.Text = "Do zmian:";
        NoteText = initialText ?? string.Empty;
        NoteTextBox.IsReadOnly = false;
        NoteTextBox.IsEnabled = true;
        TargetPanel.Visibility = Visibility.Collapsed;
        PrivateTargetsPanel.Visibility = Visibility.Visible;
        TargetLabel.Visibility = Visibility.Visible;
        AcceptButton.Visibility = Visibility.Visible;
        AcceptButton.ToolTip = "Wyślij";
        DeleteButton.Visibility = Visibility.Collapsed;
        ReplyButton.Visibility = Visibility.Collapsed;
        MarkReadButton.Visibility = Visibility.Collapsed;
        SetPrivateTargetSelection(defaultTargets ?? []);
        SetStatus("Wiadomość jest widoczna tylko dla wskazanych zmian. DCA nie zobaczy tej notatki.");
    }

    public void ConfigureForDcaReply(DateOnly data, int authorShiftId)
    {
        _requiresPrivateTargets = false;
        TitleTextBlock.Text = "Odpowiedź do DCA";
        DateTextBlock.Text = $"{data:dd.MM.yyyy} — nadawca: zmiana {ToRoman(authorShiftId)}";
        NoteText = string.Empty;
        NoteTextBox.IsReadOnly = false;
        NoteTextBox.IsEnabled = true;
        TargetPanel.Visibility = Visibility.Collapsed;
        PrivateTargetsPanel.Visibility = Visibility.Collapsed;
        TargetLabel.Visibility = Visibility.Collapsed;
        AcceptButton.Visibility = Visibility.Visible;
        AcceptButton.ToolTip = "Wyślij odpowiedź";
        DeleteButton.Visibility = Visibility.Collapsed;
        ReplyButton.Visibility = Visibility.Collapsed;
        MarkReadButton.Visibility = Visibility.Collapsed;
        SetStatus("Odpowiedź będzie widoczna dla DCA.");
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
        0 => "DCA",
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

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        ResultAction = DialogAction.Cancel;
        DialogResult = false;
        e.Handled = true;
    }

    private void OnAcceptClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NoteTextBox.Text))
        {
            BoberMessageBox.Show(this, "Treść notatki nie może być pusta.", "Kalendarz");
            return;
        }

        if (_requiresPrivateTargets && SelectedPrivateTargets.Count == 0)
        {
            BoberMessageBox.Show(this, "Wybierz co najmniej jedną zmianę docelową.", "Kalendarz");
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

    private void OnReplyClick(object sender, RoutedEventArgs e)
    {
        ResultAction = DialogAction.Reply;
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

    private void SetPrivateTargetSelection(IReadOnlyList<int> selectedTargets)
    {
        var selected = selectedTargets.ToHashSet();
        Shift1CheckBox.IsChecked = selected.Contains(1);
        Shift2CheckBox.IsChecked = selected.Contains(2);
        Shift3CheckBox.IsChecked = selected.Contains(3);
    }
}
