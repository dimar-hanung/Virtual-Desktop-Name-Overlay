$AppDisplayName = "Virtual Desktop Overlay"
$AppDataRoot = Join-Path ([Environment]::GetFolderPath("LocalApplicationData")) "VirtualDesktopOverlay"
$LogRoot = Join-Path $AppDataRoot "logs"
$LogFile = Join-Path $LogRoot "overlay.log"
$RequiredVirtualDesktopVersion = "1.5.11"
$BundledVirtualDesktopManifest = Join-Path $PSScriptRoot "modules\VirtualDesktop\$RequiredVirtualDesktopVersion\VirtualDesktop.psd1"
$RequiredVirtualDesktopCommands = @(
    "Get-CurrentDesktop",
    "Get-DesktopName",
    "Get-DesktopIndex",
    "Pin-Window"
)

$script:SingleInstanceMutex = $null
$script:HasSingleInstanceMutex = $false
$script:VirtualDesktopReady = $false
$script:StartupMessage = $null

function Write-OverlayLog {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,

        [string]$Level = "INFO"
    )

    try {
        if (-not (Test-Path -LiteralPath $LogRoot)) {
            New-Item -ItemType Directory -Path $LogRoot -Force | Out-Null
        }

        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Add-Content -LiteralPath $LogFile -Value "[$timestamp] [$Level] $Message" -Encoding UTF8
    }
    catch {
        # Logging must never prevent the overlay from starting.
    }
}

function Start-SingleInstance {
    try {
        $createdNew = $false
        $script:SingleInstanceMutex = New-Object System.Threading.Mutex($true, "Local\VirtualDesktopOverlay.SingleInstance", [ref]$createdNew)
        $script:HasSingleInstanceMutex = $createdNew

        if (-not $createdNew) {
            Write-OverlayLog "Another overlay instance is already running. Exiting."
            return $false
        }

        return $true
    }
    catch {
        Write-OverlayLog "Failed to create single-instance mutex: $($_.Exception.Message)" "WARN"
        return $true
    }
}

function Stop-SingleInstance {
    if ($script:SingleInstanceMutex -eq $null) {
        return
    }

    try {
        if ($script:HasSingleInstanceMutex) {
            $script:SingleInstanceMutex.ReleaseMutex()
        }
    }
    catch {
        Write-OverlayLog "Failed to release single-instance mutex: $($_.Exception.Message)" "WARN"
    }
    finally {
        $script:SingleInstanceMutex.Dispose()
        $script:SingleInstanceMutex = $null
        $script:HasSingleInstanceMutex = $false
    }
}

function Initialize-WinForms {
    try {
        Add-Type -AssemblyName System.Windows.Forms
        Add-Type -AssemblyName System.Drawing
        [System.Windows.Forms.Application]::EnableVisualStyles()
        return $null
    }
    catch {
        $message = "Windows Forms unavailable"
        Write-OverlayLog "${message}: $($_.Exception.Message)" "ERROR"
        return $message
    }
}

function Test-SupportedWindowsVersion {
    try {
        $version = [Environment]::OSVersion.Version
        return ($version.Major -gt 10) -or (($version.Major -eq 10) -and ($version.Build -ge 19041))
    }
    catch {
        Write-OverlayLog "Failed to detect Windows version: $($_.Exception.Message)" "WARN"
        return $false
    }
}

function Initialize-VirtualDesktopModule {
    try {
        if (Test-Path -LiteralPath $BundledVirtualDesktopManifest) {
            Import-Module -Name $BundledVirtualDesktopManifest -ErrorAction Stop
            Write-OverlayLog "Loaded bundled VirtualDesktop module from $BundledVirtualDesktopManifest."
        }
        else {
            Write-OverlayLog "Bundled VirtualDesktop module not found at $BundledVirtualDesktopManifest. Trying global module fallback." "WARN"
            Import-Module -Name VirtualDesktop -ErrorAction Stop
            Write-OverlayLog "Loaded global VirtualDesktop module fallback."
        }
    }
    catch {
        Write-OverlayLog "Failed to load VirtualDesktop module: $($_.Exception.Message)" "ERROR"
        return "VirtualDesktop module missing"
    }

    $missingCommands = @($RequiredVirtualDesktopCommands | Where-Object {
        -not (Get-Command -Name $_ -ErrorAction SilentlyContinue)
    })

    if ($missingCommands.Count -gt 0) {
        Write-OverlayLog "VirtualDesktop module is missing required commands: $($missingCommands -join ', ')" "ERROR"
        return "VirtualDesktop command missing"
    }

    $script:VirtualDesktopReady = $true
    return $null
}

