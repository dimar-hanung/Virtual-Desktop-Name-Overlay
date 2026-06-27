using System.Runtime.InteropServices;

namespace VirtualDesktopOverlay;

internal static class NativeHotkey
{
    public const int WmHotkey = 0x0312;

    [Flags]
    public enum KeyModifiers : uint
    {
        None = 0,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, KeyModifiers modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
