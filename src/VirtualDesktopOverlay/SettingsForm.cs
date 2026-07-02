namespace VirtualDesktopOverlay;

internal sealed class SettingsForm : Form
{
    private const int MinimumTransparencyPercent = 0;
    private const int MaximumTransparencyPercent = 70;
    private const int LayoutMargin = 16;
    private const int ValueLabelWidth = 56;

    private readonly RadioButton darkModeRadioButton = new();
    private readonly RadioButton lightModeRadioButton = new();
    private readonly RadioButton likeCurrentDesignRadioButton = new();
    private readonly RadioButton flexDesignRadioButton = new();
    private readonly RadioButton justShowActiveDesignRadioButton = new();
    private readonly TrackBar transparencyTrackBar = new();
    private readonly Label transparencyValueLabel = new();
    private readonly TrackBar fontSizeTrackBar = new();
    private readonly Label fontSizeValueLabel = new();
    private readonly Button saveButton = new();
    private readonly Button cancelButton = new();
    private readonly List<HotkeyRowControls> hotkeyRows = [];
    private readonly Action<OverlaySettings> saveSettings;

    private GroupBox? themeGroup;
    private GroupBox? designTypeGroup;
    private GroupBox? hotkeyGroup;
    private Label? transparencyLabel;
    private Label? fontSizeLabel;
    private int? capturingDesktopIndex;
    private Label? hotkeyStatusLabel;

    public SettingsForm(OverlaySettings settings, Action<OverlaySettings> saveSettings)
    {
        this.saveSettings = saveSettings;

        Text = "Settings";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = true;
        MinimumSize = new Size(420, 680);
        KeyPreview = true;
        ClientSize = new Size(420, 680);

        ConfigureControls(settings);
        LayoutControls();
        Resize += (_, _) => LayoutControls();
        KeyDown += OnKeyDownCapture;
        KeyUp += OnKeyUpCapture;
    }

    private void ConfigureControls(OverlaySettings settings)
    {
        themeGroup = new GroupBox
        {
            Text = "Theme",
            Location = new Point(LayoutMargin, LayoutMargin),
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

        designTypeGroup = new GroupBox
        {
            Text = "Design type",
            Location = new Point(LayoutMargin, 104),
            Size = new Size(388, 100)
        };

        likeCurrentDesignRadioButton.Text = "Like current";
        likeCurrentDesignRadioButton.AutoSize = true;
        likeCurrentDesignRadioButton.Location = new Point(18, 28);

        flexDesignRadioButton.Text = "Flex";
        flexDesignRadioButton.AutoSize = true;
        flexDesignRadioButton.Location = new Point(18, 52);

        justShowActiveDesignRadioButton.Text = "Just show Active";
        justShowActiveDesignRadioButton.AutoSize = true;
        justShowActiveDesignRadioButton.Location = new Point(18, 76);

        designTypeGroup.Controls.Add(likeCurrentDesignRadioButton);
        designTypeGroup.Controls.Add(flexDesignRadioButton);
        designTypeGroup.Controls.Add(justShowActiveDesignRadioButton);

        transparencyLabel = new Label
        {
            Text = "Transparency",
            AutoSize = true,
            Location = new Point(LayoutMargin, 216)
        };

        transparencyTrackBar.Minimum = MinimumTransparencyPercent;
        transparencyTrackBar.Maximum = MaximumTransparencyPercent;
        transparencyTrackBar.TickFrequency = 10;
        transparencyTrackBar.SmallChange = 5;
        transparencyTrackBar.LargeChange = 10;
        transparencyTrackBar.Location = new Point(LayoutMargin, 240);
        transparencyTrackBar.Size = new Size(300, 45);
        transparencyTrackBar.ValueChanged += (_, _) => UpdateTransparencyValueLabel();

        transparencyValueLabel.AutoSize = true;
        transparencyValueLabel.Location = new Point(326, 246);

        fontSizeLabel = new Label
        {
            Text = "Font size",
            AutoSize = true,
            Location = new Point(LayoutMargin, 292)
        };

        fontSizeTrackBar.Minimum = OverlaySettings.MinFontSize;
        fontSizeTrackBar.Maximum = OverlaySettings.MaxFontSize;
        fontSizeTrackBar.TickFrequency = 2;
        fontSizeTrackBar.SmallChange = 1;
        fontSizeTrackBar.LargeChange = 2;
        fontSizeTrackBar.Location = new Point(LayoutMargin, 316);
        fontSizeTrackBar.Size = new Size(300, 45);
        fontSizeTrackBar.ValueChanged += (_, _) => UpdateFontSizeValueLabel();

        fontSizeValueLabel.AutoSize = true;
        fontSizeValueLabel.Location = new Point(326, 322);

        hotkeyGroup = new GroupBox
        {
            Text = "Desktop shortcuts",
            Location = new Point(LayoutMargin, 372),
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

            hotkeyRows.Add(new HotkeyRowControls(desktopIndex, hotkeyTextBox, setButton, clearButton, binding));
        }

        hotkeyGroup.Controls.Add(hotkeyStatusLabel);

        saveButton.Text = "Save";
        saveButton.Size = new Size(75, 26);
        saveButton.Click += (_, _) => SaveAndClose();

        cancelButton.Text = "Cancel";
        cancelButton.Size = new Size(75, 26);
        cancelButton.DialogResult = DialogResult.Cancel;

        AcceptButton = saveButton;
        CancelButton = cancelButton;

        Controls.Add(themeGroup);
        Controls.Add(designTypeGroup);
        Controls.Add(transparencyLabel);
        Controls.Add(transparencyTrackBar);
        Controls.Add(transparencyValueLabel);
        Controls.Add(fontSizeLabel);
        Controls.Add(fontSizeTrackBar);
        Controls.Add(fontSizeValueLabel);
        Controls.Add(hotkeyGroup);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);

        ApplySettingsToControls(settings);
    }

