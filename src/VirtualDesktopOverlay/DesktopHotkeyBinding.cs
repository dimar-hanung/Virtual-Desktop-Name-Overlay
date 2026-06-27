namespace VirtualDesktopOverlay;

internal sealed class DesktopHotkeyBinding
{
    public int DesktopIndex { get; set; }

    public int Modifiers { get; set; }

    public int Key { get; set; }

    public bool IsConfigured => Key != 0;

    public string DisplayText
    {
        get
        {
            if (!IsConfigured)
            {
                return "(not set)";
            }

            var parts = new List<string>();
            var modifiers = (NativeHotkey.KeyModifiers)Modifiers;

            if (modifiers.HasFlag(NativeHotkey.KeyModifiers.Control))
            {
                parts.Add("Ctrl");
            }

            if (modifiers.HasFlag(NativeHotkey.KeyModifiers.Alt))
            {
                parts.Add("Alt");
            }

            if (modifiers.HasFlag(NativeHotkey.KeyModifiers.Shift))
            {
                parts.Add("Shift");
            }

            if (modifiers.HasFlag(NativeHotkey.KeyModifiers.Win))
            {
                parts.Add("Win");
            }

            parts.Add(((Keys)Key).ToString());
            return string.Join("+", parts);
        }
    }

    public NativeHotkey.KeyModifiers GetModifiers() => (NativeHotkey.KeyModifiers)Modifiers;

    public Keys GetKey() => (Keys)Key;
}
