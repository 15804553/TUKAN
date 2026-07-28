namespace BOBER.Core.Models;

/// <summary>Uwaga tekstowa dla pracownika w danym miesiącu grafiku (per zmiana).</summary>
public sealed class GrafikUwagaMiesieczna
{
    public int Id { get; set; }
    public int FunkcjonariuszId { get; set; }
    public int ZmianaId { get; set; }
    public int Rok { get; set; }
    public int Miesiac { get; set; }
    public string Tresc { get; set; } = string.Empty;
}
