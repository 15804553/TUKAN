namespace SKRYBEK.Core.Audit;

/// <summary>Most do audytu Gościa (konfigurowany przez TUKAN).</summary>
public static class GuestChangeAudit
{
    /// <summary>moduleKey: Grafik|Personel|Rozkazy|Urlopy|Ustawienia</summary>
    public static Func<string, string, Task>? TryAppendAsync { get; set; }

    public static void Clear() => TryAppendAsync = null;
}
