namespace VirtualDesktopOverlay;

internal static class Program
{
    private static Mutex? singleInstanceMutex;
    private static bool hasSingleInstanceMutex;

    [STAThread]
    private static void Main()
    {
        if (!StartSingleInstance())
        {
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new OverlayForm());
        }
        finally
        {
            StopSingleInstance();
        }
    }

    private static bool StartSingleInstance()
    {
        try
        {
            singleInstanceMutex = new Mutex(true, "Local\\VirtualDesktopOverlay.SingleInstance", out hasSingleInstanceMutex);
            if (!hasSingleInstanceMutex)
            {
                OverlayLog.Write("Another overlay instance is already running. Exiting.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            OverlayLog.Write($"Failed to create single-instance mutex: {ex.Message}", "WARN");
            return true;
        }
    }

    private static void StopSingleInstance()
    {
        if (singleInstanceMutex is null)
        {
            return;
        }

        try
        {
            if (hasSingleInstanceMutex)
            {
                singleInstanceMutex.ReleaseMutex();
            }
        }
        catch (Exception ex)
        {
            OverlayLog.Write($"Failed to release single-instance mutex: {ex.Message}", "WARN");
        }
        finally
        {
            singleInstanceMutex.Dispose();
            singleInstanceMutex = null;
            hasSingleInstanceMutex = false;
        }
    }
}