    private void LayoutControls()
    {
        if (themeGroup is null || designTypeGroup is null || hotkeyGroup is null)
        {
            return;
        }

        var contentWidth = ClientSize.Width - LayoutMargin * 2;
        var buttonTop = ClientSize.Height - LayoutMargin - saveButton.Height;

        themeGroup.Width = contentWidth;
        designTypeGroup.Top = themeGroup.Bottom + 10;
        designTypeGroup.Width = contentWidth;

        transparencyLabel!.Top = designTypeGroup.Bottom + 12;
        transparencyTrackBar.Top = transparencyLabel.Bottom + 4;
        transparencyTrackBar.Width = contentWidth - ValueLabelWidth;
        transparencyValueLabel.Left = transparencyTrackBar.Right + 8;
        transparencyValueLabel.Top = transparencyTrackBar.Top + 6;

        fontSizeLabel!.Top = transparencyTrackBar.Bottom + 12;
        fontSizeTrackBar.Top = fontSizeLabel.Bottom + 4;
        fontSizeTrackBar.Width = contentWidth - ValueLabelWidth;
        fontSizeValueLabel.Left = fontSizeTrackBar.Right + 8;
        fontSizeValueLabel.Top = fontSizeTrackBar.Top + 6;

        hotkeyGroup.Top = fontSizeTrackBar.Bottom + 16;
        hotkeyGroup.Width = contentWidth;
        hotkeyGroup.Height = Math.Max(180, buttonTop - hotkeyGroup.Top - 12);

        var hotkeyTextWidth = Math.Max(120, hotkeyGroup.Width - 96 - 44 - 52 - 24);
        var setButtonLeft = hotkeyGroup.Width - 44 - 52 - 12;
        var clearButtonLeft = hotkeyGroup.Width - 52 - 12;

        foreach (var row in hotkeyRows)
        {
            row.HotkeyTextBox.Width = hotkeyTextWidth;
            row.SetButton.Left = setButtonLeft;
            row.ClearButton.Left = clearButtonLeft;
        }

        hotkeyStatusLabel!.Top = hotkeyGroup.Height - 28;

        saveButton.Top = buttonTop;
        cancelButton.Top = buttonTop;
        cancelButton.Left = ClientSize.Width - LayoutMargin - cancelButton.Width;
        saveButton.Left = cancelButton.Left - 10 - saveButton.Width;
    }

    private void ApplySettingsToControls(OverlaySettings settings)
    {
        lightModeRadioButton.Checked = settings.Theme == OverlaySettings.LightTheme;
        darkModeRadioButton.Checked = !lightModeRadioButton.Checked;

        likeCurrentDesignRadioButton.Checked = settings.DesignType == OverlaySettings.LikeCurrentDesign;
        flexDesignRadioButton.Checked = settings.DesignType == OverlaySettings.FlexDesign;
        justShowActiveDesignRadioButton.Checked = settings.DesignType == OverlaySettings.JustShowActiveDesign;

        transparencyTrackBar.Value = Convert.ToInt32((1.0 - settings.Opacity) * 100);
        UpdateTransparencyValueLabel();

        fontSizeTrackBar.Value = settings.FontSize;
        UpdateFontSizeValueLabel();

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

    private void UpdateFontSizeValueLabel()
    {
        fontSizeValueLabel.Text = $"{fontSizeTrackBar.Value} pt";
    }

    private string GetSelectedDesignType()
    {
        if (flexDesignRadioButton.Checked)
        {
            return OverlaySettings.FlexDesign;
        }

        if (justShowActiveDesignRadioButton.Checked)
        {
            return OverlaySettings.JustShowActiveDesign;
        }

        return OverlaySettings.LikeCurrentDesign;
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
            DesignType = GetSelectedDesignType(),
            FontSize = fontSizeTrackBar.Value,
            Opacity = (100 - transparencyTrackBar.Value) / 100.0,
            DesktopHotkeys = hotkeyRows.Select(row => row.Binding).ToList()
        });

        Close();
    }

    private sealed class HotkeyRowControls(
        int desktopIndex,
        TextBox hotkeyTextBox,
        Button setButton,
        Button clearButton,
        DesktopHotkeyBinding binding)
    {
        public int DesktopIndex { get; } = desktopIndex;

        public TextBox HotkeyTextBox { get; } = hotkeyTextBox;

        public Button SetButton { get; } = setButton;

        public Button ClearButton { get; } = clearButton;

        public DesktopHotkeyBinding Binding { get; set; } = binding;
    }
}
