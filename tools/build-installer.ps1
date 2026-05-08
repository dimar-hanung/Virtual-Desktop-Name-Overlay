param(
    [string]$InnoSetupCompilerPath,
    [string]$SignToolPath,
    [string]$CertificateThumbprint,
    [string]$TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$issPath = Join-Path $repoRoot "installer\VirtualDesktopOverlay.iss"
$moduleManifest = Join-Path $repoRoot "modules\VirtualDesktop\1.5.11\VirtualDesktop.psd1"
$distRoot = Join-Path $repoRoot "dist"

function Resolve-InnoCompiler {
    param([string]$ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (Test-Path -LiteralPath $ExplicitPath) {
            return $ExplicitPath
        }

        throw "Inno Setup compiler was not found at $ExplicitPath."
    }

    $fromPath = Get-Command -Name ISCC.exe -ErrorAction SilentlyContinue
    if ($fromPath) {
        return $fromPath.Source
    }

    $commonPaths = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )

    foreach ($path in $commonPaths) {
        if ($path -and (Test-Path -LiteralPath $path)) {
            return $path
        }
    }

    throw "Install Inno Setup 6 or pass -InnoSetupCompilerPath."
}

if (-not (Test-Path -LiteralPath $issPath)) {
    throw "Installer script is missing: $issPath"
}

if (-not (Test-Path -LiteralPath $moduleManifest)) {
    throw "Bundled VirtualDesktop module is missing. Run tools\prepare-dependencies.ps1 first."
}

if (-not (Test-Path -LiteralPath $distRoot)) {
    New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
}

$iscc = Resolve-InnoCompiler -ExplicitPath $InnoSetupCompilerPath
& $iscc $issPath

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compiler failed with exit code $LASTEXITCODE."
}

$installer = Get-ChildItem -LiteralPath $distRoot -Filter "VirtualDesktopOverlaySetup-*.exe" |
    Sort-Object -Property LastWriteTime -Descending |
    Select-Object -First 1

if (-not $installer) {
    throw "Installer executable was not found in $distRoot."
}

if ($SignToolPath -or $CertificateThumbprint) {
    if ([string]::IsNullOrWhiteSpace($SignToolPath) -or -not (Test-Path -LiteralPath $SignToolPath)) {
        throw "Pass a valid -SignToolPath when signing is enabled."
    }

    if ([string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
        throw "Pass -CertificateThumbprint when signing is enabled."
    }

    & $SignToolPath sign /fd SHA256 /tr $TimestampUrl /td SHA256 /sha1 $CertificateThumbprint $installer.FullName

    if ($LASTEXITCODE -ne 0) {
        throw "signtool failed with exit code $LASTEXITCODE."
    }
}

"Created installer: $($installer.FullName)"
