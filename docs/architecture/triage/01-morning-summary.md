# Triage — Morning Summary

Contrato operativo de **W2-H5-T3** para la emisión diaria de Morning Summary.

Define únicamente scheduling, timezone y reglas de idempotencia diaria a partir de `Project/System Settings`.

## Quick path

1. Leer `Project/System Settings` y obtener `timezoneId` + `targetLocalTime`.
2. Resolver timezone efectiva con cadena: configurada → timezone del sistema → `UTC`.
3. Calcular `isDue` para `targetLocalTime` como hora local (wall-clock), aplicando reglas IANA/DST.
4. Emitir como máximo un Morning Summary por usuario y día local.

## Contrato operativo

| Tema | Regla |
| --- | --- |
| Modelo global de settings | Fuente única compartida: `Project/System Settings`. |
| Campos mínimos | `timezoneId` y `targetLocalTime` configurables (valor esperado por defecto: `09:00`). |
| Cadena de timezone | `timezoneId` configurado → timezone del sistema → `UTC` (fallback final). |
| Semántica de hora objetivo | `targetLocalTime` representa hora local (wall-clock) en la timezone resuelta; no es una hora UTC fija. |
| DST / horario de verano | Se aplica automáticamente según reglas de la timezone resuelta; no usar offsets UTC hardcodeados. |
| Idempotencia diaria | Máximo un Morning Summary por usuario por día local. |
| Reejecución en el mismo día local | Debe tratarse como “ya emitido” (sin duplicados). |
| Cambio de timezone en el mismo día local | Mantiene estado “ya emitido”; no habilita segundo summary. |
| Resultado del scheduler | Debe exponer `resolvedTimezoneId`, `localDate`, `targetLocalTime` e `isDue`. Si no está vencido, basta con `isDue = false`. |

## Límite de alcance

- Incluido: scheduling + timezone + idempotencia de emisión diaria.
- Excluido: ranking, composición del contenido y semántica de data windows timezone-aware.
