# WinBenchmark — Benchmark de Sistema y Valorant

App de escritorio para medir rendimiento del sistema (CPU, RAM, temperaturas, ping/jitter) y captura de frames (FPS, 1% low, 0.1% low, frame time) usando PresentMon. Permite comparar sesiones "antes/después" de aplicar un perfil del Optimizer.

## Arquitectura

```
winbenchmark-app/
  capture/
    SystemMetrics.cs    → CPU/RAM/temperaturas (PerformanceCounter + WMI)
    PingJitter.cs       → Ping/jitter/packet loss (ICMP propio)
    FrameCapture.cs     → Captura de frames vía PresentMon (fallback software)
  sessions/
    SessionStore.cs     → Guarda/lee historial local en JSON
    Compare.cs          → Lógica de comparación antes/después
  integration/
    CoreStateReader.cs  → Solo lectura de system-state.json
  ui/views/
    DashboardView       → Resumen con últimas sesiones
    ActiveSessionView   → Control de benchmark en vivo + resultados
    CompareView         → Comparador antes/después
    HistoryView         → Historial completo de sesiones
```

## Principios

1. Solo lectura del sistema — no modifica ningún setting.
2. Las fuentes de cada métrica son trazables (PresentMon, PerformanceCounter, WMI, ICMP propio).
3. No hay conexión a servidores propios — todo el guardado es local JSON.
4. Si PresentMon no está disponible, usa fallback software (estimación ligera).

## Integración con el ecosistema

- Lee `system-state.json` al iniciar cada sesión para capturar qué tweaks/perfil estaban activos.
- El comparador detecta automáticamente si el estado del sistema cambió entre sesiones.
- Exporta sesiones individuales o completas como JSON para compartir.

## Licencia

MIT — igual que el core.
