$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$issPath = Join-Path $root "installer\ZantesTweak.iss"
$publishDir = Join-Path $root "release\win-x64"
$exePath = Join-Path $publishDir "ZantesEngine.exe"
$projectPath = Join-Path $root "ZantesEngine\ZantesEngine.csproj"

$possible = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)

$iscc = $possible | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    throw "Inno Setup ISCC.exe not found. Install Inno Setup 6 first."
}

if (-not (Test-Path $exePath)) {
    throw "Publish output not found: $exePath . First run scripts\publish-release.ps1"
}

[xml]$projectXml = Get-Content $projectPath
$version = @($projectXml.Project.PropertyGroup.Version | Where-Object { $_ }) | Select-Object -First 1
if (-not $version) {
    throw "Could not resolve <Version> from $projectPath"
}

Write-Host "Building installer with $iscc"
& $iscc "/DMySourceDir=$publishDir" "/DMyAppVersion=$version" "/DMyOutputBaseFilename=ZantesTweak-Setup-$version" $issPath

Write-Host "Installer output: $root\release\installer"