function Get-CurrentVirtualDesktopName {
    if (-not $script:VirtualDesktopReady) {
        return $script:StartupMessage
    }

    try {
        $desktop = Get-CurrentDesktop
        $name = Get-DesktopName $desktop

        if ([string]::IsNullOrWhiteSpace($name)) {
            $index = Get-DesktopIndex $desktop
            return "Desktop $($index + 1)"
        }

        return $name
    }
    catch {
        Write-OverlayLog "Failed to read current virtual desktop: $($_.Exception.Message)" "WARN"
        return "Unknown desktop"
    }
}

if (-not (Start-SingleInstance)) {
    return
}

try {
    $winFormsError = Initialize-WinForms
    if ($winFormsError) {
        Write-Error $winFormsError
        return
    }

    if (-not (Test-SupportedWindowsVersion)) {
        $script:StartupMessage = "Unsupported Windows version"
        Write-OverlayLog "Unsupported Windows version. Windows 10 2004 build 19041 or later is required." "ERROR"
    }
    else {
        $script:StartupMessage = Initialize-VirtualDesktopModule
    }

    if ([string]::IsNullOrWhiteSpace($script:StartupMessage)) {
        $script:StartupMessage = "Starting..."
    }

    $form = New-Object System.Windows.Forms.Form
    $form.Text = $AppDisplayName
    $form.FormBorderStyle = "None"
    $form.TopMost = $true
    $form.ShowInTaskbar = $false
    $form.StartPosition = "Manual"
    $form.BackColor = [System.Drawing.Color]::Black
    $form.Opacity = 0.75
    $form.Width = 260
    $form.Height = 42

    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
    $form.Left = $screen.Right - $form.Width - 20
    $form.Top = $screen.Bottom - $form.Height - 20

    $label = New-Object System.Windows.Forms.Label
    $label.Dock = "Fill"
    $label.TextAlign = "MiddleCenter"
    $label.ForeColor = [System.Drawing.Color]::White
    $label.BackColor = [System.Drawing.Color]::Transparent
    $label.Font = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
    $label.Text = Get-CurrentVirtualDesktopName

    $form.Controls.Add($label)

    $refreshTimer = New-Object System.Windows.Forms.Timer
    $refreshTimer.Interval = 1000
    $refreshTimer.Add_Tick({
        $label.Text = Get-CurrentVirtualDesktopName
    })
    $refreshTimer.Start()

    $closeOnRightClick = {
        if ($_.Button -eq [System.Windows.Forms.MouseButtons]::Right) {
            $form.Close()
        }
    }

    $form.Add_MouseUp($closeOnRightClick)
    $label.Add_MouseUp($closeOnRightClick)

    $form.Add_FormClosed({
        if ($refreshTimer -ne $null) {
            $refreshTimer.Stop()
            $refreshTimer.Dispose()
        }
    })

    $form.Add_Shown({
        if (-not $script:VirtualDesktopReady) {
            return
        }

        $pinTimer = New-Object System.Windows.Forms.Timer
        $pinTimer.Interval = 500

        $pinTimer.Add_Tick({
            param($sender, $eventArgs)

            $sender.Stop()
            $sender.Dispose()

            try {
                Pin-Window $form.Handle
                $label.Text = Get-CurrentVirtualDesktopName
            }
            catch {
                Write-OverlayLog "Failed to pin overlay window: $($_.Exception.Message)" "WARN"
                $label.Text = "Could not pin overlay"
            }
        })

        $pinTimer.Start()
    })

    [System.Windows.Forms.Application]::Run($form)
}
finally {
    Stop-SingleInstance
}
