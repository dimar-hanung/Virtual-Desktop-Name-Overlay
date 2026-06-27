# Virtual Desktop Overlay

Virtual Desktop Overlay is a lightweight native Windows WinForms overlay that lists your virtual desktops, highlights the active one, and lets you switch desktops by clicking a row or using configurable global hotkeys. It reads virtual desktop state from Windows and uses COM interop to pin the overlay across desktops and switch between them.

## Runtime Requirements

- Windows 10 version 2004 build 19041 or later, or Windows 11.
- No PowerShell runtime dependency.

## Running Locally

Build and run the native app during development with:

```powershell
dotnet run --project .\src\VirtualDesktopOverlay\VirtualDesktopOverlay.csproj
```

After publishing or installing, run the app directly:

```powershell
.\publish\win-x64\VirtualDesktopOverlay.exe
```

## Overlay Controls

- The overlay lists all virtual desktops. The active desktop is highlighted.
- Left-click a desktop row to switch to that desktop.
- Drag the thin strip at the top of the overlay to move it.
- Right-click the overlay to hide it to the notification area.
- Double-click the tray icon, or choose `Show overlay`, to show it again.
- Choose `Settings...` from the tray icon menu to switch between dark mode and light mode, adjust transparency, and configure global hotkeys for desktops 1 through 9.
- Choose `Exit` from the tray icon menu to close it.
- The moved position, appearance, and desktop shortcut settings are saved to `%LOCALAPPDATA%\VirtualDesktopOverlay\settings.json` and restored on the next launch when the saved position is still visible on an attached screen.

Desktop switching uses undocumented Windows Shell COM APIs (the same class of dependency as cross-desktop pinning). If switching stops working after a Windows update, check for an app update.

## Installer Behavior

The Inno Setup installer is defined in `installer\VirtualDesktopOverlay.iss`.

- Installs per user under `%APPDATA%\VirtualDesktopOverlay`.
- Does not require administrator elevation.
- Installs `VirtualDesktopOverlay.exe` and its published .NET runtime files.
- Offers a selected-by-default `Start with Windows` task that writes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\VirtualDesktopOverlay`.
- Offers a Start Menu shortcut that launches `VirtualDesktopOverlay.exe` directly.
- Offers a post-install launch checkbox.
- On uninstall, removes the HKCU Run value and app-created data under `%LOCALAPPDATA%\VirtualDesktopOverlay`.

## Build Requirements

- Inno Setup 6.x.
- .NET SDK 9.x.
- Optional code-signing certificate and `signtool.exe` for public distribution.

## Packaging Workflow

Compile the installer:

```powershell
.\tools\build-installer.ps1
```

The build script publishes the native WinForms app to `publish\win-x64\` before invoking Inno Setup. The installer output is written to `dist\`.

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

Startup failures are shown in the overlay when possible, including unsupported Windows versions and virtual desktop API errors.
