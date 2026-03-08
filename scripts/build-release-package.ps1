param(
    [string]$Runtime = "win-x64",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot

& (Join-Path $PSScriptRoot "publish-release.ps1") -Runtime $Runtime

if ($SkipInstaller) {
    Write-Host "SkipInstaller set. Publish complete."
    exit 0
}

try {
    & (Join-Path $PSScriptRoot "build-installer.ps1")
}
catch {
    Write-Warning $_
    Write-Warning "Publish completed but installer was not produced."
    exit 0
}
