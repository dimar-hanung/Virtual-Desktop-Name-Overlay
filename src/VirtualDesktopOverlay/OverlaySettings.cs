using System.Text.Json;

namespace VirtualDesktopOverlay;

internal sealed class OverlaySettings
{
    public int Left { get; set; }
    public int Top { get; set; }

    public static Point? LoadPosition()
    {
        try
        {
            if (!File.Exists(OverlayPaths.SettingsFile))
            {
                return null;
            }

            var settings = JsonSerializer.Deserialize<OverlaySettings>(File.ReadAllText(OverlayPaths.SettingsFile));
            return settings is null ? null : new Point(settings.Left, settings.Top);
        }
        catch (Exception ex)
        {
            OverlayLog.Write($"Failed to load overlay settings: {ex.Message}", "WARN");
            return null;
        }
    }

    public static void SavePosition(Form form)
    {
        try
        {
            Directory.CreateDirectory(OverlayPaths.AppDataRoot);
            var settings = new OverlaySettings { Left = form.Left, Top = form.Top };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(OverlayPaths.SettingsFile, json);
        }
        catch (Exception ex)
        {
            OverlayLog.Write($"Failed to save overlay position: {ex.Message}", "WARN");
        }
    }
}
