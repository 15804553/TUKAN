<#
.SYNOPSIS
    Tworzy lokalny certyfikat Code Signing do testow Authenticode (nie produkcja).

.DESCRIPTION
    Certyfikat self-signed trafia do CurrentUser\My. Aby Windows nie ostrzegal
    na tym PC, skrypt moze tez dodac go do Trusted Root i Trusted Publishers
    (wymaga -TrustLocally).

    W firmie uzyj certyfikatu z wewnetrznego CA albo komercyjnego Code Signing -
    patrz docs/podpis-authenticode.md.

.PARAMETER Subject
    Distinguished Name, np. "CN=TUKAN Dev, O=Twoja Jednostka".

.PARAMETER TrustLocally
    Instaluje certyfikat w Trusted Root + Trusted Publishers biezacego uzytkownika.

.EXAMPLE
    .\scripts\New-TukanCodeSigningCert.ps1 -TrustLocally
    $env:TUKAN_SIGN_THUMBPRINT = (Get-ChildItem Cert:\CurrentUser\My | Where-Object Subject -like "*TUKAN*" | Select-Object -First 1).Thumbprint
    .\scripts\Sign-Tukan.ps1
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string] $Subject = "CN=TUKAN Dev, O=TUKAN",

    [Parameter()]
    [switch] $TrustLocally,

    [Parameter()]
    [int] $ValidYears = 2
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$notAfter = (Get-Date).AddYears($ValidYears)
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Subject `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -NotAfter $notAfter

Write-Host "Utworzono certyfikat:"
Write-Host "  Subject:    $($cert.Subject)"
Write-Host "  Thumbprint: $($cert.Thumbprint)"
Write-Host "  Wazny do:   $($cert.NotAfter.ToString('yyyy-MM-dd'))"
Write-Host ""
Write-Host "Ustaw przed podpisem:"
Write-Host "  `$env:TUKAN_SIGN_THUMBPRINT = '$($cert.Thumbprint)'"

if ($TrustLocally) {
    $rootStore = Get-Item "Cert:\CurrentUser\Root"
    $publisherStore = Get-Item "Cert:\CurrentUser\TrustedPublisher"
    $rootStore.Open("ReadWrite")
    $publisherStore.Open("ReadWrite")
    try {
        $rootStore.Add($cert)
        $publisherStore.Add($cert)
        Write-Host "Dodano do CurrentUser\Root oraz CurrentUser\TrustedPublisher."
        Write-Host "Uwaga: dziala tylko na tym koncie/PC. Na innych stanowiskach ostrzezenie pozostanie."
    } finally {
        $rootStore.Close()
        $publisherStore.Close()
    }
} else {
    Write-Host ""
    Write-Host "Bez -TrustLocally Windows nadal moze pokazywac Nieznany wydawca."
    Write-Host "Uruchom ponownie z -TrustLocally albo wdroz cert przez GPO (Trusted Publishers)."
}

return $cert.Thumbprint