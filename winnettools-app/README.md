# WinNetTools — Red de Diagnóstico / Network Diagnostics

App de escritorio para diagnosticar problemas de red (ping, jitter, packet loss, DNS, traceroute, procesos que consumen ancho de banda). Pertenece al ecosistema WinOptimizer, pero no realiza ninguna modificación al sistema.

## Arquitectura

```
winnettools-app/
  diagnostics/          -> Pruebas de red no destructivas (ping, traceroute, DNS, detección WiFi, procesos)
  integration/          -> Capa de solo lectura para system-state.json del core
  report/               -> Generador de reportes exportables
  ui/                   -> Interfaz WPF (modo oscuro, i18n es/en)
    views/              -> Dashboard, Traceroute, DNS, Processes, Report
    i18n/               -> Recursos de idioma (es.xaml, en.xaml)
  tests/                -> Pruebas (Verify-CoreAccess.ps1)
  docs/                 -> Documentación
```

## Principios

1. No modifica ningún setting del sistema — toda acción correctiva se delega al WinOptimizer.
2. Pruebas de red no destructivas — no satura la red para medir.
3. Los reportes no incluyen datos personales sensibles (solo IP local y datos relevantes para diagnóstico).

## Cómo ejecutar

Necesitas .NET 8 SDK:

```bash
cd winnettools-app
dotnet run
```

## Idiomas

- Español (por defecto)
- English (botón "EN/ES" en la barra lateral)

## Integración con el ecosistema

- Lee `system-state.json` del core para detectar tweaks de red aplicados
- Si detecta un tweak de red no aplicado que podría ayudar, sugiere abrir el Optimizer
- Botón "Abrir en Optimizer" que lanza WinOptimizer.exe si está presente

## Licencia

MIT — igual que el core.
