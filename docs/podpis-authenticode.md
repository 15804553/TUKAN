# Podpis Authenticode (TUKAN)

Dokument dla developera i IT: jak usunąć ostrzeżenie Windows „Nieznany wydawca” / SmartScreen przy uruchamianiu `TUKAN.exe`.

## Co Windows sprawdza

1. **Podpis Authenticode** — czy EXE/DLL są podpisane ważnym certyfikatem Code Signing.
2. **Łańcuch zaufania** — czy wydawca jest zaufany (publiczne CA albo wewnętrzne CA / GPO).
3. **Mark of the Web** — pliki skopiowane z Internetu/sieci mogą mieć `Zone.Identifier` (osobny temat; skrypt `Unblock-File` / właściwość „Odblokuj”).

Nazwa widoczna jako wydawca pochodzi z pola **Subject (CN/O)** certyfikatu, nie z metadanych w `.csproj`.

## Wymagania narzędzi

- Windows PowerShell 5.1+ lub PowerShell 7+
- Opcjonalnie: [Windows SDK](https://developer.microsoft.com/windows/downloads/windows-sdk/) (`signtool.exe`) — skrypt działa też bez niego (`Set-AuthenticodeSignature`)
- Certyfikat **Code Signing**:
  - firmowy z wewnętrznego CA (zalecane w intranecie), albo
  - komercyjny OV/EV Code Signing, albo
  - self-signed tylko do testów na jednym PC

**Nie commituj** plików `.pfx` / haseł do repozytorium.

## Metadane aplikacji

W `Tukan.App.csproj` ustawione są `Company`, `Authors`, `Copyright` — widać je we właściwościach pliku. Wydawca SmartScreen i tak bierze się z certyfikatu.

## Szybki start (test lokalny)

```powershell
# 1) Certyfikat deweloperski + zaufanie na tym PC
.\scripts\New-TukanCodeSigningCert.ps1 -TrustLocally

# 2) Odcisk (skrypt wypisuje Thumbprint) — albo:
$env:TUKAN_SIGN_THUMBPRINT = (Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -like "*TUKAN*" -and $_.HasPrivateKey } |
    Select-Object -First 1).Thumbprint

# 3) Zbuduj / opublikuj Release (x86 / ACE32) albo użyj istniejącego katalogu
dotnet publish src/Tukan.App/Tukan.App.csproj -c Release -r win-x86 --self-contained true -p:Platform=x86 -o src/Tukan.App/bin/x86

# 4) Podpisz TUKAN.exe i własne DLL
.\scripts\Sign-Tukan.ps1 -Path src/Tukan.App/bin/x86

# 5) Weryfikacja
Get-AuthenticodeSignature src/Tukan.App/bin/x86/TUKAN.exe | Format-List *
```

Po podpisie i lokalnym zaufaniu okno „Nieznany wydawca” na **tym** komputerze powinno zniknąć lub zmienić się na nazwę z certyfikatu.

## Produkcja / środowisko pracy (IT)

### Opcja A — wewnętrzne CA + GPO (zalecane w firmie)

1. Wystaw certyfikat **Code Signing** dla wydawcy (np. jednostka / „TUKAN”).
2. Eksportuj publiczną część łańcucha CA.
3. GPO → Computer Configuration → Policies → Windows Settings → Security Settings → Public Key Policies:
   - **Trusted Root Certification Authorities** — root CA
   - **Trusted Publishers** — certyfikat Code Signing (lub CA pośrednie wg polityki)
4. Po buildzie podpisuj artefakty skryptem `Sign-Tukan.ps1` (PFX na maszynie build albo cert w LocalMachine\My).
5. Dystrybuuj już podpisane pliki na stanowiska (najlepiej lokalna kopia programu, nie start całego katalogu z SMB).

### Opcja B — komercyjny certyfikat (DigiCert, Sectigo, …)

1. Kup **Code Signing** (EV szybciej buduje reputację SmartScreen).
2. Podpisuj na buildzie z PFX / tokena HSM zgodnie z wymaganiami dostawcy.
3. Zawsze używaj **timestamp RFC3161** (skrypt domyślnie: `http://timestamp.digicert.com`).

### Opcja C — tylko odblokowanie MotW (nie zastępuje podpisu)

```powershell
Get-ChildItem -Path "\\serwer\udzial\TUKAN" -Recurse -Include *.exe,*.dll |
    Unblock-File
```

Albo GPO: strefa Lokalny intranet dla `\\serwer\udzial`. To zmniejsza ostrzeżenia strefy, ale **nie** pokazuje zaufanego wydawcy jak Authenticode.

## Zmienne środowiskowe

| Zmienna | Znaczenie |
|---------|-----------|
| `TUKAN_SIGN_THUMBPRINT` | Odcisk certyfikatu w magazynie Windows |
| `TUKAN_SIGN_PFX` | Ścieżka do pliku `.pfx` |
| `TUKAN_SIGN_PFX_PASSWORD` | Hasło PFX (nie loguj, nie commituj) |

## Co podpisuje `Sign-Tukan.ps1`

- własne biblioteki w katalogu publish: `TUKAN*.dll`, `BOBER*.dll`, `CHOMIK*` / `Chomik*.dll`, `SKRYBEK*.dll`
- opcjonalnie MSI/MSIX, jeśli trafią do katalogu
- z `-Recurse`: także podkatalogi (zwykle niepotrzebne przy `dotnet publish -o ...`)

Nie podpisuje setek DLL runtime .NET (self-contained) — nie są wymagane do komunikatu wydawcy przy starcie EXE. W razie potrzeby rozszerz filtr w skrypcie.

## Typowe problemy

| Objaw | Przyczyna / działanie |
|-------|------------------------|
| `Status: UnknownError` po podpisie | Brak dostępu do serwera timestamp — sprawdź sieć/firewall |
| `Status: NotTrusted` | Cert nie jest w Trusted Publishers / Root na tym PC |
| SmartScreen nadal ostrzega mimo podpisu | Nowa reputacja pliku (szczególnie OV bez EV) — w intranecie ustaw GPO Trusted Publishers |
| `signtool` nie znaleziony | Zainstaluj Windows SDK albo polegaj na fallbacku PowerShell |
| Hasło PFX w skrypcie w repo | **Zabroń** — tylko env / secret store CI |

## Checklista wdrożenia

- [ ] Certyfikat Code Signing (wewnętrzny CA lub komercyjny)
- [ ] GPO: Trusted Root + Trusted Publishers (środowisko pracy)
- [ ] Pipeline / procedura: publish → `Sign-Tukan.ps1` → weryfikacja `Get-AuthenticodeSignature`
- [ ] Brak `.pfx` w git
- [ ] Timestamp włączony przy każdym podpisie
