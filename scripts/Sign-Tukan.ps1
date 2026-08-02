<#
.SYNOPSIS
    Podpisuje Authenticode pliki TUKAN (EXE/DLL) po buildzie lub publish.

.DESCRIPTION
    Wymaga certyfikatu Code Signing (PFX albo odcisk z magazynu Windows).
    Preferuje signtool.exe (Windows SDK); przy braku używa Set-AuthenticodeSignature.
    Timestamp RFC3161 chroni ważność podpisu po wygaśnięciu certyfikatu.

.PARAMETER Path
    Plik lub katalog do podpisania. Domyślnie: katalog publish Release x86.

.PARAMETER CertificatePath
    Ścieżka do pliku .pfx (albo zmienna środowiskowa TUKAN_SIGN_PFX).

.PARAMETER CertificatePassword
    Hasło do PFX (albo TUKAN_SIGN_PFX_PASSWORD). Unikaj hardcodowania w skryptach.

.PARAMETER CertificateThumbprint
    Odcisk SHA1 certyfikatu z magazynu (CurrentUser\My lub LocalMachine\My).
    Alternatywa: TUKAN_SIGN_THUMBPRINT.

.PARAMETER TimestampUrl
    Serwer znacznika czasu RFC3161.

.PARAMETER Recurse
    Podpisuj też DLL w podkatalogach (domyślnie: tylko pliki w katalogu głównym).

.EXAMPLE
    .\scripts\Sign-Tukan.ps1 -CertificateThumbprint "ABC123..." -Path .\src\Tukan.App\bin\x86

.EXAMPLE
    $env:TUKAN_SIGN_PFX = "C:\certs\tukan-codesign.pfx"
    $env:TUKAN_SIGN_PFX_PASSWORD = "***"
    .\scripts\Sign-Tukan.ps1
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string] $Path = "",

    [Parameter()]
    [string] $CertificatePath = $env:TUKAN_SIGN_PFX,

    [Parameter()]
    [string] $CertificatePassword = $env:TUKAN_SIGN_PFX_PASSWORD,

    [Parameter()]
    [string] $CertificateThumbprint = $env:TUKAN_SIGN_THUMBPRINT,

    [Parameter()]
    [string] $TimestampUrl = "http://timestamp.digicert.com",

    [Parameter()]
    [switch] $Recurse
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-DefaultPublishPath {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $candidates = @(
        (Join-Path $repoRoot "src\Tukan.App\bin\x86"),
        (Join-Path $repoRoot "src\Tukan.App\bin\x64\Debug\net10.0-windows")
    )
    foreach ($candidate in $candidates) {
        $exe = Join-Path $candidate "TUKAN.exe"
        if (Test-Path -LiteralPath $exe) {
            return $candidate
        }
    }
    return $candidates[0]
}

function Find-SignTool {
    $cmd = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    $kitRoots = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
        "${env:ProgramFiles}\Windows Kits\10\bin"
    )
    foreach ($root in $kitRoots) {
        if (-not (Test-Path -LiteralPath $root)) {
            continue
        }
        $found = Get-ChildItem -Path $root -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($found) {
            return $found.FullName
        }
    }
    return $null
}

