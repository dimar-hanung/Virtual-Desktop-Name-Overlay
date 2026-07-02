namespace VirtualDesktopOverlay;

internal sealed class OverlayForm : Form
{
    private const string AppDisplayName = "Virtual Desktop Overlay";
    private const int OverlayWidth = 260;
    private const int MinAutoWidth = 80;
    private const int VerticalPadding = 4;
    private const int ListHorizontalPadding = 8;
    private const int FlexHorizontalPadding = 4;
    private const int ListRowHorizontalPadding = 6;
    private const int FlexChipHorizontalPadding = 4;
    private const int FlexChipWidthBuffer = 14;
    private const int DragStripHeight = 8;
    private const int ClickDragThreshold = 4;
    private const int HotkeyIdBase = 0x4000;
    private const int ChipSpacing = 2;

    // Tuned for readability at default overlay opacity (text/background contrast ~4.5:1+).
    private static readonly Color LightActiveBackground = Color.FromArgb(208, 232, 249);
    private static readonly Color LightActiveForeground = Color.FromArgb(11, 74, 122);
    private static readonly Color LightRowBackground = Color.White;
    private static readonly Color LightRowForeground = Color.FromArgb(45, 45, 45);
    private static readonly Color DarkActiveBackground = Color.FromArgb(42, 95, 143);
    private static readonly Color DarkActiveForeground = Color.FromArgb(242, 246, 250);
    private static readonly Color DarkRowBackground = Color.FromArgb(30, 30, 30);
    private static readonly Color DarkRowForeground = Color.FromArgb(212, 212, 212);

    private readonly Panel dragStrip = new();
    private readonly Panel desktopListPanel = new();
    private readonly System.Windows.Forms.Timer refreshTimer = new();
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
    private readonly ToolStripMenuItem showOverlayMenuItem = new("Show overlay");
    private readonly ToolStripMenuItem hideOverlayMenuItem = new("Hide overlay");
    private readonly ToolStripMenuItem settingsMenuItem = new("Settings...");
    private readonly ToolStripMenuItem exitMenuItem = new("Exit");
    private readonly OverlaySettings settings = OverlaySettings.Load();
    private readonly Dictionary<int, int> registeredHotkeyIds = new();

    private Font rowFont;
    private Font activeRowFont;
    private int rowHeight;
    private int measuredContentWidth = OverlayWidth;

    private SettingsForm? settingsForm;
    private System.Windows.Forms.Timer? pinTimer;
    private bool isDragging;
    private bool suppressRowClick;
    private Point cursorStart;
    private Point formStart;
    private string? lastDesktopListSignature;
    private string? statusMessage;

    public OverlayForm()
    {
        rowFont = CreateRowFont(settings.FontSize, FontStyle.Regular);
        activeRowFont = CreateRowFont(settings.FontSize, FontStyle.Bold);
        rowHeight = GetRowHeight(settings.FontSize);

        Text = AppDisplayName;
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Width = OverlayWidth;

        ApplyStartupPosition();
        ConfigureDesktopList();
        ApplyAppearanceSettings(settings);
        ConfigureTrayIcon();
        ConfigureTimers();
        ConfigureDragHandlers(dragStrip);
        MouseUp += OnFormMouseUp;
        MouseMove += OnFormMouseMove;

        Controls.Add(desktopListPanel);
        Controls.Add(dragStrip);

        Shown += OnShown;
        FormClosed += OnFormClosed;
        ResizeDesktopList();
    }

    protected override void OnHandleCreated(EventArgs eventArgs)
    {
        base.OnHandleCreated(eventArgs);

        if (Visible)
        {
            SchedulePinOverlayWindow();
            RegisterDesktopHotkeys();
        }
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeHotkey.WmHotkey)
        {
            var hotkeyId = message.WParam.ToInt32();
            if (registeredHotkeyIds.TryGetValue(hotkeyId, out var desktopIndex))
            {
                SwitchToDesktop(desktopIndex);
            }

            return;
        }

        base.WndProc(ref message);
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

    private void ConfigureDesktopList()
    {
        dragStrip.Dock = DockStyle.Top;
        dragStrip.Height = DragStripHeight;
        dragStrip.Cursor = Cursors.SizeAll;

        desktopListPanel.Dock = DockStyle.Fill;
        UpdatePanelPadding();
    }

