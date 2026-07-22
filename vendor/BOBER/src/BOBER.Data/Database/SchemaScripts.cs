namespace BOBER.Data.Database;

internal static class SchemaScripts
{
    public static IReadOnlyList<string> CreateTables { get; } =
    [
        """
        CREATE TABLE UzytkownicyBOBER (
            Id AUTOINCREMENT PRIMARY KEY,
            Login TEXT(50) NOT NULL,
            NumerZmiany SHORT NOT NULL,
            HasloHash TEXT(128) NOT NULL,
            HasloSol TEXT(64) NOT NULL
        )
        """,
        """
        CREATE TABLE GrafikWpisy (
            Id AUTOINCREMENT PRIMARY KEY,
            FunkcjonariuszId LONG NOT NULL,
            ZmianaId SHORT NOT NULL,
            Rok SHORT NOT NULL,
            Miesiac SHORT NOT NULL,
            Dzien SHORT NOT NULL,
            TypWpisu TEXT(5) NOT NULL,
            IsAuto YESNO NOT NULL
        )
        """,
        """
        CREATE TABLE KolejnoscFunkcjonariuszy (
            FunkcjonariuszId LONG NOT NULL,
            ZmianaId SHORT NOT NULL,
            Pozycja SHORT NOT NULL
        )
        """,
        """
        CREATE TABLE KoloryStanowisk (
            KluczRoli TEXT(50) NOT NULL,
            KolorHex TEXT(10) NOT NULL
        )
        """,
        """
        CREATE TABLE Ustawienia (
            Klucz TEXT(100) NOT NULL,
            Wartosc TEXT(255) NOT NULL
        )
        """,
        """
        CREATE TABLE UrlopPlanWpisy (
            Id AUTOINCREMENT PRIMARY KEY,
            FunkcjonariuszId LONG NOT NULL,
            ZmianaId SHORT NOT NULL,
            Rok SHORT NOT NULL,
            Miesiac SHORT NOT NULL,
            Dzien SHORT NOT NULL,
            TypUrlopu TEXT(1) NOT NULL
        )
        """,
        """
        CREATE TABLE GrafikNurkowyZatwierdzenia (
            Rok SHORT NOT NULL,
            Miesiac SHORT NOT NULL,
            Zatwierdzony YESNO NOT NULL,
            ZatwierdzonyPrzez TEXT(100),
            DataZatwierdzenia DATETIME
        )
        """,
        """
        CREATE TABLE GrafikNotatki (
            Id AUTOINCREMENT PRIMARY KEY,
            ZmianaId SHORT NOT NULL,
            Rok SHORT NOT NULL,
            Miesiac SHORT NOT NULL,
            Dzien SHORT NOT NULL,
            Tresc MEMO NOT NULL
        )
        """
    ];
}
