using System.Runtime.InteropServices;

namespace VirtualDesktopOverlay;

// Shared undocumented Windows Shell COM types used by pinning and desktop switching.
internal static class ShellComGuids
{
    public static readonly Guid ClsidImmersiveShell = new("C2F03A33-21F5-47FA-B4BB-156362A2F239");
    public static readonly Guid ClsidVirtualDesktopManagerInternal = new("C5E0CDCA-7B6E-41B2-9FC4-D93975CC467B");
    public static readonly Guid ClsidVirtualDesktopPinnedApps = new("B5A399E7-1C87-46B8-88E9-FC5747B171BD");
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
internal interface IServiceProvider10
{
    [return: MarshalAs(UnmanagedType.IUnknown)]
    object QueryService(ref Guid service, ref Guid riid);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
internal interface IObjectArray
{
    void GetCount(out int count);

    void GetAt(int index, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object obj);
}