    private void UpdatePanelPadding()
    {
        var horizontalPadding = settings.DesignType == OverlaySettings.FlexDesign
            ? FlexHorizontalPadding
            : ListHorizontalPadding;
        desktopListPanel.Padding = new Padding(horizontalPadding, VerticalPadding, horizontalPadding, VerticalPadding);
    }

    private void ConfigureTrayIcon()
    {
        trayMenu.Items.Add(showOverlayMenuItem);
        trayMenu.Items.Add(hideOverlayMenuItem);
        trayMenu.Items.Add(settingsMenuItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(exitMenuItem);

        trayIcon.Text = AppDisplayName;
        trayIcon.Icon = SystemIcons.Application;
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;

        showOverlayMenuItem.Click += (_, _) => ShowOverlay();
        hideOverlayMenuItem.Click += (_, _) => HideOverlay();
        settingsMenuItem.Click += (_, _) => ShowSettings();
        trayIcon.DoubleClick += (_, _) => ShowOverlay();
        exitMenuItem.Click += (_, _) => Close();
        UpdateTrayMenuState();
    }

    private void ApplyAppearanceSettings(OverlaySettings appearanceSettings)
    {
        var isLightMode = appearanceSettings.Theme == OverlaySettings.LightTheme;

        BackColor = isLightMode ? Color.WhiteSmoke : Color.FromArgb(18, 18, 18);
        dragStrip.BackColor = isLightMode ? Color.Gainsboro : Color.FromArgb(38, 38, 38);
        desktopListPanel.BackColor = isLightMode ? Color.WhiteSmoke : Color.FromArgb(18, 18, 18);
        Opacity = appearanceSettings.Opacity;
        UpdatePanelPadding();
        UpdateFonts(appearanceSettings.FontSize);
        lastDesktopListSignature = null;
        RefreshDesktopList();
    }

    private void UpdateFonts(int fontSize)
    {
        rowFont.Dispose();
        activeRowFont.Dispose();
        rowFont = CreateRowFont(fontSize, FontStyle.Regular);
        activeRowFont = CreateRowFont(fontSize, FontStyle.Bold);
        rowHeight = GetRowHeight(fontSize);
    }

    private static Font CreateRowFont(int fontSize, FontStyle style) =>
        new("Segoe UI", fontSize, style);

    private static int GetRowHeight(int fontSize) =>
        Math.Max(28, fontSize + 20);

    private void ConfigureTimers()
    {
        refreshTimer.Interval = 250;
        refreshTimer.Tick += (_, _) => RefreshDesktopList();
        refreshTimer.Start();
    }

    private void RefreshDesktopList()
    {
        if (!VirtualDesktopService.IsSupportedWindowsVersion())
        {
            RenderMessageRow("Unsupported Windows version");
            return;
        }

        try
        {
            var desktops = VirtualDesktopService.GetDesktopList();
            if (desktops.Count == 0)
            {
                RenderMessageRow(statusMessage ?? "No desktops found");
                return;
            }

            statusMessage = null;
            var desktopSignature = string.Join("|", desktops.Select(desktop => $"{desktop.Index}:{desktop.DisplayName}:{desktop.IsCurrent}"));
            var signature = $"{settings.DesignType}:{settings.FontSize}|{desktopSignature}";
            if (signature == lastDesktopListSignature)
            {
                return;
            }

            lastDesktopListSignature = signature;
            RenderDesktopRows(desktops);
        }
        catch (Exception ex)
        {
            OverlayLog.Write($"Failed to refresh desktop list: {ex.Message}", "WARN");
            RenderMessageRow("Unknown desktop");
        }
    }

    private void RenderMessageRow(string message)
    {
        var signature = $"message:{settings.DesignType}:{settings.FontSize}:{message}";
        if (signature == lastDesktopListSignature)
        {
            return;
        }

        lastDesktopListSignature = signature;
        desktopListPanel.Controls.Clear();

        var isLightMode = settings.Theme == OverlaySettings.LightTheme;
        var label = CreateRowLabel(message, isCurrent: false, rowFont, activeRowFont);
        label.Dock = DockStyle.Top;
        label.Height = rowHeight;
        label.ForeColor = isLightMode ? LightRowForeground : DarkRowForeground;
        desktopListPanel.Controls.Add(label);

        measuredContentWidth = UsesAutoWidth()
            ? Math.Max(MinAutoWidth, MeasureTextWidth(message, rowFont) + FlexChipHorizontalPadding * 2 + FlexChipWidthBuffer)
            : OverlayWidth;
        ResizeDesktopList();
    }

    private void RenderDesktopRows(IReadOnlyList<VirtualDesktopInfo> desktops)
    {
        switch (settings.DesignType)
        {
            case OverlaySettings.FlexDesign:
                RenderFlexRow(desktops);
                break;
            case OverlaySettings.JustShowActiveDesign:
                RenderActiveOnly(desktops);
                break;
            default:
                RenderVerticalList(desktops);
                break;
        }
    }

    private void RenderVerticalList(IReadOnlyList<VirtualDesktopInfo> desktops)
    {
        desktopListPanel.Controls.Clear();
        measuredContentWidth = OverlayWidth;

        for (var index = desktops.Count - 1; index >= 0; index--)
        {
            var desktop = desktops[index];
            var row = CreateDesktopRow(desktop, enableClickToSwitch: true, ListRowHorizontalPadding);
            row.Dock = DockStyle.Top;
            row.Height = rowHeight;
            desktopListPanel.Controls.Add(row);
        }

        ResizeDesktopList();
    }

    private void RenderFlexRow(IReadOnlyList<VirtualDesktopInfo> desktops)
    {
        desktopListPanel.Controls.Clear();
        UpdatePanelPadding();

        var flowPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var totalWidth = 0;
        for (var index = 0; index < desktops.Count; index++)
        {
            var desktop = desktops[index];
            var chip = CreateDesktopRow(desktop, enableClickToSwitch: true, FlexChipHorizontalPadding);
            chip.Margin = new Padding(0, 0, index < desktops.Count - 1 ? ChipSpacing : 0, 0);
            chip.Height = rowHeight;

            var font = desktop.IsCurrent ? activeRowFont : rowFont;
            var textWidth = MeasureTextWidth(desktop.DisplayName, font);
            chip.Width = textWidth + FlexChipWidthBuffer + FlexChipHorizontalPadding * 2;

            flowPanel.Controls.Add(chip);
            totalWidth += chip.Width + chip.Margin.Right;
        }

        desktopListPanel.Controls.Add(flowPanel);
        measuredContentWidth = Math.Max(MinAutoWidth, totalWidth + desktopListPanel.Padding.Horizontal);
        ResizeDesktopList();
    }

    private void RenderActiveOnly(IReadOnlyList<VirtualDesktopInfo> desktops)
    {
        desktopListPanel.Controls.Clear();

        var activeDesktop = desktops.FirstOrDefault(desktop => desktop.IsCurrent);
        if (activeDesktop is null)
        {
            RenderMessageRow("No active desktop");
            return;
        }

        var row = CreateDesktopRow(activeDesktop, enableClickToSwitch: false, FlexChipHorizontalPadding);
        row.Dock = DockStyle.Top;
        row.Height = rowHeight;
        desktopListPanel.Controls.Add(row);

        measuredContentWidth = Math.Max(
            MinAutoWidth,
            MeasureTextWidth(activeDesktop.DisplayName, activeRowFont) + FlexChipWidthBuffer + FlexChipHorizontalPadding * 2 + desktopListPanel.Padding.Horizontal);
        ResizeDesktopList();
    }

    private Panel CreateDesktopRow(VirtualDesktopInfo desktop, bool enableClickToSwitch, int horizontalPadding)
    {
        var isLightMode = settings.Theme == OverlaySettings.LightTheme;
        var row = new Panel
        {
            Tag = desktop.Index,
            Cursor = enableClickToSwitch ? Cursors.Hand : Cursors.Default,
            Padding = new Padding(horizontalPadding, 0, horizontalPadding, 0)
        };

        ApplyRowAppearance(row, desktop.IsCurrent, isLightMode);

        var label = CreateRowLabel(desktop.DisplayName, desktop.IsCurrent, rowFont, activeRowFont);
        label.Dock = DockStyle.Fill;
        row.Controls.Add(label);

        if (enableClickToSwitch)
        {
            row.MouseDown += OnRowMouseDown;
            row.MouseMove += OnRowMouseMove;
            row.MouseUp += OnRowMouseUp;
            label.MouseDown += OnRowMouseDown;
            label.MouseMove += OnRowMouseMove;
            label.MouseUp += OnRowMouseUp;
        }

        return row;
    }

    private static Label CreateRowLabel(string text, bool isCurrent, Font normalFont, Font activeFont)
    {
        return new Label
        {
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            BackColor = Color.Transparent,
            Font = isCurrent ? activeFont : normalFont
        };
    }

    private void ApplyRowAppearance(Panel row, bool isCurrent, bool isLightMode)
    {
        if (isCurrent)
        {
            row.BackColor = isLightMode ? LightActiveBackground : DarkActiveBackground;
            foreach (Control child in row.Controls)
            {
                child.ForeColor = isLightMode ? LightActiveForeground : DarkActiveForeground;
            }

            return;
        }

        row.BackColor = isLightMode ? LightRowBackground : DarkRowBackground;
        foreach (Control child in row.Controls)
        {
            child.ForeColor = isLightMode ? LightRowForeground : DarkRowForeground;
        }
    }

    private int MeasureTextWidth(string text, Font font) =>
        TextRenderer.MeasureText(
            text,
            font,
            new Size(int.MaxValue, rowHeight),
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;

    private bool UsesAutoWidth() =>
        settings.DesignType is OverlaySettings.FlexDesign or OverlaySettings.JustShowActiveDesign;

    private void ResizeDesktopList()
    {
        if (UsesAutoWidth())
        {
            Width = measuredContentWidth;
            Height = DragStripHeight + VerticalPadding * 2 + rowHeight;
            return;
        }

        Width = OverlayWidth;
        var rowCount = Math.Max(1, desktopListPanel.Controls.Count);
        Height = DragStripHeight + VerticalPadding * 2 + rowHeight * rowCount;
    }

    private static bool IsLeftMouseButtonHeld() =>
        (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left;

    private void UpdateDragPosition()
    {
        var currentCursor = Cursor.Position;
        Left = formStart.X + currentCursor.X - cursorStart.X;
        Top = formStart.Y + currentCursor.Y - cursorStart.Y;
    }

    private void OnFormMouseUp(object? sender, MouseEventArgs eventArgs)
    {
        if (eventArgs.Button != MouseButtons.Left || !isDragging)
        {
            return;
        }

        FinishDrag();
    }

    private void OnFormMouseMove(object? sender, MouseEventArgs eventArgs)
    {
        if (!isDragging || !IsLeftMouseButtonHeld() || !Capture)
        {
            return;
        }

        UpdateDragPosition();
    }

    private void OnRowMouseDown(object? sender, MouseEventArgs eventArgs)
    {
        if (eventArgs.Button != MouseButtons.Left)
        {
            return;
        }

        if (isDragging && !IsLeftMouseButtonHeld())
        {
            FinishDrag();
        }

        suppressRowClick = false;
        cursorStart = Cursor.Position;
        formStart = Location;
    }

    private void OnRowMouseMove(object? sender, MouseEventArgs eventArgs)
    {
        if (!IsLeftMouseButtonHeld())
        {
            return;
        }

        var distance = Math.Abs(Cursor.Position.X - cursorStart.X) + Math.Abs(Cursor.Position.Y - cursorStart.Y);
        if (distance < ClickDragThreshold)
        {
            return;
        }

        suppressRowClick = true;
        if (!isDragging)
        {
            isDragging = true;
            Capture = true;
        }

        UpdateDragPosition();
    }

    private void OnRowMouseUp(object? sender, MouseEventArgs eventArgs)
    {
        if (eventArgs.Button != MouseButtons.Left)
        {
            return;
        }

        if (isDragging)
        {
            FinishDrag();
            return;
        }

        if (suppressRowClick)
        {
            return;
        }

        if (sender is not Control control)
        {
            return;
        }

        var row = control is Panel panel ? panel : control.Parent;
        if (row?.Tag is int desktopIndex)
        {
            SwitchToDesktop(desktopIndex);
        }
    }

    private void SwitchToDesktop(int desktopIndex)
    {
        if (!VirtualDesktopService.TrySwitchToDesktop(desktopIndex, out var error))
        {
            statusMessage = error ?? "Switch failed";
            lastDesktopListSignature = null;
            OverlayLog.Write($"Desktop switch failed for index {desktopIndex}: {statusMessage}", "WARN");
            RefreshDesktopList();
            return;
        }

        statusMessage = null;
        lastDesktopListSignature = null;
        RefreshDesktopList();
        SchedulePinOverlayWindow();
    }

    private void ConfigureDragHandlers(Control control)
    {
        control.MouseDown += StartDrag;
        control.MouseMove += MoveDrag;
        control.MouseUp += StopDrag;
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
        if (!isDragging || !IsLeftMouseButtonHeld())
        {
            return;
        }

        UpdateDragPosition();
    }

    private void StopDrag(object? sender, MouseEventArgs eventArgs)
    {
        if (!isDragging)
        {
            return;
        }

        FinishDrag();

        if (sender is Control control)
        {
            control.Capture = false;
        }
    }

    private void FinishDrag()
    {
        isDragging = false;
        Capture = false;
        OverlaySettings.SavePosition(this);
    }

    private void ShowOverlay()
    {
        Show();
        Activate();
        SchedulePinOverlayWindow();
        RegisterDesktopHotkeys();
        UpdateTrayMenuState();
    }

    private void HideOverlay()
    {
        Hide();
        UpdateTrayMenuState();
    }

    private void UpdateTrayMenuState()
    {
        showOverlayMenuItem.Enabled = !Visible;
        hideOverlayMenuItem.Enabled = Visible;
    }

    private void ShowSettings()
    {
        if (settingsForm is not null)
        {
            settingsForm.Activate();
            return;
        }

        settingsForm = new SettingsForm(settings, SaveAppearanceSettings);
        settingsForm.FormClosed += (_, _) => settingsForm = null;
        settingsForm.Show();
        settingsForm.Activate();
    }

    private void SaveAppearanceSettings(OverlaySettings appearanceSettings)
    {
        settings.Theme = appearanceSettings.Theme;
        settings.DesignType = appearanceSettings.DesignType;
        settings.FontSize = appearanceSettings.FontSize;
        settings.Opacity = appearanceSettings.Opacity;
        settings.DesktopHotkeys = appearanceSettings.DesktopHotkeys;
        settings.Left = Left;
        settings.Top = Top;

        ApplyAppearanceSettings(settings);
        OverlaySettings.Save(settings);
        RegisterDesktopHotkeys();
        SchedulePinOverlayWindow();
    }

    private void RegisterDesktopHotkeys()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        UnregisterDesktopHotkeys();

        foreach (var binding in settings.DesktopHotkeys)
        {
            if (!binding.IsConfigured)
            {
                continue;
            }

            var hotkeyId = HotkeyIdBase + binding.DesktopIndex;
            var registered = NativeHotkey.RegisterHotKey(
                Handle,
                hotkeyId,
                binding.GetModifiers(),
                (uint)binding.GetKey());

            if (!registered)
            {
                OverlayLog.Write(
                    $"Failed to register hotkey for desktop {binding.DesktopIndex + 1} ({binding.DisplayText})",
                    "WARN");
                continue;
            }

            registeredHotkeyIds[hotkeyId] = binding.DesktopIndex;
        }
    }

    private void UnregisterDesktopHotkeys()
    {
        if (!IsHandleCreated)
        {
            registeredHotkeyIds.Clear();
            return;
        }

        foreach (var hotkeyId in registeredHotkeyIds.Keys)
        {
            NativeHotkey.UnregisterHotKey(Handle, hotkeyId);
        }

        registeredHotkeyIds.Clear();
    }

    private void OnShown(object? sender, EventArgs eventArgs)
    {
        SchedulePinOverlayWindow();
        RegisterDesktopHotkeys();
        RefreshDesktopList();
        UpdateTrayMenuState();
    }

    private void SchedulePinOverlayWindow()
    {
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }

        pinTimer?.Stop();
        pinTimer?.Dispose();

        pinTimer = new System.Windows.Forms.Timer { Interval = 500 };
        pinTimer.Tick += (_, _) =>
        {
            pinTimer?.Stop();
            pinTimer?.Dispose();
            pinTimer = null;
            PinOverlayWindow();
        };
        pinTimer.Start();
    }

    private void PinOverlayWindow()
    {
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }

        try
        {
            VirtualDesktopService.PinWindow(Handle);
            RefreshDesktopList();
        }
        catch (Exception ex)
        {
            OverlayLog.Write($"Failed to pin overlay window: {ex.Message}", "WARN");
            RenderMessageRow("Could not pin overlay");
        }
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs eventArgs)
    {
        settingsForm?.Close();
        UnregisterDesktopHotkeys();
        pinTimer?.Stop();
        pinTimer?.Dispose();
        refreshTimer.Stop();
        refreshTimer.Dispose();
        trayIcon.Visible = false;
        trayIcon.Dispose();
        trayMenu.Dispose();
        desktopListPanel.Dispose();
        dragStrip.Dispose();
        rowFont.Dispose();
        activeRowFont.Dispose();
    }
}
