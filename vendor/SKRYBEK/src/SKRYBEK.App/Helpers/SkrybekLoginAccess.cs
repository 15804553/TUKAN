namespace SKRYBEK.App.Helpers;

/// <summary>Reguły widoczności edytora rozkazu wg zalogowanego użytkownika.</summary>
public static class SkrybekLoginAccess
{
    public static bool IsPaAccount(string? login) =>
        string.Equals(login?.Trim(), "PA", StringComparison.OrdinalIgnoreCase);

    public static bool ShowPersonnelPanel(string? login) => !IsPaAccount(login);

    /// <summary>Zapisz — dla zmian (nie PA, nie DCA JRG).</summary>
    public static bool ShowSaveButton(string? login, bool canEditAll) =>
        !IsPaAccount(login) && !canEditAll;

    /// <summary>Eksport Word — ukryty dla DCA JRG (zatwierdza zamiast eksportować z edytora).</summary>
    public static bool ShowExportWordButton(bool canEditAll) => !canEditAll;

    /// <summary>Combo Zatwierdź / Zatwierdź wszystkie — tylko DCA JRG.</summary>
    public static bool ShowApproveCombo(string? login, bool canEditAll) =>
        !IsPaAccount(login) && canEditAll;

    public static bool ShowUnlockButton(string? login, bool canEditAll, bool mozeOdblokowac) =>
        !IsPaAccount(login) && canEditAll && mozeOdblokowac;
}
