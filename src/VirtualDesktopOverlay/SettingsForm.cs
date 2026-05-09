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
    private readonly Action<OverlaySettings> saveSettings;

    public SettingsForm(OverlaySettings settings, Action<OverlaySettings> saveSettings)
    {
        this.saveSettings = saveSettings;

        Text = "Appearance Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 238);

        ConfigureControls(settings);
    }

    private void ConfigureControls(OverlaySettings settings)
    {
        var themeGroup = new GroupBox
        {
            Text = "Theme",
            Location = new Point(16, 16),
            Size = new Size(328, 78)
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
            Location = new Point(16, 112)
        };

        transparencyTrackBar.Minimum = MinimumTransparencyPercent;
        transparencyTrackBar.Maximum = MaximumTransparencyPercent;
        transparencyTrackBar.TickFrequency = 10;
        transparencyTrackBar.SmallChange = 5;
        transparencyTrackBar.LargeChange = 10;
        transparencyTrackBar.Location = new Point(16, 136);
        transparencyTrackBar.Size = new Size(262, 45);
        transparencyTrackBar.ValueChanged += (_, _) => UpdateTransparencyValueLabel();

        transparencyValueLabel.AutoSize = true;
        transparencyValueLabel.Location = new Point(288, 142);

        saveButton.Text = "Save";
        saveButton.Location = new Point(184, 198);
        saveButton.Size = new Size(75, 26);
        saveButton.Click += (_, _) => SaveAndClose();

        cancelButton.Text = "Cancel";
        cancelButton.Location = new Point(269, 198);
        cancelButton.Size = new Size(75, 26);
        cancelButton.DialogResult = DialogResult.Cancel;

        AcceptButton = saveButton;
        CancelButton = cancelButton;

        Controls.Add(themeGroup);
        Controls.Add(transparencyLabel);
        Controls.Add(transparencyTrackBar);
        Controls.Add(transparencyValueLabel);
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
    }

    private void UpdateTransparencyValueLabel()
    {
        transparencyValueLabel.Text = $"{transparencyTrackBar.Value}%";
    }

    private void SaveAndClose()
    {
        saveSettings(new OverlaySettings
        {
            Theme = lightModeRadioButton.Checked ? OverlaySettings.LightTheme : OverlaySettings.DarkTheme,
            Opacity = (100 - transparencyTrackBar.Value) / 100.0
        });

        Close();
    }
}
