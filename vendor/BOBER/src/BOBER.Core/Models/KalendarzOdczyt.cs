namespace BOBER.Core.Models;

/// <summary>Potwierdzenie odczytu notatki kalendarza przez zmianę.</summary>
public sealed class KalendarzOdczyt
{
    public int WpisId { get; set; }
    public int ZmianaId { get; set; }
    public bool Przeczytane { get; set; }
    public string? PrzeczytanePrzez { get; set; }
    public DateTime? DataOdczytu { get; set; }
}
