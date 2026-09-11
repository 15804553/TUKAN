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
            TypWpisu TEXT(20) NOT NULL,
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
        """,
        """
        CREATE TABLE GrafikUwagiMiesieczne (
            Id AUTOINCREMENT PRIMARY KEY,
            FunkcjonariuszId LONG NOT NULL,
            ZmianaId SHORT NOT NULL,
            Rok SHORT NOT NULL,
            Miesiac SHORT NOT NULL,
            Tresc MEMO NOT NULL
        )
        """,
        """
        CREATE TABLE ObsadaFunkcjiUwagiMiesieczne (
            Id AUTOINCREMENT PRIMARY KEY,
            FunkcjonariuszId LONG NOT NULL,
            ZmianaId SHORT NOT NULL,
            Rok SHORT NOT NULL,
            Miesiac SHORT NOT NULL,
            Tresc MEMO NOT NULL
        )
        """,
        """
        CREATE TABLE KalendarzWpisy (
            Id AUTOINCREMENT PRIMARY KEY,
            Data DATETIME NOT NULL,
            ZmianaId SHORT NOT NULL,
            TypWpisu TEXT(30) NOT NULL,
            AutorZmianaId SHORT,
            Tresc MEMO NOT NULL,
            AutorLogin TEXT(100) NOT NULL,
            DataUtworzenia DATETIME NOT NULL,
            DataModyfikacji DATETIME NOT NULL
        )
        """,
        """
        CREATE TABLE KalendarzOdczyty (
            WpisId LONG NOT NULL,
            ZmianaId SHORT NOT NULL,
            Przeczytane YESNO NOT NULL,
            PrzeczytanePrzez TEXT(100),
            DataOdczytu DATETIME
        )
        """,
        """
        CREATE TABLE OznaczeniaGrafiku (
            Id AUTOINCREMENT PRIMARY KEY,
            ZmianaId SHORT NOT NULL,
            Kod TEXT(20) NOT NULL,
            Nazwa TEXT(50) NOT NULL,
            KolorHex TEXT(10) NOT NULL,
            WPracy YESNO NOT NULL,
            SekcjaRozkazu SHORT,
            SkrotKlawiszowy TEXT(20),
            TekstWyswietlany TEXT(20),
            MoznaOddac YESNO NOT NULL,
            MoznaKropke YESNO NOT NULL,
            ZachowajTloWsPrzyBraku YESNO NOT NULL,
            DodatkowaSekcjaRozkazu SHORT,
            RolaNalozania SHORT NOT NULL,
            Kolejnosc SHORT NOT NULL,
            EksportDoExcela YESNO NOT NULL,
            KolorExcelHex TEXT(10),
            AdnotacjaRozkazu TEXT(40),
            StylWyswietlania SHORT NOT NULL,
            FlagaPozycja SHORT NOT NULL
        )
        """
    ];
}
