namespace SKRYBEK.App.Views;

/// <summary>
/// Sekcja ustawień SKRYBEK — <see cref="All"/> zachowuje zagnieżdżone zakładki (samodzielna aplikacja),
/// pozostałe wartości eksponują jedną sekcję na poziomie nadrzędnego okna ustawień TUKAN.
/// </summary>
public enum SkrybekSettingsSection
{
    All,
    Ogolne,
    Pojazdy,
    Backup,
    OgolneZBackupem
}
