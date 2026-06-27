# Release Checklist

Use this checklist before sharing a Windows installer outside the development machine.

1. Run `tools\build-installer.ps1` on a machine with .NET SDK 9.x and Inno Setup 6 installed.
2. Confirm the installer output exists in `dist\`.
3. Install on a fresh Windows 10 2004+ or Windows 11 user profile without administrator elevation.
4. Confirm the post-install launch checkbox starts the overlay without a PowerShell window.
5. Confirm the overlay lists all virtual desktops, highlights the active desktop, and updates after switching with `Win+Ctrl+Left` or `Win+Ctrl+Right`.
6. Confirm left-clicking a desktop row switches to that desktop.
7. In `Settings...`, assign a global hotkey to a desktop, save, and confirm the hotkey switches correctly.
8. Restart the app and confirm configured desktop hotkeys still work.
9. Confirm right-clicking the overlay hides it to the notification area, and the tray menu can show or exit it.
10. Confirm dragging the top strip moves the overlay and the taller list layout still pins across desktops.
11. Run the app again while the overlay is open and confirm no duplicate overlay appears.
12. Enable `Start with Windows`, sign out and sign back in, and confirm the overlay starts in the interactive user session.
13. Uninstall while the overlay is running and confirm the installed files are removed.
14. Confirm `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\VirtualDesktopOverlay` is removed after uninstall.
15. Confirm no `powershell.exe` child process is needed for normal overlay operation.
16. For public distribution, sign the installer and verify the signature before publishing.
