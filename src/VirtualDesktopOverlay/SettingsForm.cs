namespace VirtualDesktopOverlay;

internal sealed class SettingsForm : Form
{
    private const int MinimumTransparencyPercent = 0;
    private const int MaximumTransparencyPercent = 70;

    private readonly RadioButton darkModeRadioButton = new();
    private readonly RadioButton lightModeRadioButton = new();
    private readonly TrackBar transparencyTrackBar = new();
    private readonly Label transparencyValueLabel = new();
    private readonly Button saveButton = new();
    private readonly Button cancelButton = new();
    private readonly List<HotkeyRowControls> hotkeyRows = [];
    private readonly Action<OverlaySettings> saveSettings;

    private int? capturingDesktopIndex;
    private Label? hotkeyStatusLabel;

    public SettingsForm(OverlaySettings settings, Action<OverlaySettings> saveSettings)
    {
        this.saveSettings = saveSettings;

        Text = "Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;
        ClientSize = new Size(420, 520);

        ConfigureControls(settings);
        KeyDown += OnKeyDownCapture;
        KeyUp += OnKeyUpCapture;
    }

    private void ConfigureControls(OverlaySettings settings)
    {
        var themeGroup = new GroupBox
        {
            Text = "Theme",
            Location = new Point(16, 16),
            Size = new Size(388, 78)
        };

        darkModeRadioButton.Text = "Dark mode";
        darkModeRadioButton.AutoSize = true;
        darkModeRadioButton.Location = new Point(18, 32);

        lightModeRadioButton.Text = "Light mode";
        lightModeRadioButton.AutoSize = true;
        lightModeRadioButton.Location = new Point(152, 32);

        themeGroup.Controls.Add(darkModeRadioButton);
        themeGroup.Controls.Add(lightModeRadioButton);

        var transparencyLabel = new Label
        {
            Text = "Transparency",
            AutoSize = true,
            Location = new Point(16, 108)
        };

        transparencyTrackBar.Minimum = MinimumTransparencyPercent;
        transparencyTrackBar.Maximum = MaximumTransparencyPercent;
        transparencyTrackBar.TickFrequency = 10;
        transparencyTrackBar.SmallChange = 5;
        transparencyTrackBar.LargeChange = 10;
        transparencyTrackBar.Location = new Point(16, 132);
        transparencyTrackBar.Size = new Size(300, 45);
        transparencyTrackBar.ValueChanged += (_, _) => UpdateTransparencyValueLabel();

        transparencyValueLabel.AutoSize = true;
        transparencyValueLabel.Location = new Point(326, 138);

        var hotkeyGroup = new GroupBox
        {
            Text = "Desktop shortcuts",
            Location = new Point(16, 188),
            Size = new Size(388, 280)
        };

        hotkeyStatusLabel = new Label
        {
            AutoSize = true,
            Location = new Point(18, 248),
            ForeColor = Color.DimGray
        };

        for (var index = 0; index < OverlaySettings.MaxDesktopHotkeySlots; index++)
        {
            var desktopIndex = index;
            var rowTop = 28 + desktopIndex * 24;
            var binding = settings.DesktopHotkeys.ElementAtOrDefault(desktopIndex)
                ?? new DesktopHotkeyBinding { DesktopIndex = desktopIndex };

            var desktopLabel = new Label
            {
                Text = $"Desktop {desktopIndex + 1}",
                AutoSize = true,
                Location = new Point(18, rowTop + 3)
            };

            var hotkeyTextBox = new TextBox
            {
                ReadOnly = true,
                Location = new Point(96, rowTop),
                Size = new Size(170, 23),
                Text = binding.DisplayText
            };

            var setButton = new Button
            {
                Text = "Set",
                Location = new Point(274, rowTop - 1),
                Size = new Size(44, 24),
                Tag = desktopIndex
            };
            setButton.Click += (_, _) => BeginHotkeyCapture(desktopIndex);

            var clearButton = new Button
            {
                Text = "Clear",
                Location = new Point(324, rowTop - 1),
                Size = new Size(52, 24),
                Tag = desktopIndex
            };
            clearButton.Click += (_, _) => ClearHotkey(desktopIndex);

            hotkeyGroup.Controls.Add(desktopLabel);
            hotkeyGroup.Controls.Add(hotkeyTextBox);
            hotkeyGroup.Controls.Add(setButton);
            hotkeyGroup.Controls.Add(clearButton);

            hotkeyRows.Add(new HotkeyRowControls(desktopIndex, hotkeyTextBox, binding));
        }

        hotkeyGroup.Controls.Add(hotkeyStatusLabel);

        saveButton.Text = "Save";
        saveButton.Location = new Point(244, 480);
        saveButton.Size = new Size(75, 26);
        saveButton.Click += (_, _) => SaveAndClose();

        cancelButton.Text = "Cancel";
        cancelButton.Location = new Point(329, 480);
        cancelButton.Size = new Size(75, 26);
        cancelButton.DialogResult = DialogResult.Cancel;

        AcceptButton = saveButton;
        CancelButton = cancelButton;

        Controls.Add(themeGroup);
        Controls.Add(transparencyLabel);
        Controls.Add(transparencyTrackBar);
        Controls.Add(transparencyValueLabel);
        Controls.Add(hotkeyGroup);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);

