param(
    [string]$InstallPath = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

try {
    $resolvedInstallPath = [System.IO.Path]::GetFullPath($InstallPath)
    $overlayScript = [System.IO.Path]::GetFullPath((Join-Path $resolvedInstallPath "VirtualDesktopOverlay.ps1"))
}
catch {
    return
}

$currentProcessId = $PID

try {
    $processes = Get-CimInstance -ClassName Win32_Process -Filter "Name = 'powershell.exe' OR Name = 'pwsh.exe'"
}
catch {
    return
}

foreach ($process in $processes) {
    if ($process.ProcessId -eq $currentProcessId) {
        continue
    }

    $commandLine = [string]$process.CommandLine
    if ([string]::IsNullOrWhiteSpace($commandLine)) {
        continue
    }

    $matchesOverlayPath = $commandLine.IndexOf($overlayScript, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    $matchesInstallPath = ($commandLine.IndexOf("VirtualDesktopOverlay.ps1", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -and
        ($commandLine.IndexOf($resolvedInstallPath, [System.StringComparison]::OrdinalIgnoreCase) -ge 0)

    if ($matchesOverlayPath -or $matchesInstallPath) {
        try {
            Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop
        }
        catch {
            # Best-effort cleanup during uninstall.
        }
    }
}