function Get-SigningCertificate {
    if ($CertificatePath) {
        if (-not (Test-Path -LiteralPath $CertificatePath)) {
            throw "Nie znaleziono pliku PFX: $CertificatePath"
        }
        $secure = if ($CertificatePassword) {
            ConvertTo-SecureString -String $CertificatePassword -AsPlainText -Force
        } else {
            Read-Host -Prompt "Hasło do PFX" -AsSecureString
        }
        return [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
            $CertificatePath,
            $secure,
            [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable
        )
    }

    if (-not $CertificateThumbprint) {
        throw @"
Brak certyfikatu.
Podaj -CertificateThumbprint, -CertificatePath albo ustaw
TUKAN_SIGN_THUMBPRINT / TUKAN_SIGN_PFX (+ opcjonalnie TUKAN_SIGN_PFX_PASSWORD).
Szczegóły: docs/podpis-authenticode.md
"@
    }

    $normalized = ($CertificateThumbprint -replace '\s', '').ToUpperInvariant()
    foreach ($location in @("CurrentUser", "LocalMachine")) {
        $store = [System.Security.Cryptography.X509Certificates.X509Store]::new(
            [System.Security.Cryptography.X509Certificates.StoreName]::My,
            $location
        )
        try {
            $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
            $match = $store.Certificates | Where-Object { $_.Thumbprint -eq $normalized } | Select-Object -First 1
            if ($match) {
                return $match
            }
        } finally {
            $store.Close()
        }
    }
    throw "Nie znaleziono certyfikatu o odcisku $normalized w CurrentUser\My ani LocalMachine\My."
}

function Get-FilesToSign {
    param([string] $Target)

    if (Test-Path -LiteralPath $Target -PathType Leaf) {
        return @(Get-Item -LiteralPath $Target)
    }

    if (-not (Test-Path -LiteralPath $Target -PathType Container)) {
        throw "Ścieżka nie istnieje: $Target"
    }

    $extensions = @("*.exe", "*.dll", "*.msi", "*.msix")
    $files = @()
    foreach ($ext in $extensions) {
        if ($Recurse) {
            $files += Get-ChildItem -LiteralPath $Target -Filter $ext -File -Recurse
        } else {
            $files += Get-ChildItem -LiteralPath $Target -Filter $ext -File
        }
    }

    # Self-contained: nie podpisuj natywnych bibliotek systemowych runtime bez potrzeby -
    # podpisujemy TUKAN.exe oraz własne DLL (TUKAN / BOBER / CHOMIK / SKRYBEK / Chomik).
    $ownNamePattern = '^(TUKAN|BOBER|CHOMIK|Chomik|SKRYBEK)(\.|$)'
    $filtered = $files | Where-Object {
        $_.Name -match $ownNamePattern -or $_.Extension -ieq ".msi" -or $_.Extension -ieq ".msix"
    }

    if (-not $filtered -or $filtered.Count -eq 0) {
        # Fallback: przynajmniej EXE w katalogu głównym
        $filtered = $files | Where-Object { $_.Extension -ieq ".exe" }
    }

    return @($filtered | Sort-Object FullName -Unique)
}

function Invoke-SignWithSignTool {
    param(
        [string] $SignTool,
        [System.IO.FileInfo[]] $Files,
        [System.Security.Cryptography.X509Certificates.X509Certificate2] $Certificate
    )

    $common = @(
        "sign",
        "/fd", "SHA256",
        "/td", "SHA256",
        "/tr", $TimestampUrl,
        "/sha1", $Certificate.Thumbprint,
        "/v"
    )

    foreach ($file in $Files) {
        Write-Host "signtool: $($file.FullName)"
        & $SignTool @common $file.FullName
        if ($LASTEXITCODE -ne 0) {
            throw "signtool zakończył się kodem $LASTEXITCODE dla $($file.FullName)"
        }
    }
}

function Invoke-SignWithPowerShell {
    param(
        [System.IO.FileInfo[]] $Files,
        [System.Security.Cryptography.X509Certificates.X509Certificate2] $Certificate
    )

    foreach ($file in $Files) {
        Write-Host "Set-AuthenticodeSignature: $($file.FullName)"
        $result = Set-AuthenticodeSignature -FilePath $file.FullName -Certificate $Certificate -TimestampServer $TimestampUrl -HashAlgorithm SHA256
        if ($result.Status -ne "Valid" -and $result.Status -ne "UnknownError") {
            # UnknownError bywa przy offline timestamp - StatusMessage i tak warto sprawdzić
            if ($result.Status -ne "Valid") {
                Write-Warning "Status podpisu $($file.Name): $($result.Status) - $($result.StatusMessage)"
            }
        }
        if ($result.Status -eq "HashMismatch" -or $result.Status -eq "NotSigned") {
            throw "Nie udało się podpisać $($file.FullName): $($result.Status) $($result.StatusMessage)"
        }
    }
}

# --- main ---
if (-not $Path) {
    $Path = Resolve-DefaultPublishPath
}

$cert = Get-SigningCertificate
Write-Host "Certyfikat: $($cert.Subject)"
Write-Host "Odcisk:    $($cert.Thumbprint)"
Write-Host "Ważny do:  $($cert.NotAfter.ToString('yyyy-MM-dd'))"

$toSign = Get-FilesToSign -Target $Path
if ($toSign.Count -eq 0) {
    throw "Brak plików EXE/DLL do podpisania w: $Path"
}

Write-Host "Plików do podpisu: $($toSign.Count)"
$signTool = Find-SignTool
if ($signTool) {
    Write-Host "Narzędzie: $signTool"
    Invoke-SignWithSignTool -SignTool $signTool -Files $toSign -Certificate $cert
} else {
    Write-Warning "Brak signtool.exe (zainstaluj Windows SDK). Używam Set-AuthenticodeSignature."
    Invoke-SignWithPowerShell -Files $toSign -Certificate $cert
}

$failed = @()
foreach ($file in $toSign) {
    $sig = Get-AuthenticodeSignature -FilePath $file.FullName
    $status = $sig.Status.ToString()
    $publisher = if ($sig.SignerCertificate) { $sig.SignerCertificate.Subject } else { "(brak)" }
    Write-Host ("[{0}] {1} - {2}" -f $status, $file.Name, $publisher)
    if ($sig.Status -ne "Valid") {
        $failed += $file.FullName
    }
}

if ($failed.Count -gt 0) {
    throw "Podpis niepoprawny dla $($failed.Count) plików. Sprawdź certyfikat i łańcuch zaufania."
}

Write-Host "OK - wszystkie wskazane pliki mają ważny podpis Authenticode."
