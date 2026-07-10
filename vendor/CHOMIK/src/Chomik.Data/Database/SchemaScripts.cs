namespace Chomik.Data.Database;

internal static class SchemaScripts
{
    public static IReadOnlyList<string> CreateTables { get; } =
    [
        """
        CREATE TABLE StopnieSlownik (
            Id AUTOINCREMENT PRIMARY KEY,
            Nazwa TEXT(50) NOT NULL
        )
        """,
        """
        CREATE TABLE StanowiskaSlownik (
            Id AUTOINCREMENT PRIMARY KEY,
            Nazwa TEXT(100) NOT NULL
        )
        """,
        """
        CREATE TABLE TypyUprawnien (
            Id AUTOINCREMENT PRIMARY KEY,
            Nazwa TEXT(100) NOT NULL,
            Podtyp TEXT(50),
            WymagaDaty YESNO NOT NULL
        )
        """,
        """
        CREATE TABLE TypyOdznaczen (
            Id AUTOINCREMENT PRIMARY KEY,
            Nazwa TEXT(200) NOT NULL
        )
        """,
        """
        CREATE TABLE Funkcjonariusze (
            Id AUTOINCREMENT PRIMARY KEY,
            NumerZmiany SHORT NOT NULL,
            NumerPorzadkowy SHORT NOT NULL,
            StopienId LONG NOT NULL,
            Imie TEXT(50) NOT NULL,
            Nazwisko TEXT(80) NOT NULL,
            StanowiskoId LONG NOT NULL,
            Telefon TEXT(20),
            StazLat SHORT,
            BadaniaOkresoweDo DATETIME,
            KomoraDymowaDo DATETIME,
            KppDo DATETIME,
            DataWstepieniaDoSluzby DATETIME,
            InformacjaDodatkowa TEXT(255),
            DataAwansuStopien DATETIME,
            DataAwansuGrupa DATETIME,
            DodatekMotywacyjny CURRENCY
        )
        """,
        """
        CREATE TABLE FunkcjonariuszUprawnienia (
            Id AUTOINCREMENT PRIMARY KEY,
            FunkcjonariuszId LONG NOT NULL,
            TypUprawnieniaId LONG NOT NULL,
            WazneDo DATETIME,
            Uwagi TEXT(255)
        )
        """,
        """
        CREATE TABLE FunkcjonariuszOdznaczenia (
            Id AUTOINCREMENT PRIMARY KEY,
            FunkcjonariuszId LONG NOT NULL,
            TypOdznaczeniaId LONG NOT NULL,
            DataNadania DATETIME NOT NULL
        )
        """,
        """
        CREATE TABLE Uzytkownicy (
            Id AUTOINCREMENT PRIMARY KEY,
            Login TEXT(50) NOT NULL,
            Rola SHORT NOT NULL,
            NumerZmiany SHORT,
            HasloHash TEXT(128) NOT NULL,
            HasloSol TEXT(64) NOT NULL
        )
        """,
        """
        CREATE TABLE UstawieniaAplikacji (
            Klucz TEXT(50) PRIMARY KEY,
            Wartosc TEXT(255) NOT NULL
        )
        """
    ];
}
