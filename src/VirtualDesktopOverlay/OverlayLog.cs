namespace VirtualDesktopOverlay;

internal static class OverlayLog
{
    public static void Write(string message, string level = "INFO")
    {
        try
        {
            Directory.CreateDirectory(OverlayPaths.LogRoot);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(OverlayPaths.LogFile, $"[{timestamp}] [{level}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never prevent the overlay from starting.
        }
    }
}
