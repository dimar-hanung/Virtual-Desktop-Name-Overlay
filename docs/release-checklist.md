# Release Checklist

Use this checklist before sharing a Windows installer outside the development machine.

1. Run `tools\build-installer.ps1` on a machine with .NET SDK 9.x and Inno Setup 6 installed.
2. Confirm the installer output exists in `dist\`.
3. Install on a fresh Windows 10 2004+ or Windows 11 user profile without administrator elevation.
4. Confirm the post-install launch checkbox starts the overlay without a PowerShell window.
5. Confirm the overlay displays the current virtual desktop name and updates after switching desktops.
6. Confirm right-clicking the overlay hides it to the notification area, and the tray menu can show or exit it.
7. Run the app again while the overlay is open and confirm no duplicate overlay appears.
8. Enable `Start with Windows`, sign out and sign back in, and confirm the overlay starts in the interactive user session.
9. Uninstall while the overlay is running and confirm the installed files are removed.
10. Confirm `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\VirtualDesktopOverlay` is removed after uninstall.
11. Confirm no `powershell.exe` child process is needed for normal overlay operation.
12. For public distribution, sign the installer and verify the signature before publishing.
