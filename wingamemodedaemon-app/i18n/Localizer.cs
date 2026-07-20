namespace WinGameModeDaemon;

/// <summary>
/// Simple i18n string provider. Loads strings from embedded resources or falls back to Spanish defaults.
/// </summary>
public sealed class Localizer
{
    private readonly Dictionary<string, string> _strings = new();
    private readonly string _language;

    public Localizer(string language = "es")
    {
        _language = language.ToLowerInvariant();
        LoadDefaults();
    }

    public string Language => _language;

    public string this[string key] => _strings.TryGetValue(key, out var val) ? val : key;

    private void LoadDefaults()
    {
        if (_language == "en")
        {
            // English strings
            _strings["tray.status.inactive"] = "Inactive";
            _strings["tray.status.disabled"] = "Disabled";
            _strings["tray.status.gaming"] = "Gaming mode active";
            _strings["tray.status.reverting"] = "Reverting...";
            _strings["tray.no_game"] = "No game detected";
            _strings["tray.active_game"] = "Active game";
            _strings["tray.reverting"] = "Restoring previous state...";
            _strings["tray.daemon_disabled"] = "Daemon is disabled";
            _strings["tray.enable"] = "Enable";
            _strings["tray.disable"] = "Disable";
            _strings["tray.configure"] = "Configure watched games...";
            _strings["tray.open_config_file"] = "Open config file";
            _strings["tray.open_log"] = "Open log";
            _strings["tray.exit"] = "Exit";

            _strings["notif.gaming Activated"] = "Gaming mode activated";
            _strings["notif.gaming message"] = "Profile '{0}' applied for {1}.";
            _strings["notif.reverted"] = "Gaming mode deactivated";
            _strings["notif.reverted message"] = "Previous system state restored.";
            _strings["notif.error"] = "Error";
            _strings["notif.vanguard blocked"] = "Profile blocked — Vanguard detected";
            _strings["notif.vanguard message"] = "Incompatible tweaks were skipped.";

            _strings["config.title"] = "WinGameModeDaemon — Configure Watched Games";
            _strings["config.label"] = "Executable names to monitor (one per line):";
            _strings["config.save"] = "Save";
            _strings["config.cancel"] = "Cancel";
            _strings["config.detect"] = "Auto-detect installed games";
            _strings["config.profile"] = "Profile to apply:";
        }
        else
        {
            // Spanish defaults
            _strings["tray.status.inactive"] = "Inactivo";
            _strings["tray.status.disabled"] = "Desactivado";
            _strings["tray.status.gaming"] = "Modo gaming activo";
            _strings["tray.status.reverting"] = "Revirtiendo...";
            _strings["tray.no_game"] = "Sin juego detectado";
            _strings["tray.active_game"] = "Juego activo";
            _strings["tray.reverting"] = "Restaurando estado anterior...";
            _strings["tray.daemon_disabled"] = "Daemon desactivado";
            _strings["tray.enable"] = "Activar";
            _strings["tray.disable"] = "Desactivar";
            _strings["tray.configure"] = "Configurar juegos vigilados...";
            _strings["tray.open_config_file"] = "Abrir archivo de config";
            _strings["tray.open_log"] = "Abrir log";
            _strings["tray.exit"] = "Salir";

            _strings["notif.gaming Activated"] = "Modo gaming activado";
            _strings["notif.gaming message"] = "Perfil '{0}' aplicado para {1}.";
            _strings["notif.reverted"] = "Modo gaming desactivado";
            _strings["notif.reverted message"] = "Estado del sistema restaurado.";
            _strings["notif.error"] = "Error";
            _strings["notif.vanguard blocked"] = "Perfil bloqueado — Vanguard detectado";
            _strings["notif.vanguard message"] = "Tweaks incompatibles fueron omitidos.";

            _strings["config.title"] = "WinGameModeDaemon — Configurar Juegos Vigilados";
            _strings["config.label"] = "Nombres de ejecutables a monitorear (uno por línea):";
            _strings["config.save"] = "Guardar";
            _strings["config.cancel"] = "Cancelar";
            _strings["config.detect"] = "Detectar juegos instalados";
            _strings["config.profile"] = "Perfil a aplicar:";
        }
    }
}
