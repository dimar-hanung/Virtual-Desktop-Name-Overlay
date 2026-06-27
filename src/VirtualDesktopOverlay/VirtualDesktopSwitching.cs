using System.Runtime.InteropServices;

namespace VirtualDesktopOverlay;

// Undocumented Shell COM APIs for listing and switching virtual desktops.
// Interface GUIDs vary by Windows build; see PSVirtualDesktop for reference.
internal static class VirtualDesktopSwitching
{
    private static readonly Guid IidVirtualDesktopWin10 = new("FF72FFDD-BE7E-43FC-9C03-AD81681E88E4");
    private static readonly Guid IidVirtualDesktopWin11 = new("536D3495-B208-4CC9-AE26-DE8111275BF8");
    private static readonly Guid IidVirtualDesktopWin11_22H2 = new("3F07F4BE-B107-441A-AF0F-39D82529072C");

    public static IReadOnlyList<VirtualDesktopComEntry> GetDesktopEntries()
    {
        if (!VirtualDesktopService.IsSupportedWindowsVersion())
        {
            return [];
        }

        try
        {
            return EnumerateFromCom();
        }
        catch (Exception ex)
        {
            OverlayLog.Write($"COM desktop enumeration failed, falling back to registry: {ex.Message}", "WARN");
            return EnumerateFromRegistry();
        }
    }

    public static bool TrySwitchToDesktop(int index, out string? error)
    {
        error = null;

        if (!VirtualDesktopService.IsSupportedWindowsVersion())
        {
            error = "Unsupported Windows version";
            return false;
        }

        object? shell = null;
        object? managerObject = null;
        object? desktopsArrayObject = null;
        object? targetDesktopObject = null;

        try
        {
            shell = Activator.CreateInstance(Type.GetTypeFromCLSID(ShellComGuids.ClsidImmersiveShell, true)!);
            var serviceProvider = (IServiceProvider10)shell!;
            managerObject = CreateDesktopManager(serviceProvider);
            var desktopIid = GetVirtualDesktopInterfaceId();

            GetDesktops(managerObject, out var desktops);
            desktopsArrayObject = desktops;

            desktops.GetCount(out var count);
            if (index < 0 || index >= count)
            {
                error = $"Desktop index {index} is out of range (0..{count - 1})";
                return false;
            }

            desktops.GetAt(index, ref desktopIid, out targetDesktopObject);
            SwitchDesktop(managerObject, targetDesktopObject);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            OverlayLog.Write($"Failed to switch to desktop {index}: {ex.Message}", "WARN");
            return false;
        }
        finally
        {
            ReleaseComObject(targetDesktopObject);
            ReleaseComObject(desktopsArrayObject);
            ReleaseComObject(managerObject);
            ReleaseComObject(shell);
        }
    }

    private static IReadOnlyList<VirtualDesktopComEntry> EnumerateFromCom()
    {
        object? shell = null;
        object? managerObject = null;
        object? desktopsArrayObject = null;
        object? currentDesktopObject = null;

        try
        {
            shell = Activator.CreateInstance(Type.GetTypeFromCLSID(ShellComGuids.ClsidImmersiveShell, true)!);
            var serviceProvider = (IServiceProvider10)shell!;
            managerObject = CreateDesktopManager(serviceProvider);
            var desktopIid = GetVirtualDesktopInterfaceId();

            GetDesktops(managerObject, out var desktops);
            desktopsArrayObject = desktops;

            currentDesktopObject = GetCurrentDesktop(managerObject);
            var currentId = GetDesktopId(currentDesktopObject, desktopIid);

            desktops.GetCount(out var count);
            var entries = new List<VirtualDesktopComEntry>(count);

            for (var index = 0; index < count; index++)
            {
                object? desktopObject = null;
                try
                {
                    desktops.GetAt(index, ref desktopIid, out desktopObject);
                    var id = GetDesktopId(desktopObject, desktopIid);
                    entries.Add(new VirtualDesktopComEntry(index, id, id == currentId));
                }
                finally
                {
                    ReleaseComObject(desktopObject);
                }
            }

            return entries;
        }
        finally
        {
            ReleaseComObject(currentDesktopObject);
            ReleaseComObject(desktopsArrayObject);
            ReleaseComObject(managerObject);
            ReleaseComObject(shell);
        }
    }

    private static IReadOnlyList<VirtualDesktopComEntry> EnumerateFromRegistry()
    {
        var ids = VirtualDesktopService.GetDesktopIdsForSwitching();
        var currentId = VirtualDesktopService.TryGetCurrentDesktopId();

        return ids
            .Select((id, index) => new VirtualDesktopComEntry(index, id, currentId is not null && id == currentId.Value))
            .ToList();
    }

    private static object CreateDesktopManager(IServiceProvider10 serviceProvider)
    {
        var build = Environment.OSVersion.Version.Build;

        if (build >= 22621)
        {
            var service = ShellComGuids.ClsidVirtualDesktopManagerInternal;
            var interfaceId = typeof(IVirtualDesktopManagerInternal22621).GUID;
            return serviceProvider.QueryService(ref service, ref interfaceId);
        }

        if (build >= 22000)
        {
            var service = ShellComGuids.ClsidVirtualDesktopManagerInternal;
            var interfaceId = typeof(IVirtualDesktopManagerInternal22000).GUID;
            return serviceProvider.QueryService(ref service, ref interfaceId);
        }

        var serviceWin10 = ShellComGuids.ClsidVirtualDesktopManagerInternal;
        var interfaceIdWin10 = typeof(IVirtualDesktopManagerInternalWin10).GUID;
        return serviceProvider.QueryService(ref serviceWin10, ref interfaceIdWin10);
    }

