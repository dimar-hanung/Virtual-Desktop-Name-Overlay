namespace VirtualDesktopOverlay;

internal static class OverlayPaths
{
    public static readonly string AppDataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VirtualDesktopOverlay");

    public static readonly string LogRoot = Path.Combine(AppDataRoot, "logs");
    public static readonly string LogFile = Path.Combine(LogRoot, "overlay.log");
    public static readonly string SettingsFile = Path.Combine(AppDataRoot, "settings.json");
}
