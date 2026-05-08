using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace VirtualDesktopOverlay;

internal static class VirtualDesktopService
{
    private const string VirtualDesktopsRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops";

    public static bool IsSupportedWindowsVersion()
    {
        var version = Environment.OSVersion.Version;
        return version.Major > 10 || version.Major == 10 && version.Build >= 19041;
    }

    public static string GetCurrentDesktopName()
    {
        var currentDesktopId = GetCurrentDesktopId();
        var desktopIds = GetDesktopIds();

        var index = currentDesktopId is null
            ? -1
            : desktopIds.FindIndex(id => id == currentDesktopId.Value);

        if (currentDesktopId is { } guid)
        {
            var name = GetDesktopName(guid);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return index >= 0 ? $"Desktop {index + 1}" : "Desktop";
    }

    public static void PinWindow(IntPtr hWnd)
    {
        VirtualDesktopPinning.PinWindow(hWnd);
    }

    private static Guid? GetCurrentDesktopId()
    {
        foreach (var path in GetCurrentDesktopRegistryPaths(sessionFirst: true))
        {
            var guid = ReadGuidValue(path, "CurrentVirtualDesktop");
            if (guid is not null)
            {
                return guid;
            }
        }

        return null;
    }

    private static List<Guid> GetDesktopIds()
    {
        foreach (var path in GetCurrentDesktopRegistryPaths(sessionFirst: false))
        {
            var ids = ReadGuidListValue(path, "VirtualDesktopIDs");
            if (ids.Count > 0)
            {
                return ids;
            }
        }

        return [];
    }

    private static IEnumerable<string> GetCurrentDesktopRegistryPaths(bool sessionFirst)
    {
        var sessionPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SessionInfo\{Process.GetCurrentProcess().SessionId}\VirtualDesktops";

        if (sessionFirst)
        {
            yield return sessionPath;
            yield return VirtualDesktopsRegistryPath;
            yield break;
        }

        yield return VirtualDesktopsRegistryPath;
        yield return sessionPath;
    }

    private static string? GetDesktopName(Guid desktopId)
    {
        using var key = Registry.CurrentUser.OpenSubKey($@"{VirtualDesktopsRegistryPath}\Desktops\{{{desktopId}}}");
        return key?.GetValue("Name") as string;
    }

    private static Guid? ReadGuidValue(string path, string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(path);
        var value = key?.GetValue(valueName);

        return value switch
        {
            byte[] bytes when bytes.Length >= 16 => new Guid(bytes.Take(16).ToArray()),
            string text when Guid.TryParse(text, out var guid) => guid,
            _ => null
        };
    }

    private static List<Guid> ReadGuidListValue(string path, string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(path);
        if (key?.GetValue(valueName) is not byte[] bytes || bytes.Length < 16)
        {
            return [];
        }

        var ids = new List<Guid>();
        for (var offset = 0; offset + 16 <= bytes.Length; offset += 16)
        {
            ids.Add(new Guid(bytes.Skip(offset).Take(16).ToArray()));
        }

        return ids;
    }
}

internal static class VirtualDesktopPinning
{
    private static readonly Guid ClsidImmersiveShell = new("C2F03A33-21F5-47FA-B4BB-156362A2F239");
    private static readonly Guid ClsidVirtualDesktopPinnedApps = new("B5A399E7-1C87-46B8-88E9-FC5747B171BD");

    public static void PinWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            throw new ArgumentException("Window handle cannot be zero.", nameof(hWnd));
        }

        object? shell = null;
        object? viewObject = null;
        object? viewCollectionObject = null;
        object? pinnedAppsObject = null;

