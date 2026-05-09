using System.Text.Json;

namespace VirtualDesktopOverlay;

internal sealed class OverlaySettings
{
    public const string DarkTheme = "Dark";
    public const string LightTheme = "Light";
    public const double DefaultOpacity = 0.75;

    public int Left { get; set; }
    public int Top { get; set; }
    public string Theme { get; set; } = DarkTheme;
    public double Opacity { get; set; } = DefaultOpacity;

    public static OverlaySettings Load()
    {
        try
        {
            if (!File.Exists(OverlayPaths.SettingsFile))
            {
                return new OverlaySettings();
            }

            var settings = JsonSerializer.Deserialize<OverlaySettings>(File.ReadAllText(OverlayPaths.SettingsFile));
            return settings is null ? new OverlaySettings() : Normalize(settings);
        }
        catch (Exception ex)
        {
            OverlayLog.Write($"Failed to load overlay settings: {ex.Message}", "WARN");
            return new OverlaySettings();
        }
    }

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
            OverlayLog.Write($"Failed to load overlay position: {ex.Message}", "WARN");
            return null;
        }
    }

    public static void SavePosition(Form form)
    {
        var settings = Load();
        settings.Left = form.Left;
        settings.Top = form.Top;
        Save(settings);
    }

    public static void Save(OverlaySettings settings)
    {
        try
        {
            Directory.CreateDirectory(OverlayPaths.AppDataRoot);
            var json = JsonSerializer.Serialize(Normalize(settings), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(OverlayPaths.SettingsFile, json);
        }
        catch (Exception ex)
        {
            OverlayLog.Write($"Failed to save overlay settings: {ex.Message}", "WARN");
        }
    }

    private static OverlaySettings Normalize(OverlaySettings settings)
    {
        if (!string.Equals(settings.Theme, LightTheme, StringComparison.OrdinalIgnoreCase))
        {
            settings.Theme = DarkTheme;
        }
        else
        {
            settings.Theme = LightTheme;
        }

        settings.Opacity = Math.Clamp(settings.Opacity, 0.3, 1.0);
        return settings;
    }
}
