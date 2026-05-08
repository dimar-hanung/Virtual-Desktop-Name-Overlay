Option Explicit

Dim fso
Dim shell
Dim scriptPath
Dim scriptDirectory
Dim overlayScript
Dim command

Set fso = CreateObject("Scripting.FileSystemObject")
Set shell = CreateObject("WScript.Shell")

scriptPath = WScript.ScriptFullName
scriptDirectory = fso.GetParentFolderName(scriptPath)
overlayScript = fso.BuildPath(scriptDirectory, "VirtualDesktopOverlay.ps1")

command = "powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -WindowStyle Hidden -File """ & overlayScript & """"
shell.Run command, 0, False
