# Triáje — Morning Summary

Contrato operativo para emisión del Morning Summary diario según `Project/System Settings`, con reglas explícitas de timezone, hora objetivo configurable, idempotencia y observabilidad.

## Quick path

1. Resolver `Project/System Settings` y obtener `timezoneId` y `targetLocalTime`.
2. Resolver timezone efectiva (configurada en settings; fallback al sistema y luego UTC).
3. Determinar due-state para `targetLocalTime` (wall-clock) usando reglas IANA/DST.
4. Emitir como máximo un Morning Summary por usuario y día local.

## Contrato de scheduling (W2-H5-T3)

| Tema | Regla |
| --- | --- |
| Fuente de verdad de scheduling | Objeto único `Project/System Settings` compartido por el proyecto/sistema. |
| Campos mínimos de settings | `timezoneId` configurado y `targetLocalTime` configurado (por defecto esperado: `09:00`). |
| Fallback de timezone | Si falta o es inválida, usar timezone del sistema. |
| Falla resolviendo timezone del sistema | Usar `UTC` como fallback final. |
| Semántica de hora objetivo | `targetLocalTime` significa hora local (wall-clock) en la timezone resuelta; es configurable y no hardcodeada. |
| DST / horario de verano | Se aplica automáticamente por reglas de timezone; nunca usar offsets UTC fijos. |
| Idempotencia diaria | Máximo un Morning Summary por usuario por día local. |
| Reejecución del scheduler el mismo día | Las ejecuciones posteriores deben tratarlo como “ya emitido”. |
| Cambio de timezone del proyecto/sistema durante el día | Sigue contando como “ya emitido”; no generar un segundo summary. |
| Resultado del scheduler | Debe exponer `resolvedTimezoneId`, `localDate`, `targetLocalTime` (configurada; por defecto esperado `09:00`) y `isDue`. Si no está vencido, basta con `isDue = false`. |

## Alcance de este corte

- Incluido: scheduling + timezone + idempotencia de emisión diaria.
- Excluido: interpretación timezone-aware de ventanas de datos, ranking o semántica de composición del contenido.
