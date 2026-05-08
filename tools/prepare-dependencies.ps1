param(
    [string]$VirtualDesktopVersion = "1.5.11"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$modulesRoot = Join-Path $repoRoot "modules"
$moduleRoot = Join-Path $modulesRoot "VirtualDesktop"
$versionRoot = Join-Path $moduleRoot $VirtualDesktopVersion
$manifestPath = Join-Path $versionRoot "VirtualDesktop.psd1"

if (-not (Get-Command -Name Save-Module -ErrorAction SilentlyContinue) -and
    -not (Get-Command -Name Save-PSResource -ErrorAction SilentlyContinue)) {
    throw "Install PowerShellGet or PSResourceGet before preparing dependencies."
}

if (Test-Path -LiteralPath $versionRoot) {
    Remove-Item -LiteralPath $versionRoot -Recurse -Force
}

if (-not (Test-Path -LiteralPath $modulesRoot)) {
    New-Item -ItemType Directory -Path $modulesRoot -Force | Out-Null
}

if (Get-Command -Name Save-PSResource -ErrorAction SilentlyContinue) {
    Save-PSResource -Name VirtualDesktop -Version $VirtualDesktopVersion -Path $modulesRoot -TrustRepository -ErrorAction Stop
}
else {
    Save-Module -Name VirtualDesktop -RequiredVersion $VirtualDesktopVersion -Path $modulesRoot -Force -ErrorAction Stop
}

if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "VirtualDesktop $VirtualDesktopVersion was not staged at $manifestPath."
}

$metadata = @"
VirtualDesktop dependency
Name: VirtualDesktop
Version: $VirtualDesktopVersion
Source: https://www.powershellgallery.com/packages/VirtualDesktop/$VirtualDesktopVersion
Repository: https://github.com/MScholtes/PSVirtualDesktop
PreparedAtUtc: $([DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
"@

Set-Content -LiteralPath (Join-Path $moduleRoot "DEPENDENCY.txt") -Value $metadata -Encoding UTF8

"Prepared VirtualDesktop $VirtualDesktopVersion at $versionRoot"
