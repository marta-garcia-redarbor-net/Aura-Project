# Reporte de archivo: outlook-unread-only

**Change**: `outlook-unread-only`
**Fecha de archivo**: 2026-07-02
**Modo**: openspec
**Veredicto al archivar**: PASS WITH WARNINGS

## Operación realizada

- Se normalizó el delta plano `spec.md` en la estructura OpenSpec `specs/{dominio}/spec.md` para preservar el cambio en el formato de archivo usado por el repositorio.
- Se sincronizaron los deltas sobre los specs fuente de verdad de `outlook-connector-mapping`, `connector-execution` y `work-item-persistence`.
- Se preservó `verify-report.md` sin cambios.
- Se conserva el `spec.md` original dentro del cambio archivado como evidencia histórica; los deltas normalizados reflejan la estructura estándar del repositorio.

## Artefactos archivados

| Artefacto | Estado | Ruta esperada en archivo |
|-----------|--------|--------------------------|
| `proposal.md` | ✅ | `openspec/changes/archive/2026-07-02-outlook-unread-only/proposal.md` |
| `spec.md` original | ✅ | `openspec/changes/archive/2026-07-02-outlook-unread-only/spec.md` |
| `specs/` normalizado | ✅ | `openspec/changes/archive/2026-07-02-outlook-unread-only/specs/...` |
| `design.md` | ✅ | `openspec/changes/archive/2026-07-02-outlook-unread-only/design.md` |
| `tasks.md` | ✅ | `openspec/changes/archive/2026-07-02-outlook-unread-only/tasks.md` |
| `verify-report.md` | ✅ | `openspec/changes/archive/2026-07-02-outlook-unread-only/verify-report.md` |
| `archive-report.md` | ✅ | `openspec/changes/archive/2026-07-02-outlook-unread-only/archive-report.md` |

## Specs sincronizadas

| Dominio | Acción | Detalle |
|---------|--------|---------|
| `outlook-connector-mapping` | Actualizado | Se añadieron requisitos para filtro inbox unread y mapeo de `OutlookEmailDto.IsRead`. |
| `connector-execution` | Actualizado | Se añadieron requisitos para diff lifecycle post-persistencia y ownership del filtrado Pending en Application/store. |
| `work-item-persistence` | Actualizado | Se añadieron requisitos para `GetPendingExternalIdsAsync` y `MarkCompletedAsync` con filtro por source y estados TEXT. |

## Estado de tareas

| Métrica | Valor |
|---------|-------|
| Tareas totales | 16 |
| Tareas completas | 16 |
| Tareas incompletas | 0 |

## Advertencias registradas

- El veredicto `PASS WITH WARNINGS` se mantiene porque los fallos de Integration/E2E auth, Playwright y bloqueos transientes de `MvcTestingAppManifest.json` pertenecen al baseline del workspace y quedaron explícitamente fuera del alcance de este cambio.
- El `spec.md` plano original conserva wording previo con `Status = 0`; los specs normalizados y la fuente de verdad se sincronizaron con el comportamiento verificado real, usando estados TEXT (`"Pending"`, `"Completed"`) según `design.md` y `verify-report.md`.

## Fuente de verdad actualizada

- `openspec/specs/outlook-connector-mapping/spec.md`
- `openspec/specs/connector-execution/spec.md`
- `openspec/specs/work-item-persistence/spec.md`

## Cierre

El cambio queda archivado como auditoría completa: propuesta, diseño, tareas, verificación, delta plano original y deltas normalizados. No quedan tareas de implementación abiertas para este ciclo SDD.
