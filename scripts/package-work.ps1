param(
    [string]$OutputRoot = "",
    [int]$Port = 5050
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path (Split-Path $projectRoot -Parent) "LagerPalleSortering-work-package"
}

$publishDir = Join-Path $OutputRoot "app"
$zipPath = Join-Path $OutputRoot "LagerPalleSortering-work.zip"

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

$readme = @"
LagerPalleSortering - Arbejdspakke
==================================

1) Pak filerne ud i en mappe (fx C:\LagerPalleSortering).
2) Dobbeltklik Start-Lager.cmd.
3) Appen aabner i browser paa http://127.0.0.1:$Port
4) Luk appen ved at lukke terminal-vinduet.

Data gemmes i App_Data\lager.db i samme mappe.
"@
Set-Content -Path (Join-Path $publishDir "README_WORK.txt") -Value $readme -Encoding UTF8

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host "Work package klar:"
Write-Host "Folder: $publishDir"
Write-Host "Zip:    $zipPath"
