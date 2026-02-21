param(
    [string]$OutputRoot = "",
    [int]$Port = 5050,
    # Keep false by default to avoid committing binary artifacts to git history.
    [bool]$SyncTrackedZip = $false
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path (Split-Path $projectRoot -Parent) "LagerPalleSortering-work-package"
}

$publishDir = Join-Path $OutputRoot "app"
$zipPath = Join-Path $OutputRoot "LagerPalleSortering-work.zip"
$trackedZipPath = Join-Path $projectRoot "work-package\LagerPalleSortering-work.zip"

if (Test-Path $OutputRoot) {
    Remove-Item $OutputRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDir | Out-Null

Push-Location $projectRoot
try {
    dotnet publish ".\LagerPalleSortering.csproj" `
        -c Release `
        -r win-x64 `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishDir

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish fejlede."
    }
}
finally {
    Pop-Location
}

New-Item -ItemType Directory -Path (Join-Path $publishDir "App_Data") -Force | Out-Null

$productionSettings = @"
{
  "DisableHttpsRedirection": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
"@
Set-Content -Path (Join-Path $publishDir "appsettings.Production.json") -Value $productionSettings -Encoding UTF8

$startCmd = @"
@echo off
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://127.0.0.1:$Port
start "" "http://127.0.0.1:$Port"
"%~dp0LagerPalleSortering.exe"
"@
Set-Content -Path (Join-Path $publishDir "Start-Lager.cmd") -Value $startCmd -Encoding UTF8

$generatedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$buildCommit = "ukendt"
try {
    $buildCommit = (git -C $projectRoot rev-parse --short HEAD).Trim()
}
catch {
    # Best effort metadata only.
}
$readme = @"
LagerPalleSortering - Arbejdspakke
==================================
Buildet: $generatedAt
Commit: $buildCommit

Start
-----
1) Pak filerne ud i en mappe (fx C:\LagerPalleSortering).
2) Dobbeltklik Start-Lager.cmd.
3) Appen åbner i browser på http://127.0.0.1:$Port.
4) Luk appen ved at lukke terminal-vinduet.

Indhold i pakken
----------------
- Backend + React frontend er inkluderet og klar til brug.
- Ingen installation af .NET eller Node.js er nødvendig på arbejds-pc.
- Palleindhold kan printes i standardformat eller 190x100 SVG-labelformat.

Data og backup
--------------
- Data gemmes i App_Data\lager.db i samme mappe.
- Tag backup via appen før større ændringer.

Scanner-tip
-----------
- Hvis scanner-layout ikke matcher Windows-layout, kan ':' blive til 'æ'.
- Palle-scan i appen håndterer dette, men matchende layout anbefales stadig.
"@
Set-Content -Path (Join-Path $publishDir "README_WORK.txt") -Value $readme -Encoding UTF8

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

if ($SyncTrackedZip) {
    New-Item -ItemType Directory -Path (Split-Path $trackedZipPath -Parent) -Force | Out-Null
    Copy-Item -Path $zipPath -Destination $trackedZipPath -Force
    Write-Host "Tracked zip opdateret: $trackedZipPath"
}

Write-Host "Work package klar:"
Write-Host "Folder: $publishDir"
Write-Host "Zip:    $zipPath"

