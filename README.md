# TUKAN

Jedna aplikacja desktopowa WPF (.NET 10) do zarządzania personelem, grafikiem służb i rozkazami dziennymi.

**TUKAN jest w pełni samodzielnym repozytorium** — moduły CHOMIK, BOBER i SKRYBEK są wbudowane w katalogu `vendor/` i nie wymagają osobnych repozytoriów ani folderów obok projektu.

## Wymagania

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Microsoft Access Database Engine](https://www.microsoft.com/en-us/download/details.aspx?id=54920) (ACE OLEDB 12.0, **x64**)

## Klonowanie i uruchomienie

```powershell
git clone https://github.com/15804553/TUKAN.git
cd TUKAN
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
TUKAN/
├── src/Tukan.App/          ← jedyny program uruchamialny
└── vendor/
    ├── CHOMIK/             ← moduł personelu (biblioteka)
    ├── BOBER/              ← moduł grafiku (biblioteka)
    └── SKRYBEK/            ← moduł rozkazów (biblioteka)
```

Kod w `vendor/` jest kopią modułów skompilowaną jako biblioteki DLL (`TukanIntegration=true`). Osobne repozytoria CHOMIK, BOBER i SKRYBEK **nie są wymagane** do budowy ani uruchomienia TUKAN.
