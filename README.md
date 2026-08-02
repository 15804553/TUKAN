# TUKAN

Jedna aplikacja desktopowa WPF (.NET 10) do zarządzania personelem, grafikiem służb i rozkazami dziennymi.

**TUKAN jest samodzielną aplikacją** — kod domenowy personelu, grafiku i rozkazów leży w `vendor/` jako biblioteki (`TukanIntegration=true`). Nie uruchamia się ich osobno; jedyny program to `TUKAN.exe`.

## Wymagania

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Microsoft Access Database Engine](https://www.microsoft.com/en-us/download/details.aspx?id=54920) (ACE OLEDB 12.0, **x64**)

## Klonowanie i uruchomienie

```powershell
git clone https://github.com/15804553/TUKAN.git
cd TUKAN
dotnet run --project src/Tukan.App/Tukan.App.csproj
```

Przy pierwszym uruchomieniu TUKAN tworzy wspólną bazę `TukanDatabase.accdb` (hasło: `5359`) w katalogu programu.

| Plik | Opis |
|------|------|
| **`TukanDatabase.accdb`** | Wspólna baza — personel, grafik i rozkazy |
| `databasepath.txt` | Ścieżka do bazy (tworzona/aktualizowana automatycznie) |

Słowniki stopni, stanowisk, uprawnień oraz medali/odznaczeń zarządza użytkownik **DCA JRG** w **Ustawieniach**.

## Logowanie

Jedno logowanie synchronizuje sesję we wszystkich obszarach aplikacji. Konta i hasła pochodzą z bazy personelu.

| Login | Hasło |
|-------|-------|
| PA | *(brak)* |
| Zmiana 1–4 | 1111 / 2222 / 3333 / 4444 |
| DCA JRG | 0000 |
| Administrator | 5359 |

## Interfejs

- **Domyślny widok** po zalogowaniu: heatmapa / widok ogólny personelu
- **Lewy panel** (zwijany): nawigacja między obszarami w jednym oknie
- **Ustawienia**: zakładki Wygląd / Personel / Grafik / Rozkazy

## Architektura

```
TUKAN/
├── src/Tukan.App/          ← jedyny program uruchamialny
└── vendor/
    ├── CHOMIK/             ← biblioteka: personel
    ├── BOBER/              ← biblioteka: grafik
    └── SKRYBEK/            ← biblioteka: rozkazy
```

Kolory ról (grafik / rozkazy) pochodzą z domyślnych stałych (`RoleKeys`) oraz tabeli `KoloryStanowisk` we wspólnej bazie — edycja w ustawieniach grafiku.

## Podpis Authenticode

Aby Windows nie pokazywał „Nieznany wydawca”, podpisz artefakty po publish:

```powershell
dotnet publish src/Tukan.App/Tukan.App.csproj -c Release -r win-x86 --self-contained true -p:Platform=x86 -o src/Tukan.App/bin/x86
.\scripts\Sign-Tukan.ps1 -Path src/Tukan.App/bin/x86
```

Instrukcja certyfikatu, GPO i testów lokalnych: [docs/podpis-authenticode.md](docs/podpis-authenticode.md).
