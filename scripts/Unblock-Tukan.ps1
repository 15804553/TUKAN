<#
.SYNOPSIS
    Usuwa Mark of the Web (Zone.Identifier) z plików TUKAN - uzupełnienie, nie zamiennik Authenticode.

.PARAMETER Path
    Katalog lub plik do odblokowania.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $Path)) {
    throw "Ścieżka nie istnieje: $Path"
}

$items = if (Test-Path -LiteralPath $Path -PathType Leaf) {
    @(Get-Item -LiteralPath $Path)
} else {
    @(Get-ChildItem -LiteralPath $Path -Recurse -File -Include *.exe,*.dll,*.msi,*.msix,*.ps1)
}

$count = 0
foreach ($item in $items) {
    Unblock-File -LiteralPath $item.FullName -ErrorAction SilentlyContinue
    $count++
}

Write-Host "Przetworzono plików: $count"
Write-Host "Uwaga: Unblock nie zastępuje podpisu Authenticode (docs/podpis-authenticode.md)."
