using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinGameModeDaemon.Tray;

/// <summary>
/// System tray icon with context menu. Manages the NotifyIcon lifecycle.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _contextMenu;
    private readonly Localizer _loc;
    private bool _disposed;

    // Menu items (stored for updates)
    private ToolStripMenuItem? _statusItem;
    private ToolStripMenuItem? _enableItem;
    private ToolStripMenuItem? _activeGameItem;

    // Events raised from menu clicks
    public event Action? OnToggleEnabled;
    public event Action? OnOpenConfig;
    public event Action? OnOpenLog;
    public event Action? OnOpenConfigFile;
    public event Action? OnExit;

    public TrayIconManager(Localizer loc)
    {
        _loc = loc;
    }

    /// <summary>Creates and shows the tray icon. Call on the UI thread.</summary>
    public void Create()
    {
        _contextMenu = new ContextMenuStrip();
        _contextMenu.Renderer = new DarkMenuRenderer();

        // Status (read-only, bold)
        _statusItem = new ToolStripMenuItem(_loc["tray.status.inactive"]) { Enabled = false, Font = new Font(SystemFonts.MenuFont!, FontStyle.Bold) };
        _contextMenu.Items.Add(_statusItem);

        _activeGameItem = new ToolStripMenuItem(_loc["tray.no_game"]) { Enabled = false };
        _contextMenu.Items.Add(_activeGameItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Enable / Disable toggle
        _enableItem = new ToolStripMenuItem(_loc["tray.disable"]);
        _enableItem.Click += (_, _) => OnToggleEnabled?.Invoke();
        _contextMenu.Items.Add(_enableItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Configure watched games
        var configItem = new ToolStripMenuItem(_loc["tray.configure"]);
        configItem.Click += (_, _) => OnOpenConfig?.Invoke();
        _contextMenu.Items.Add(configItem);

        // Open config file
        var configFileItem = new ToolStripMenuItem(_loc["tray.open_config_file"]);
        configFileItem.Click += (_, _) => OnOpenConfigFile?.Invoke();
        _contextMenu.Items.Add(configFileItem);

        // Open log
        var logItem = new ToolStripMenuItem(_loc["tray.open_log"]);
        logItem.Click += (_, _) => OnOpenLog?.Invoke();
        _contextMenu.Items.Add(logItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Exit
        var exitItem = new ToolStripMenuItem(_loc["tray.exit"]);
        exitItem.Click += (_, _) => OnExit?.Invoke();
        _contextMenu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(Color.FromArgb(100, 100, 100)),
            ContextMenuStrip = _contextMenu,
            Text = "WinGameModeDaemon",
            Visible = true
        };

        _trayIcon.DoubleClick += (_, _) => OnOpenConfig?.Invoke();
    }

    /// <summary>Updates the tray icon to reflect the current daemon state.</summary>
    public void SetState(DaemonState state, string? gameName = null)
    {
        if (_trayIcon == null) return;

        switch (state)
        {
            case DaemonState.Inactive:
                _trayIcon.Icon = CreateTrayIcon(Color.FromArgb(100, 100, 100));
                _trayIcon.Text = "WinGameModeDaemon - " + _loc["tray.status.inactive"];
                _statusItem!.Text = _loc["tray.status.inactive"];
                _activeGameItem!.Text = _loc["tray.no_game"];
                break;

            case DaemonState.Disabled:
                _trayIcon.Icon = CreateTrayIcon(Color.FromArgb(150, 50, 50));
                _trayIcon.Text = "WinGameModeDaemon - " + _loc["tray.status.disabled"];
                _statusItem!.Text = _loc["tray.status.disabled"];
                _activeGameItem!.Text = _loc["tray.daemon_disabled"];
                break;

            case DaemonState.Gaming:
                _trayIcon.Icon = CreateTrayIcon(Color.FromArgb(50, 180, 50));
                _trayIcon.Text = $"WinGameModeDaemon - {_loc["tray.status.gaming"]} ({gameName})";
                _statusItem!.Text = _loc["tray.status.gaming"];
                _activeGameItem!.Text = gameName ?? _loc["tray.active_game"];
                break;

            case DaemonState.Reverting:
                _trayIcon.Icon = CreateTrayIcon(Color.FromArgb(200, 180, 50));
                _trayIcon.Text = "WinGameModeDaemon - " + _loc["tray.status.reverting"];
                _statusItem!.Text = _loc["tray.status.reverting"];
                _activeGameItem!.Text = _loc["tray.reverting"];
                break;
        }
    }

    /// <summary>Updates the enable/disable menu text.</summary>
    public void SetEnabledState(bool enabled)
    {
        if (_enableItem == null) return;
        _enableItem.Text = enabled ? _loc["tray.disable"] : _loc["tray.enable"];
    }

    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _trayIcon?.ShowBalloonTip(3000, title, message, icon);
    }

    private static Icon CreateTrayIcon(Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 14, 14);

        // Small "G" letter in white for Game Mode
        using var font = new Font("Segoe UI", 7f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(Color.White);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("G", font, textBrush, 7.5f, 6.5f, sf);

        var hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
        _contextMenu?.Dispose();
    }
}

public enum DaemonState
{
    Inactive,
    Disabled,
    Gaming,
    Reverting
}

/// <summary>Dark-themed menu renderer for the context menu.</summary>
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var rect = new Rectangle(Point.Empty, e.Item.Size);
        var color = e.Item.Selected ? Color.FromArgb(60, 60, 60) : Color.FromArgb(40, 40, 40);
        using var brush = new SolidBrush(color);
        e.Graphics.FillRectangle(brush, rect);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Color.FromArgb(40, 40, 40));
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var rect = new Rectangle(Point.Empty, new Size(e.Item.Width, 1));
        using var pen = new Pen(Color.FromArgb(70, 70, 70));
        e.Graphics.DrawLine(pen, rect.Location, new Point(rect.Right, rect.Y));
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Color.FromArgb(220, 220, 220) : Color.FromArgb(120, 120, 120);
        base.OnRenderItemText(e);
    }
}
