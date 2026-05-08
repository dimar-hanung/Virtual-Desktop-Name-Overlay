param(
    [string]$InnoSetupCompilerPath,
    [string]$SignToolPath,
    [string]$CertificateThumbprint,
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$issPath = Join-Path $repoRoot "installer\VirtualDesktopOverlay.iss"
$projectPath = Join-Path $repoRoot "src\VirtualDesktopOverlay\VirtualDesktopOverlay.csproj"
$distRoot = Join-Path $repoRoot "dist"
$publishRoot = Join-Path $repoRoot "publish\$RuntimeIdentifier"

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

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Native overlay project is missing: $projectPath"
}

if (-not (Test-Path -LiteralPath $distRoot)) {
    New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
}

$iscc = Resolve-InnoCompiler -ExplicitPath $InnoSetupCompilerPath

if (Test-Path -LiteralPath $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime $RuntimeIdentifier `
    --self-contained true `
    --output $publishRoot `
    /p:PublishSingleFile=false `
    /p:PublishReadyToRun=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

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
