# Release Checklist

Use this checklist before sharing a Windows installer outside the development machine.

1. Run `tools\prepare-dependencies.ps1` and confirm `modules\VirtualDesktop\1.5.11\VirtualDesktop.psd1` exists.
2. Run `tools\build-installer.ps1` on a machine with Inno Setup 6 installed.
3. Install on a fresh Windows 10 2004+ or Windows 11 user profile without administrator elevation.
4. Confirm the post-install launch checkbox starts the overlay with PowerShell minimized.
5. Confirm the overlay displays the current virtual desktop name and updates after switching desktops.
6. Confirm right-clicking the overlay hides it to the notification area, and the tray menu can show or exit it.
7. Re-run the launcher while the overlay is open and confirm no duplicate overlay appears.
8. Enable `Start with Windows`, sign out and sign back in, and confirm the overlay starts in the interactive user session.
9. Uninstall while the overlay is running and confirm the installed files are removed.
10. Confirm `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\VirtualDesktopOverlay` is removed after uninstall.
11. Temporarily remove the bundled module from a test install and confirm the overlay shows a clear missing-module state or logs the failure.
12. For public distribution, sign the installer and verify the signature before publishing.