    private static Guid GetVirtualDesktopInterfaceId()
    {
        var build = Environment.OSVersion.Version.Build;
        if (build >= 22621)
        {
            return IidVirtualDesktopWin11_22H2;
        }

        if (build >= 22000)
        {
            return IidVirtualDesktopWin11;
        }

        return IidVirtualDesktopWin10;
    }

    private static void GetDesktops(object managerObject, out IObjectArray desktops)
    {
        var build = Environment.OSVersion.Version.Build;

        if (build >= 22621)
        {
            ((IVirtualDesktopManagerInternal22621)managerObject).GetDesktops(out desktops);
            return;
        }

        if (build >= 22000)
        {
            ((IVirtualDesktopManagerInternal22000)managerObject).GetDesktops(IntPtr.Zero, out desktops);
            return;
        }

        ((IVirtualDesktopManagerInternalWin10)managerObject).GetDesktops(out desktops);
    }

    private static object GetCurrentDesktop(object managerObject)
    {
        var build = Environment.OSVersion.Version.Build;

        if (build >= 22621)
        {
            return ((IVirtualDesktopManagerInternal22621)managerObject).GetCurrentDesktop();
        }

        if (build >= 22000)
        {
            return ((IVirtualDesktopManagerInternal22000)managerObject).GetCurrentDesktop(IntPtr.Zero);
        }

        return ((IVirtualDesktopManagerInternalWin10)managerObject).GetCurrentDesktop();
    }

    private static void SwitchDesktop(object managerObject, object desktopObject)
    {
        var build = Environment.OSVersion.Version.Build;

        if (build >= 22621)
        {
            ((IVirtualDesktopManagerInternal22621)managerObject).SwitchDesktop((IVirtualDesktop22621)desktopObject);
            return;
        }

        if (build >= 22000)
        {
            ((IVirtualDesktopManagerInternal22000)managerObject).SwitchDesktop(IntPtr.Zero, (IVirtualDesktop22000)desktopObject);
            return;
        }

        ((IVirtualDesktopManagerInternalWin10)managerObject).SwitchDesktop((IVirtualDesktopWin10)desktopObject);
    }

    private static Guid GetDesktopId(object desktopObject, Guid desktopIid)
    {
        if (desktopIid == IidVirtualDesktopWin11_22H2)
        {
            return ((IVirtualDesktop22621)desktopObject).GetId();
        }

        if (desktopIid == IidVirtualDesktopWin11)
        {
            return ((IVirtualDesktop22000)desktopObject).GetId();
        }

        return ((IVirtualDesktopWin10)desktopObject).GetId();
    }

    private static void ReleaseComObject(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("FF72FFDD-BE7E-43FC-9C03-AD81681E88E4")]
    private interface IVirtualDesktopWin10
    {
        bool IsViewVisible(IntPtr view);

        Guid GetId();
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("536D3495-B208-4CC9-AE26-DE8111275BF8")]
    private interface IVirtualDesktop22000
    {
        bool IsViewVisible(IntPtr view);

        Guid GetId();

        IntPtr Unknown1();
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("3F07F4BE-B107-441A-AF0F-39D82529072C")]
    private interface IVirtualDesktop22621
    {
        bool IsViewVisible(IntPtr view);

        Guid GetId();
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("F31574D6-B682-4CDC-BD56-1827860ABEC6")]
    private interface IVirtualDesktopManagerInternalWin10
    {
        int GetCount();

        void MoveViewToDesktop(IntPtr view, IVirtualDesktopWin10 desktop);

        bool CanViewMoveDesktops(IntPtr view);

        IVirtualDesktopWin10 GetCurrentDesktop();

        void GetDesktops(out IObjectArray desktops);

        int GetAdjacentDesktop(IVirtualDesktopWin10 from, int direction, out IVirtualDesktopWin10 desktop);

        void SwitchDesktop(IVirtualDesktopWin10 desktop);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("B2F925B9-5A0F-4D2E-9F4D-2B1507593C10")]
    private interface IVirtualDesktopManagerInternal22000
    {
        int GetCount(IntPtr hWndOrMon);

        void MoveViewToDesktop(IntPtr view, IVirtualDesktop22000 desktop);

        bool CanViewMoveDesktops(IntPtr view);

        IVirtualDesktop22000 GetCurrentDesktop(IntPtr hWndOrMon);

        void GetDesktops(IntPtr hWndOrMon, out IObjectArray desktops);

        int GetAdjacentDesktop(IVirtualDesktop22000 from, int direction, out IVirtualDesktop22000 desktop);

        void SwitchDesktop(IntPtr hWndOrMon, IVirtualDesktop22000 desktop);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("53F5CA0B-158F-4124-900C-057158060B27")]
    private interface IVirtualDesktopManagerInternal22621
    {
        int GetCount();

        void MoveViewToDesktop(IntPtr view, IVirtualDesktop22621 desktop);

        bool CanViewMoveDesktops(IntPtr view);

        IVirtualDesktop22621 GetCurrentDesktop();

        void GetDesktops(out IObjectArray desktops);

        int GetAdjacentDesktop(IVirtualDesktop22621 from, int direction, out IVirtualDesktop22621 desktop);

        void SwitchDesktop(IVirtualDesktop22621 desktop);
    }
}

internal sealed record VirtualDesktopComEntry(int Index, Guid Id, bool IsCurrent);

internal sealed record VirtualDesktopInfo(int Index, Guid Id, string DisplayName, bool IsCurrent);
