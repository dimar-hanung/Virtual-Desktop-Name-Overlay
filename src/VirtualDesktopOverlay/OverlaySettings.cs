using System.Text.Json;

namespace VirtualDesktopOverlay;

internal sealed class OverlaySettings
{
    public const string DarkTheme = "Dark";
    public const string LightTheme = "Light";
    public const string LikeCurrentDesign = "LikeCurrent";
    public const string FlexDesign = "Flex";
    public const string JustShowActiveDesign = "JustShowActive";
    public const double DefaultOpacity = 0.75;
    public const int DefaultFontSize = 11;
    public const int MinFontSize = 8;
    public const int MaxFontSize = 18;
    public const int MaxDesktopHotkeySlots = 9;

    public int Left { get; set; }
    public int Top { get; set; }
    public string Theme { get; set; } = DarkTheme;
    public string DesignType { get; set; } = LikeCurrentDesign;
    public int FontSize { get; set; } = DefaultFontSize;
    public double Opacity { get; set; } = DefaultOpacity;
    public List<DesktopHotkeyBinding> DesktopHotkeys { get; set; } = CreateDefaultDesktopHotkeys();

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
        settings.DesignType = NormalizeDesignType(settings.DesignType);
        settings.FontSize = Math.Clamp(settings.FontSize, MinFontSize, MaxFontSize);
        settings.DesktopHotkeys = NormalizeDesktopHotkeys(settings.DesktopHotkeys);
        return settings;
    }

    private static string NormalizeDesignType(string? designType)
    {
        if (string.Equals(designType, FlexDesign, StringComparison.OrdinalIgnoreCase))
        {
            return FlexDesign;
        }

        if (string.Equals(designType, JustShowActiveDesign, StringComparison.OrdinalIgnoreCase))
        {
            return JustShowActiveDesign;
        }

        return LikeCurrentDesign;
    }

    private static List<DesktopHotkeyBinding> CreateDefaultDesktopHotkeys()
    {
        var bindings = new List<DesktopHotkeyBinding>();
        for (var index = 0; index < MaxDesktopHotkeySlots; index++)
        {
            bindings.Add(new DesktopHotkeyBinding { DesktopIndex = index });
        }

        return bindings;
    }

    private static List<DesktopHotkeyBinding> NormalizeDesktopHotkeys(List<DesktopHotkeyBinding>? bindings)
    {
        var normalized = CreateDefaultDesktopHotkeys();
        if (bindings is null)
        {
            return normalized;
        }

        var seenKeys = new HashSet<(int Modifiers, int Key)>();
        foreach (var binding in bindings)
        {
            if (binding.DesktopIndex < 0 || binding.DesktopIndex >= MaxDesktopHotkeySlots)
            {
                continue;
            }

            if (!binding.IsConfigured)
            {
                continue;
            }

            var key = (binding.Modifiers, binding.Key);
            if (!seenKeys.Add(key))
            {
                continue;
            }

            normalized[binding.DesktopIndex] = new DesktopHotkeyBinding
            {
                DesktopIndex = binding.DesktopIndex,
                Modifiers = binding.Modifiers,
                Key = binding.Key
            };
        }

        return normalized;
    }
}
