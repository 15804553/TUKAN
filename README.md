# TUKAN

Jedna aplikacja desktopowa WPF (.NET 10) do zarządzania personelem, grafikiem służb i rozkazami dziennymi. Łączy moduły **CHOMIK**, **BOBER** i **SKRYBEK** jako biblioteki UI — nie uruchamia się ich jako osobnych programów.

## Wymagania

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Microsoft Access Database Engine](https://www.microsoft.com/en-us/download/details.aspx?id=54920) (ACE OLEDB 12.0, **x64**)

## Uruchomienie

```powershell
dotnet run --project src/Tukan.App/Tukan.App.csproj
```

Przy pierwszym uruchomieniu TUKAN **automatycznie scala** dane z wcześniejszych instalacji CHOMIK / BOBER / SKRYBEK (jeśli są na dysku) do **jednego pliku** `TukanDatabase.accdb` (hasło: `5359`). Szczegóły w `TukanMigration.log`.

W katalogu programu:

| Plik | Opis |
|------|------|
| **`TukanDatabase.accdb`** | **Jedna wspólna baza** — personel, grafik i rozkazy |
| `databasepath.txt` | Ścieżka do wspólnej bazy (tworzona/aktualizowana automatycznie) |
| `Stopnie.txt`, `Stanowiska.txt` | Słowniki personelu |

Stare pliki `ChomikDatabase.accdb`, `BoberDatabase.accdb`, `SkrybekDatabase.accdb` w katalogu programu są archiwizowane jako `*.legacy.bak`.

## Logowanie

Jedno logowanie synchronizuje sesję we wszystkich trzech modułach. Konta i hasła pochodzą z bazy personelu.

| Login | Hasło |
|-------|-------|
| PA | *(brak)* |
| Zmiana 1–4 | 1111 / 2222 / 3333 / 4444 |
| DCA JRG | 0000 |
| Administrator | 5359 |

## Interfejs

- **Domyślny widok** po zalogowaniu: heatmapa / widok ogólny personelu
- **Lewy panel** (zwijany): nawigacja między modułami w jednym oknie
- **Ustawienia**: zakładki Wygląd / Personel / Grafik / Rozkazy

## Architektura

```
Tukan.App          ← jedyny program uruchamialny
 ├── Chomik.App    (biblioteka — personel)
 ├── BOBER.App     (biblioteka — grafik)
 └── SKRYBEK.App   (biblioteka — rozkazy)
```

Repozytoria CHOMIK, BOBER i SKRYBEK służą wyłącznie jako moduły biblioteczne wbudowane w TUKAN.
