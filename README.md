# Virtual Desktop Overlay

Virtual Desktop Overlay is a lightweight Windows PowerShell overlay that shows the active Windows virtual desktop name. It uses WinForms for the overlay window and the `VirtualDesktop` PowerShell module for Windows virtual desktop APIs.

## Runtime Requirements

- Windows 10 version 2004 build 19041 or later, or Windows 11.
- Windows PowerShell 5.1 through `powershell.exe`.
- Windows Script Host through `wscript.exe` for hidden launches.
- The bundled `VirtualDesktop` PowerShell module version `1.5.11` at `modules\VirtualDesktop\1.5.11\`.

## Running Locally

Run the overlay directly with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File .\VirtualDesktopOverlay.ps1
```

Run it hidden with:

```powershell
wscript.exe .\StartVirtualDesktopOverlay.vbs
```

The overlay prefers the bundled module at `modules\VirtualDesktop\1.5.11\VirtualDesktop.psd1`. If that file is not present, it falls back to a globally installed `VirtualDesktop` module and writes a warning to `%LOCALAPPDATA%\VirtualDesktopOverlay\logs\overlay.log`.

## Installer Behavior

The Inno Setup installer is defined in `installer\VirtualDesktopOverlay.iss`.

- Installs per user under `%APPDATA%\VirtualDesktopOverlay`.
- Does not require administrator elevation.
- Installs `VirtualDesktopOverlay.ps1`, `StartVirtualDesktopOverlay.vbs`, `StopVirtualDesktopOverlay.ps1`, and the bundled `VirtualDesktop` module.
- Offers a selected-by-default `Start with Windows` task that writes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\VirtualDesktopOverlay`.
- Offers a Start Menu shortcut that launches the VBS hidden launcher.
- Offers a post-install launch checkbox.
- On uninstall, removes the HKCU Run value, stops the overlay instance launched from the installed path, and removes app-created data under `%LOCALAPPDATA%\VirtualDesktopOverlay`.

## Build Requirements

- Inno Setup 6.x.
- PowerShellGet `Save-Module` or PSResourceGet `Save-PSResource` for refreshing the bundled dependency.
- Internet access only when running `tools\prepare-dependencies.ps1`.
- Optional code-signing certificate and `signtool.exe` for public distribution.

## Packaging Workflow

Prepare the pinned dependency:

```powershell
.\tools\prepare-dependencies.ps1
```

Compile the installer:

```powershell
.\tools\build-installer.ps1
```

The build script verifies that `modules\VirtualDesktop\1.5.11\VirtualDesktop.psd1` exists before invoking Inno Setup. The installer output is written to `dist\`.

To sign the generated installer, pass signing options to the build script:

```powershell
.\tools\build-installer.ps1 -SignToolPath "C:\Path\To\signtool.exe" -CertificateThumbprint "THUMBPRINT"
```

## Upgrade Behavior

Keep the same Inno `AppId` across releases so upgrades target the existing per-user installation. The current installer uses the same HKCU Run value name for each version, so upgrades should not create duplicate startup entries. The user's startup choice is represented by that Run value.

## Logs and Diagnostics

Runtime diagnostics are written to:

```text
%LOCALAPPDATA%\VirtualDesktopOverlay\logs\overlay.log
```

Startup failures are shown in the overlay when WinForms is available, including unsupported Windows versions, missing `VirtualDesktop`, and missing required module commands.
