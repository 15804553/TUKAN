namespace BOBER.Core.Audit;

/// <summary>Most do audytu Gościa, blokady Planu urlopów i dostępu do zarządzania grafikiem (TUKAN).</summary>
public static class GuestChangeAudit
{
    /// <summary>moduleKey: Grafik|Personel|Rozkazy|Urlopy|Ustawienia</summary>
    public static Func<string, string, Task>? TryAppendAsync { get; set; }

    public static Func<int, Task<bool>>? IsUrlopPlanLockedAsync { get; set; }

    /// <summary>Czy Gość ma dostęp do przygotowania grafiku / czyszczenia półroczy (domyślnie nie).</summary>
    public static Func<int, Task<bool>>? CanManageGrafikAsync { get; set; }

    public static bool IsGuestSession { get; set; }

    public static void Clear()
    {
        TryAppendAsync = null;
        IsUrlopPlanLockedAsync = null;
        CanManageGrafikAsync = null;
        IsGuestSession = false;
    }
}
