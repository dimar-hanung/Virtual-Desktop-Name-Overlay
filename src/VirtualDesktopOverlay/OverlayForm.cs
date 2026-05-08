namespace VirtualDesktopOverlay;

internal sealed class OverlayForm : Form
{
    private const string AppDisplayName = "Virtual Desktop Overlay";

    private readonly Label label = new();
    private readonly System.Windows.Forms.Timer refreshTimer = new();
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
    private readonly ToolStripMenuItem showOverlayMenuItem = new("Show overlay");
    private readonly ToolStripMenuItem exitMenuItem = new("Exit");

    private bool isDragging;
    private Point cursorStart;
    private Point formStart;

    public OverlayForm()
    {
        Text = AppDisplayName;
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
        Opacity = 0.75;
        Width = 260;
        Height = 42;

        ApplyStartupPosition();
        ConfigureLabel();
        ConfigureTrayIcon();
        ConfigureTimers();
        ConfigureMouseHandlers(this);
        ConfigureMouseHandlers(label);

        Controls.Add(label);

        Shown += OnShown;
        FormClosed += OnFormClosed;
    }

    private void ApplyStartupPosition()
    {
        var savedPosition = OverlaySettings.LoadPosition();
        if (savedPosition is { } position && IsPositionVisible(position))
        {
            Left = position.X;
            Top = position.Y;
            return;
        }

        var screen = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromPoint(Cursor.Position).WorkingArea;
        Left = screen.Right - Width - 20;
        Top = screen.Bottom - Height - 20;
    }

    private bool IsPositionVisible(Point position)
    {
        var bounds = new Rectangle(position, Size);
        return Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(bounds));
    }

    private void ConfigureLabel()
    {
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleCenter;
        label.ForeColor = Color.White;
        label.BackColor = Color.Transparent;
        label.Font = new Font("Segoe UI", 13, FontStyle.Bold);
        label.Text = GetOverlayText();
    }

    private void ConfigureTrayIcon()
    {
        trayMenu.Items.Add(showOverlayMenuItem);
        trayMenu.Items.Add(exitMenuItem);

        trayIcon.Text = AppDisplayName;
        trayIcon.Icon = SystemIcons.Application;
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;

        showOverlayMenuItem.Click += (_, _) => ShowOverlay();
        trayIcon.DoubleClick += (_, _) => ShowOverlay();
        exitMenuItem.Click += (_, _) => Close();
    }

    private void ConfigureTimers()
    {
        refreshTimer.Interval = 1000;
        refreshTimer.Tick += (_, _) => label.Text = GetOverlayText();
        refreshTimer.Start();
    }

    private void ConfigureMouseHandlers(Control control)
    {
        control.MouseDown += StartDrag;
        control.MouseMove += MoveDrag;
        control.MouseUp += StopDrag;
        control.MouseUp += HideOnRightClick;
    }

    private string GetOverlayText()
    {
        if (!VirtualDesktopService.IsSupportedWindowsVersion())
        {
            return "Unsupported Windows version";
        }

        try
        {
            return VirtualDesktopService.GetCurrentDesktopName();
        }
        catch (Exception ex)
        {
            OverlayLog.Write($"Failed to read current virtual desktop: {ex.Message}", "WARN");
            return "Unknown desktop";
        }
    }

    private void StartDrag(object? sender, MouseEventArgs eventArgs)
    {
        if (eventArgs.Button != MouseButtons.Left)
        {
            return;
        }

        isDragging = true;
        cursorStart = Cursor.Position;
        formStart = Location;

        if (sender is Control control)
        {
            control.Capture = true;
        }
    }

    private void MoveDrag(object? sender, MouseEventArgs eventArgs)
    {
        if (!isDragging)
        {
            return;
        }

        var currentCursor = Cursor.Position;
        Left = formStart.X + currentCursor.X - cursorStart.X;
        Top = formStart.Y + currentCursor.Y - cursorStart.Y;
    }

    private void StopDrag(object? sender, MouseEventArgs eventArgs)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        if (sender is Control control)
        {
            control.Capture = false;
        }

        OverlaySettings.SavePosition(this);
    }

    private void HideOnRightClick(object? sender, MouseEventArgs eventArgs)
    {
        if (eventArgs.Button == MouseButtons.Right)
        {
            Hide();
        }
    }

    private void ShowOverlay()
    {
        Show();
        Activate();
    }

    private void OnShown(object? sender, EventArgs eventArgs)
    {
        var pinTimer = new System.Windows.Forms.Timer { Interval = 500 };
        pinTimer.Tick += (_, _) =>
        {
            pinTimer.Stop();
            pinTimer.Dispose();

            try
            {
                VirtualDesktopService.PinWindow(Handle);
                label.Text = GetOverlayText();
            }
            catch (Exception ex)
            {
                OverlayLog.Write($"Failed to pin overlay window: {ex.Message}", "WARN");
                label.Text = "Could not pin overlay";
            }
        };
        pinTimer.Start();
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs eventArgs)
    {
        refreshTimer.Stop();
        refreshTimer.Dispose();
        trayIcon.Visible = false;
        trayIcon.Dispose();
        trayMenu.Dispose();
        label.Dispose();
    }
}
