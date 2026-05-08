$AppDisplayName = "Virtual Desktop Overlay"
$AppDataRoot = Join-Path ([Environment]::GetFolderPath("LocalApplicationData")) "VirtualDesktopOverlay"
$LogRoot = Join-Path $AppDataRoot "logs"
$LogFile = Join-Path $LogRoot "overlay.log"
$SettingsFile = Join-Path $AppDataRoot "settings.json"
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

function Get-SavedOverlayPosition {
    try {
        if (-not (Test-Path -LiteralPath $SettingsFile)) {
            return $null
        }

        $settings = Get-Content -LiteralPath $SettingsFile -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
        if (($null -eq $settings.Left) -or ($null -eq $settings.Top)) {
            return $null
        }

        return New-Object System.Drawing.Point([int]$settings.Left, [int]$settings.Top)
    }
    catch {
        Write-OverlayLog "Failed to load overlay settings: $($_.Exception.Message)" "WARN"
        return $null
    }
}

function Test-OverlayPositionVisible {
    param(
        [Parameter(Mandatory = $true)]
        [System.Drawing.Point]$Position,

        [Parameter(Mandatory = $true)]
        [int]$Width,

        [Parameter(Mandatory = $true)]
        [int]$Height
    )

    $overlayBounds = New-Object System.Drawing.Rectangle($Position.X, $Position.Y, $Width, $Height)
    foreach ($screen in [System.Windows.Forms.Screen]::AllScreens) {
        if ($screen.WorkingArea.IntersectsWith($overlayBounds)) {
            return $true
        }
    }

    return $false
}

function Save-OverlayPosition {
    param(
        [Parameter(Mandatory = $true)]
        [System.Windows.Forms.Form]$Form
    )

    try {
        if (-not (Test-Path -LiteralPath $AppDataRoot)) {
            New-Item -ItemType Directory -Path $AppDataRoot -Force | Out-Null
        }

        [pscustomobject]@{
            Left = $Form.Left
            Top = $Form.Top
        } | ConvertTo-Json | Set-Content -LiteralPath $SettingsFile -Encoding UTF8
    }
    catch {
        Write-OverlayLog "Failed to save overlay position: $($_.Exception.Message)" "WARN"
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
    $savedPosition = Get-SavedOverlayPosition
    if (($null -ne $savedPosition) -and (Test-OverlayPositionVisible -Position $savedPosition -Width $form.Width -Height $form.Height)) {
        $form.Left = $savedPosition.X
        $form.Top = $savedPosition.Y
    }
    else {
        $form.Left = $screen.Right - $form.Width - 20
        $form.Top = $screen.Bottom - $form.Height - 20
    }

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

    $dragState = @{
        IsDragging = $false
        CursorStart = $null
        FormStart = $null
    }

    $startDrag = {
        param($sender, $eventArgs)

        if ($eventArgs.Button -ne [System.Windows.Forms.MouseButtons]::Left) {
            return
        }

        $dragState["IsDragging"] = $true
        $dragState["CursorStart"] = [System.Windows.Forms.Cursor]::Position
        $dragState["FormStart"] = $form.Location
        $sender.Capture = $true
    }

    $moveDrag = {
        param($sender, $eventArgs)

        if (-not $dragState["IsDragging"]) {
            return
        }

        $currentCursor = [System.Windows.Forms.Cursor]::Position
        $cursorStart = $dragState["CursorStart"]
        $formStart = $dragState["FormStart"]

        $form.Left = $formStart.X + ($currentCursor.X - $cursorStart.X)
        $form.Top = $formStart.Y + ($currentCursor.Y - $cursorStart.Y)
    }

    $stopDrag = {
        param($sender, $eventArgs)

        if (-not $dragState["IsDragging"]) {
            return
        }

        $dragState["IsDragging"] = $false
        $sender.Capture = $false
        Save-OverlayPosition -Form $form
    }

    $closeOnRightClick = {
        param($sender, $eventArgs)

        if ($eventArgs.Button -eq [System.Windows.Forms.MouseButtons]::Right) {
            $form.Close()
        }
    }

    $form.Add_MouseDown($startDrag)
    $form.Add_MouseMove($moveDrag)
    $form.Add_MouseUp($stopDrag)
    $form.Add_MouseUp($closeOnRightClick)

    $label.Add_MouseDown($startDrag)
    $label.Add_MouseMove($moveDrag)
    $label.Add_MouseUp($stopDrag)
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