        ApplySettingsToControls(settings);
    }

    private void ApplySettingsToControls(OverlaySettings settings)
    {
        lightModeRadioButton.Checked = settings.Theme == OverlaySettings.LightTheme;
        darkModeRadioButton.Checked = !lightModeRadioButton.Checked;
        transparencyTrackBar.Value = Convert.ToInt32((1.0 - settings.Opacity) * 100);
        UpdateTransparencyValueLabel();

        foreach (var row in hotkeyRows)
        {
            var binding = settings.DesktopHotkeys.ElementAtOrDefault(row.DesktopIndex)
                ?? new DesktopHotkeyBinding { DesktopIndex = row.DesktopIndex };
            row.Binding = binding;
            row.HotkeyTextBox.Text = binding.DisplayText;
        }
    }

    private void UpdateTransparencyValueLabel()
    {
        transparencyValueLabel.Text = $"{transparencyTrackBar.Value}%";
    }

    private void BeginHotkeyCapture(int desktopIndex)
    {
        capturingDesktopIndex = desktopIndex;
        hotkeyStatusLabel!.Text = $"Press a key combination for Desktop {desktopIndex + 1}...";
        hotkeyStatusLabel.ForeColor = Color.DimGray;
    }

    private void ClearHotkey(int desktopIndex)
    {
        var row = hotkeyRows.First(rowControls => rowControls.DesktopIndex == desktopIndex);
        row.Binding = new DesktopHotkeyBinding { DesktopIndex = desktopIndex };
        row.HotkeyTextBox.Text = row.Binding.DisplayText;
        capturingDesktopIndex = null;
        hotkeyStatusLabel!.Text = string.Empty;
    }

    private void OnKeyDownCapture(object? sender, KeyEventArgs eventArgs)
    {
        if (capturingDesktopIndex is null)
        {
            return;
        }

        eventArgs.SuppressKeyPress = true;
        eventArgs.Handled = true;
    }

    private void OnKeyUpCapture(object? sender, KeyEventArgs eventArgs)
    {
        if (capturingDesktopIndex is not int desktopIndex)
        {
            return;
        }

        eventArgs.Handled = true;
        eventArgs.SuppressKeyPress = true;

        var key = eventArgs.KeyCode;
        if (key is Keys.ShiftKey or Keys.ControlKey or Keys.Menu or Keys.LWin or Keys.RWin)
        {
            return;
        }

        var modifiers = NativeHotkey.KeyModifiers.None;
        if (eventArgs.Control)
        {
            modifiers |= NativeHotkey.KeyModifiers.Control;
        }

        if (eventArgs.Alt)
        {
            modifiers |= NativeHotkey.KeyModifiers.Alt;
        }

        if (eventArgs.Shift)
        {
            modifiers |= NativeHotkey.KeyModifiers.Shift;
        }

        if ((Control.ModifierKeys & Keys.LWin) != 0 || (Control.ModifierKeys & Keys.RWin) != 0)
        {
            modifiers |= NativeHotkey.KeyModifiers.Win;
        }

        if (modifiers == NativeHotkey.KeyModifiers.None)
        {
            hotkeyStatusLabel!.Text = "Use at least one modifier (Ctrl, Alt, Shift, or Win).";
            hotkeyStatusLabel.ForeColor = Color.Firebrick;
            return;
        }

        var binding = new DesktopHotkeyBinding
        {
            DesktopIndex = desktopIndex,
            Modifiers = (int)modifiers,
            Key = (int)key
        };

        if (HasDuplicateHotkey(binding))
        {
            hotkeyStatusLabel!.Text = "That shortcut is already assigned to another desktop.";
            hotkeyStatusLabel.ForeColor = Color.Firebrick;
            return;
        }

        if (!TryTestHotkeyRegistration(binding))
        {
            hotkeyStatusLabel!.Text = "That shortcut is already in use by another application.";
            hotkeyStatusLabel.ForeColor = Color.Firebrick;
            return;
        }

        var row = hotkeyRows.First(rowControls => rowControls.DesktopIndex == desktopIndex);
        row.Binding = binding;
        row.HotkeyTextBox.Text = binding.DisplayText;
        capturingDesktopIndex = null;
        hotkeyStatusLabel!.Text = string.Empty;
    }

    private bool HasDuplicateHotkey(DesktopHotkeyBinding candidate)
    {
        return hotkeyRows.Any(row =>
            row.DesktopIndex != candidate.DesktopIndex
            && row.Binding.IsConfigured
            && row.Binding.Modifiers == candidate.Modifiers
            && row.Binding.Key == candidate.Key);
    }

    private bool TryTestHotkeyRegistration(DesktopHotkeyBinding binding)
    {
        const int testHotkeyId = 0x7FFF;
        var registered = NativeHotkey.RegisterHotKey(Handle, testHotkeyId, binding.GetModifiers(), (uint)binding.GetKey());
        if (!registered)
        {
            return false;
        }

        NativeHotkey.UnregisterHotKey(Handle, testHotkeyId);
        return true;
    }

    private void SaveAndClose()
    {
        saveSettings(new OverlaySettings
        {
            Theme = lightModeRadioButton.Checked ? OverlaySettings.LightTheme : OverlaySettings.DarkTheme,
            Opacity = (100 - transparencyTrackBar.Value) / 100.0,
            DesktopHotkeys = hotkeyRows.Select(row => row.Binding).ToList()
        });

        Close();
    }

    private sealed class HotkeyRowControls(int desktopIndex, TextBox hotkeyTextBox, DesktopHotkeyBinding binding)
    {
        public int DesktopIndex { get; } = desktopIndex;

        public TextBox HotkeyTextBox { get; } = hotkeyTextBox;

        public DesktopHotkeyBinding Binding { get; set; } = binding;
    }
}
