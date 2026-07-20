# Solicitudes Pendientes al Core Engine

## 1. Exponer Get-Profiles como función oficial

La app necesita una lista de perfiles. Actualmente lee los archivos .json directamente de `profiles/`. Debería haber una función `Get-Profiles` oficial en el módulo.

## 2. Incluir información de Vanguard en system-state.json

Actualmente `system_snapshot.vanguard_installed` existe pero no hay tweaks marcados como incompatibles con Vanguard en los perfiles (todos son compatibles). Si se añaden tweaks conflictivos, el benchmark podría filtrar sugerencias.

## 3. Añadir sesión de benchmark al core como función opcional

No es necesario ahora, pero si en el futuro se quisiera que el core tenga su propia lógica de "test de estrés" para medir antes/después de aplicar tweaks, se podría exponer como función opt-in.