        try
        {
            shell = Activator.CreateInstance(Type.GetTypeFromCLSID(ClsidImmersiveShell, true)!);
            var serviceProvider = (IServiceProvider10)shell!;

            var collectionService = typeof(IApplicationViewCollection).GUID;
            var collectionInterface = typeof(IApplicationViewCollection).GUID;
            var viewCollection = (IApplicationViewCollection)serviceProvider.QueryService(ref collectionService, ref collectionInterface);
            viewCollectionObject = viewCollection;

            var pinnedAppsService = ClsidVirtualDesktopPinnedApps;
            var pinnedAppsInterface = typeof(IVirtualDesktopPinnedApps).GUID;
            var pinnedApps = (IVirtualDesktopPinnedApps)serviceProvider.QueryService(ref pinnedAppsService, ref pinnedAppsInterface);
            pinnedAppsObject = pinnedApps;

            var result = viewCollection.GetViewForHwnd(hWnd, out var view);
            Marshal.ThrowExceptionForHR(result);
            viewObject = view;

            if (!pinnedApps.IsViewPinned(view))
            {
                pinnedApps.PinView(view);
            }
        }
        finally
        {
            if (viewObject is not null && Marshal.IsComObject(viewObject))
            {
                Marshal.ReleaseComObject(viewObject);
            }

            if (pinnedAppsObject is not null && Marshal.IsComObject(pinnedAppsObject))
            {
                Marshal.ReleaseComObject(pinnedAppsObject);
            }

            if (viewCollectionObject is not null && Marshal.IsComObject(viewCollectionObject))
            {
                Marshal.ReleaseComObject(viewCollectionObject);
            }

            if (shell is not null && Marshal.IsComObject(shell))
            {
                Marshal.ReleaseComObject(shell);
            }
        }
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
    private interface IServiceProvider10
    {
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object QueryService(ref Guid service, ref Guid riid);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("372E1D3B-38D3-42E4-A15B-8AB2B178F513")]
    private interface IApplicationView
    {
        int SetFocus();
        int SwitchTo();
        int TryInvokeBack(IntPtr callback);
        int GetThumbnailWindow(out IntPtr hwnd);
        int GetMonitor(out IntPtr immersiveMonitor);
        int GetVisibility(out int visibility);
        int SetCloak(int cloakType, int unknown);
        int GetPosition(ref Guid guid, out IntPtr position);
        int SetPosition(ref IntPtr position);
        int InsertAfterWindow(IntPtr hwnd);
        int GetExtendedFramePosition(out NativeRect rect);
        int GetAppUserModelId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        int SetAppUserModelId(string id);
        int IsEqualByAppUserModelId(string id, out int result);
        int GetViewState(out uint state);
        int SetViewState(uint state);
        int GetNeediness(out int neediness);
        int GetLastActivationTimestamp(out ulong timestamp);
        int SetLastActivationTimestamp(ulong timestamp);
        int GetVirtualDesktopId(out Guid guid);
        int SetVirtualDesktopId(ref Guid guid);
        int GetShowInSwitchers(out int flag);
        int SetShowInSwitchers(int flag);
        int GetScaleFactor(out int factor);
        int CanReceiveInput(out bool canReceiveInput);
        int GetCompatibilityPolicyType(out int flags);
        int SetCompatibilityPolicyType(int flags);
        int GetSizeConstraints(IntPtr monitor, out NativeSize size1, out NativeSize size2);
        int GetSizeConstraintsForDpi(uint dpi, out NativeSize size1, out NativeSize size2);
        int SetSizeConstraintsForDpi(ref uint dpi, ref NativeSize size1, ref NativeSize size2);
        int OnMinSizePreferencesUpdated(IntPtr hwnd);
        int ApplyOperation(IntPtr operation);
        int IsTray(out bool isTray);
        int IsInHighZOrderBand(out bool isInHighZOrderBand);
        int IsSplashScreenPresented(out bool isSplashScreenPresented);
        int Flash();
        int GetRootSwitchableOwner(out IApplicationView rootSwitchableOwner);
        int EnumerateOwnershipTree(out IObjectArray ownershipTree);
        int GetEnterpriseId([MarshalAs(UnmanagedType.LPWStr)] out string enterpriseId);
        int IsMirrored(out bool isMirrored);
        int Unknown1(out int unknown);
        int Unknown2(out int unknown);
        int Unknown3(out int unknown);
        int Unknown4(out int unknown);
        int Unknown5(out int unknown);
        int Unknown6(int unknown);
        int Unknown7();
        int Unknown8(out int unknown);
        int Unknown9(int unknown);
        int Unknown10(int unknownX, int unknownY);
        int Unknown11(int unknown);
        int Unknown12(out NativeSize size1);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSize
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5")]
    private interface IApplicationViewCollection
    {
        int GetViews(out IObjectArray array);
        int GetViewsByZOrder(out IObjectArray array);
        int GetViewsByAppUserModelId(string id, out IObjectArray array);
        int GetViewForHwnd(IntPtr hwnd, out IApplicationView view);
        int GetViewForApplication(object application, out IApplicationView view);
        int GetViewForAppUserModelId(string id, out IApplicationView view);
        int GetViewInFocus(out IntPtr view);
        int Unknown1(out IntPtr view);
        void RefreshCollection();
        int RegisterForApplicationViewChanges(object listener, out int cookie);
        int UnregisterForApplicationViewChanges(int cookie);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("4CE81583-1E4C-4632-A621-07A53543148F")]
    private interface IVirtualDesktopPinnedApps
    {
        bool IsAppIdPinned(string appId);
        void PinAppID(string appId);
        void UnpinAppID(string appId);
        bool IsViewPinned(IApplicationView applicationView);
        void PinView(IApplicationView applicationView);
        void UnpinView(IApplicationView applicationView);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
    private interface IObjectArray
    {
        void GetCount(out int count);
        void GetAt(int index, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object obj);
    }
}
