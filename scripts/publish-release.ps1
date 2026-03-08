param(
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "ZantesEngine\ZantesEngine.csproj"
$outDir = Join-Path $root "release\$Runtime"

Write-Host "Publishing $project -> $outDir"

dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishTrimmed=false `
    -o $outDir

Write-Host "Done. Output: $outDir"
