# Microsoft Graph — Calendar Connector

> Placeholder. Este documento debe definir la ingestión de reuniones, cambios de agenda y señales de calendario relevantes para el triáje.

## Quick path

1. Identificar eventos de calendario que impactan foco y prioridad.
2. Definir sync incremental y manejo de cambios/cancelaciones.
3. Diseñar normalización, timezone y ventanas laborales.

## Debe cubrir

- Contrato `IExternalConnector<CalendarEvent>`.
- Normalización de meetings, attendees y urgencia temporal.
- Reglas de timezone, horario laboral y próximas reuniones.
- Idempotencia frente a updates y cancellations.
- Tests para edge cases de recurrencia.

## Pendiente

- [ ] Completar diseño de señales de calendario para foco e interrupciones.
