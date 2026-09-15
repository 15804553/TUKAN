namespace BOBER.Core.Enums;

/// <summary>Typ wiersza podsumowania grafiku (poza stałymi „Wolne miejsca”).</summary>
public enum GrafikZliczanieTyp : short
{
    Zliczanie = 0,
    Poziom = 1
}

/// <summary>Źródło dopasowania osoby: słownik uprawnień albo stanowisk.</summary>
public enum GrafikZliczanieZrodlo : short
{
    Uprawnienia = 0,
    Stanowiska = 1
}
