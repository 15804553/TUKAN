namespace BOBER.Core.Models;

/// <summary>Wynik generowania / aktualizacji grafiku nurkowego.</summary>
public sealed class GrafikNurkowySyncResult
{
    public required string FilePath { get; init; }
    public bool CreatedNew { get; init; }
    public int UpdatedPeople { get; init; }
    public string Message { get; init; } = string.Empty;
}
