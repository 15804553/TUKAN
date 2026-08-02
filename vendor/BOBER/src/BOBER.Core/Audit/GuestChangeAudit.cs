namespace BOBER.Core.Audit;

/// <summary>Most do audytu Gościa i blokady Planu urlopów (konfigurowany przez TUKAN).</summary>
public static class GuestChangeAudit
{
    /// <summary>moduleKey: Grafik|Personel|Rozkazy|Urlopy|Ustawienia</summary>
    public static Func<string, string, Task>? TryAppendAsync { get; set; }

    public static Func<int, Task<bool>>? IsUrlopPlanLockedAsync { get; set; }

    public static bool IsGuestSession { get; set; }

    public static void Clear()
    {
        TryAppendAsync = null;
        IsUrlopPlanLockedAsync = null;
        IsGuestSession = false;
    }
}
