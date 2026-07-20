using System.Drawing;
using System.Windows.Forms;
using WinGameModeDaemon.Config;

namespace WinGameModeDaemon;

/// <summary>
/// Simple dark-themed form for editing the watched games configuration.
/// Opens from tray menu, no main window behavior.
/// </summary>
public sealed class ConfigForm : Form
{
    private readonly TextBox _executablesBox;
    private readonly TextBox _profileBox;
    private readonly Action<WatchedGamesConfig> _onSave;
    private readonly WatchedGamesConfig _original;
    private readonly Localizer _loc;

    public ConfigForm(WatchedGamesConfig config, Localizer loc, Action<WatchedGamesConfig> onSave)
    {
        _loc = loc;
        _onSave = onSave;
        _original = config;

        // Form setup
        Text = loc["config.title"];
        Size = new Size(480, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.FromArgb(220, 220, 220);

        // Executables label
        var labelExec = new Label
        {
            Text = loc["config.label"],
            Location = new Point(12, 12),
            AutoSize = true,
            ForeColor = Color.FromArgb(200, 200, 200)
        };
        Controls.Add(labelExec);

        // Executables text box
        _executablesBox = new TextBox
        {
            Location = new Point(12, 35),
            Size = new Size(440, 200),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.FromArgb(220, 220, 220),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 10f),
            Text = string.Join(Environment.NewLine, config.Executables)
        };
        Controls.Add(_executablesBox);

        // Profile label
        var labelProfile = new Label
        {
            Text = loc["config.profile"],
            Location = new Point(12, 250),
            AutoSize = true,
            ForeColor = Color.FromArgb(200, 200, 200)
        };
        Controls.Add(labelProfile);

        // Profile text box
        _profileBox = new TextBox
        {
            Location = new Point(12, 273),
            Size = new Size(440, 25),
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.FromArgb(220, 220, 220),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 10f),
            Text = config.ProfileToApply
        };
        Controls.Add(_profileBox);

        // Auto-detect button
        var detectBtn = new Button
        {
            Text = loc["config.detect"],
            Location = new Point(12, 310),
            Size = new Size(215, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.FromArgb(200, 200, 200),
            Cursor = Cursors.Hand
        };
        detectBtn.Click += (_, _) => DetectGames();
        Controls.Add(detectBtn);

        // Save button
        var saveBtn = new Button
        {
            Text = loc["config.save"],
            Location = new Point(237, 310),
            Size = new Size(108, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 120, 40),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        saveBtn.Click += (_, _) => SaveAndClose();
        Controls.Add(saveBtn);

        // Cancel button
        var cancelBtn = new Button
        {
            Text = loc["config.cancel"],
            Location = new Point(355, 310),
            Size = new Size(97, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(120, 40, 40),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        cancelBtn.Click += (_, _) => Close();
        Controls.Add(cancelBtn);

        AcceptButton = saveBtn;
        CancelButton = cancelBtn;
    }

    private void SaveAndClose()
    {
        var execs = _executablesBox.Text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        var newConfig = new WatchedGamesConfig
        {
            Executables = execs,
            ProfileToApply = _profileBox.Text.Trim(),
            Enabled = _original.Enabled,
            NotificationsEnabled = _original.NotificationsEnabled,
            StartWithWindows = _original.StartWithWindows
        };

        _onSave(newConfig);
        Close();
    }

    private void DetectGames()
    {
        var detected = ConfigManager.DetectInstalledGames();
        if (detected.Count > 0)
        {
            _executablesBox.Text = string.Join(Environment.NewLine, detected);
        }
        else
        {
            MessageBox.Show("No games detected in common install paths.", "Detection",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Color.FromArgb(60, 60, 60));
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
