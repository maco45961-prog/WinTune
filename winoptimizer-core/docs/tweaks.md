# WinOptimizer Core — Tweaks Catalog

Each tweak is a self-contained JSON definition in `tweaks/<category>/`.

**Total tweaks: 15** across 7 categories.

## Privacy (3)

| ID | Name | Risk | Reversible | Vanguard | Restart |
|---|---|---|---|---|---|
| `disable-telemetry` | Deshabilitar Telemetría de Windows | medium | Yes | Yes | No |
| `disable-advertising-id` | Deshabilitar ID de Publicidad | low | Yes | Yes | No |
| `disable-location-tracking` | Deshabilitar Rastreo de Ubicación | low | Yes | Yes | No |

- **`disable-telemetry`** — Desactiva la recopilación de telemetría y datos de diagnóstico. Detiene servicios DiagTrack y dmwappushservice.
- **`disable-advertising-id`** — Desactiva el identificador de publicidad, impidiendo que las apps rastreen al usuario para publicidad personalizada.
- **`disable-location-tracking`** — Desactiva el servicio lfsvc y políticas de ubicación. Las apps no podrán acceder a la ubicación del dispositivo.

## Services (3)

| ID | Name | Risk | Reversible | Vanguard | Restart |
|---|---|---|---|---|---|
| `disable-windows-search-indexer` | Deshabilitar Windows Search Indexer | low | Yes | Yes | No |
| `disable-sysmain` | Deshabilitar SysMain (Superfetch) | low | Yes | Yes | No |
| `disable-bits-background` | Deshabilitar BITS en Background | medium | Yes | Yes | No |

- **`disable-windows-search-indexer`** — Detiene y deshabilita el servicio WSearch. Libera disco I/O y CPU.
- **`disable-sysmain`** — Desactiva SysMain/Superfetch. Beneficioso en SSDs para reducir escrituras innecesarias.
- **`disable-bits-background`** — Detiene BITS para evitar descargas en background. ⚠️ Las actualizaciones de Windows dejarán de descargar automáticamente.

## Network (2)

| ID | Name | Risk | Reversible | Vanguard | Restart |
|---|---|---|---|---|---|
| `disable-nagle-algorithm` | Deshabilitar Algoritmo de Nagle | medium | Yes | Yes | Yes |
| `optimize-dns` | Optimizar Configuración DNS | medium | Yes | Yes | No |

- **`disable-nagle-algorithm`** — Establece TcpAckFrequency=1 y TCPNoDelay=1 en todas las interfaces. Elimina buffer de Nagle, reduciendo latencia en gaming competitivo y VoIP. Requiere reinicio.
- **`optimize-dns`** — Configura Cloudflare DNS (1.1.1.1/1.0.0.1) en todas las interfaces activas. Requiere reinicio.

## Gaming (2)

| ID | Name | Risk | Reversible | Vanguard | Restart |
|---|---|---|---|---|---|
| `enable-hardware-gpu-scheduling` | Habilitar Programación de GPU por Hardware | low | Yes | Yes | Yes |
| `disable-power-throttling` | Deshabilitar Power Throttling | low | Yes | Yes | No |

- **`enable-hardware-gpu-scheduling`** — Activa HAGS (Hardware-Accelerated GPU Scheduling). Permite que la GPU gestione su propia memoria de video. Requiere reinicio.
- **`disable-power-throttling`** — Desactiva el limitado de potencia de Windows. Mejora FPS en juegos y rendimiento en apps de productividad.

## Appearance (3)

| ID | Name | Risk | Reversible | Vanguard | Restart |
|---|---|---|---|---|---|
| `disable-transparency-effects` | Deshabilitar Efectos de Transparencia | low | Yes | Yes | No |
| `disable-animations` | Deshabilitar Animaciones de Windows | low | Yes | Yes | No |
| `enable-dark-mode` | Habilitar Modo Oscuro | low | Yes | Yes | No |

- **`disable-transparency-effects`** — Desactiva las transparencias Aero Glass. Reduce consumo de GPU.
- **`disable-animations`** — Desactiva todas las animaciones de la interfaz (minimizar, maximizar, transiciones). La UI se siente más rápida.
- **`enable-dark-mode`** — Activa el tema oscuro en Windows y las apps del sistema. Mejor para pantallas OLED y para los ojos.

## Start Menu / Taskbar (2)

| ID | Name | Risk | Reversible | Vanguard | Restart |
|---|---|---|---|---|---|
| `disable-cortana` | Deshabilitar Cortana | medium | Yes | Yes | No |
| `disable-widgets` | Deshabilitar Widgets | low | Yes | Yes | No |

- **`disable-cortana`** — Desactiva Cortana y su barra de búsqueda web. Políticas de grupo en `HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search`.
- **`disable-widgets`** — Desactiva el panel de Widgets de Windows 11 que consume recursos y bandwidth en segundo plano.

## Explorer (3)

| ID | Name | Risk | Reversible | Vanguard | Restart |
|---|---|---|---|---|---|
| `hide-file-extension` | Mostrar Extensiones de Archivo | low | Yes | Yes | No |
| `disable-quick-access` | Deshabilitar Acceso Rápido en Explorer | low | Yes | Yes | No |
| `enable-natural-sorting` | Habilitar Ordenamiento Natural | low | Yes | Yes | No |

- **`hide-file-extension`** — Fuerza la visualización de extensiones de archivo. Protege contra malware con doble extensión.
- **`disable-quick-access`** — Muestra "Este equipo" por defecto en lugar de Acceso Rápido. Reduce rastreo de archivos recientes.
- **`enable-natural-sorting`** — Activa ordenamiento natural (file1, file2, file10 en vez de file1, file10, file2).

---

## Adding New Tweaks

1. Create a JSON file in the appropriate `tweaks/<category>/` directory.
2. Follow the schema in `shared/schemas/tweak.schema.json`.
3. The tweak will be automatically discovered by `Get-AvailableTweaks`.
4. Document it in this file.
5. Run `Invoke-ScriptAnalyzer` on related engine scripts.
