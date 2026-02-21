param(
  [string]$AppUrl = "http://localhost:5000/app",
  [string]$Browser = "edge"
)

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$profileRoot = Join-Path $workspaceRoot "App_Data\browser-kiosk-profile"
New-Item -ItemType Directory -Force -Path $profileRoot | Out-Null

function Resolve-BrowserExecutable([string]$browserName) {
  if ($browserName -ieq "chrome") {
    $chromePaths = @(
      "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
      "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe"
    )

    foreach ($path in $chromePaths) {
      if (Test-Path $path) { return $path }
    }

    throw "Chrome blev ikke fundet. Angiv -Browser edge eller installer Chrome."
  }

  $edgePaths = @(
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
  )

  foreach ($path in $edgePaths) {
    if (Test-Path $path) { return $path }
  }

  throw "Edge blev ikke fundet. Angiv -Browser chrome eller installer Edge."
}

$browserExe = Resolve-BrowserExecutable -browserName $Browser
$profileArg = "--user-data-dir=$profileRoot"
$appArg = "--app=$AppUrl"

$args = @(
  "--new-window",
  $appArg,
  "--kiosk-printing",
  "--no-first-run",
  "--no-default-browser-check",
  $profileArg
)

Write-Host "Starter kiosk-print browser med URL: $AppUrl"
Write-Host "Browserprofil: $profileRoot"
Start-Process -FilePath $browserExe -ArgumentList $args | Out-Null
