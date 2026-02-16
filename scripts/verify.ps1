param(
    [switch]$NoTest
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $projectRoot "LagerPalleSortering.slnx"

function Stop-LingeringTestHosts {
    $repoMarker = [regex]::Escape($projectRoot)
    $processes = Get-CimInstance Win32_Process |
        Where-Object {
            ($_.Name -match "testhost|dotnet") -and
            ($_.CommandLine -match $repoMarker)
        }

    foreach ($p in $processes) {
        try {
            Stop-Process -Id $p.ProcessId -Force -ErrorAction Stop
        }
        catch {
            Write-Warning "Kunne ikke stoppe proces $($p.ProcessId): $($_.Exception.Message)"
        }
    }
}

try {
    Stop-LingeringTestHosts
    dotnet build $solutionPath

    if (-not $NoTest) {
        dotnet test $solutionPath --no-build
    }
}
finally {
    # Ensure no repo test/build processes are left running after verify.
    Stop-LingeringTestHosts
}
