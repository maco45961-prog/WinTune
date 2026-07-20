# Solicitudes Pendientes al Core Engine

## 1. Exponer Get-Profiles como función oficial

La app necesita una lista de perfiles. Actualmente lee los archivos .json directamente de `profiles/`. Debería haber una función `Get-Profiles` oficial en el módulo.

## 2. Restore-SystemBackup como función auto-contenida

Actualmente `Restore-SystemBackup` está dentro de `New-SystemBackup.ps1`. Sería más limpio tener un archivo separado `Restore-SystemBackup.ps1`.

## 3. Añadir tweak `optimize-dns` con cambio automático de DNS

Si el diagnóstico de WinNetTools detecta que el DNS del sistema es lento, podría sugerir un tweak que permita cambiarlo a Cloudflare/Google/Quad9 desde el Optimizer. Actualmente no existe ese tweak en el core.

## 4. Añadir tweak `disable-delivery-optimization` para desactivar P2P de Windows Update

Si WinNetTools detecta que Delivery Optimization está activo, el Optimizer podría ofrecer desactivarlo.
