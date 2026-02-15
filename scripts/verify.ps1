param(
    [switch]$NoTest
)

$ErrorActionPreference = "Stop"

function Stop-LingeringTestHosts {
    $repoMarker = "LagerPalleSortering.Tests"
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

Stop-LingeringTestHosts
dotnet build LagerPalleSortering.slnx

if (-not $NoTest) {
    dotnet test LagerPalleSortering.slnx --no-build
}
