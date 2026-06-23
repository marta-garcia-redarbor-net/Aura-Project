# Calendar — Reuniones y notificaciones

Calendar no es ingestión de triaje. Es un dominio de contexto temporal con dos responsabilidades:
visualizar las reuniones del día en el dashboard y avisar al usuario antes de que empiecen.

## Decisiones de dominio

- Los `CalendarEvent` NO son `WorkItem`. No entran al triage ni al pipeline de ingestión.
- Calendar es un dominio propio en `Aura.Domain/Calendar/`.
- Las alertas se almacenan en `aura.db` (SQLite compartido), tabla `meeting_alerts`.
- Avisos a los **60, 10 y 5 minutos** antes del inicio de cada reunión.
- Si hay múltiples tabs abiertos, solo uno dispara la notificación (deduplicación via SignalR + JS).

## Quick path

1. `MeetingAlertWorker` hace polling cada 2 minutos.
2. `CheckAndDispatchMeetingAlertsUseCase` obtiene eventos del día via `ICalendarEventProvider`.
3. Por cada evento × trigger (60/10/5 min): si es due y no fue enviado → marcar → despachar.
4. `SignalRMeetingAlertDispatcher` envía al grupo SignalR del usuario.
5. El browser recibe la notificación, reproduce sonido y muestra toast.

## Modelo de dominio

```
CalendarEvent        — Id, Title, StartUtc, EndUtc, IsOnlineMeeting, JoinUrl?
MeetingAlertTrigger  — SixtyMinutes | TenMinutes | FiveMinutes
MeetingAlert         — EventId, Title, Trigger, StartsAtUtc, JoinUrl?
```

## Puertos (Application)

| Puerto | Responsabilidad |
|--------|----------------|
| `ICalendarEventProvider` | `GetEventsAsync(date, userId)` → eventos del día |
| `IMeetingAlertStore` | `HasBeenSentAsync` / `MarkSentAsync` — idempotencia |
| `IMeetingAlertDispatcher` | `DispatchAsync(alert)` — salida hacia SignalR |

## Use Cases (Application)

| Use Case | Responsabilidad |
|----------|----------------|
| `GetUpcomingMeetingsUseCase` | Para el dashboard — devuelve reuniones del día |
| `CheckAndDispatchMeetingAlertsUseCase` | Para el worker — detecta alertas due y las despacha |

## Adaptadores (Infrastructure)

| Adaptador | Path | Detalle |
|-----------|------|---------|
| `GraphCalendarEventProvider` | `Adapters/Calendar/` | Llama `/me/calendarView` via `GraphServiceClient` |
| `SqliteMeetingAlertStore` | `Adapters/Calendar/` | PK `(EventId, Trigger, LocalDate)` en `aura.db` |
| `SignalRMeetingAlertDispatcher` | `Adapters/Calendar/` | Envía al hub `MeetingAlertHub` |
| `GraphClientFactory` | `Adapters/Graph/` | `GraphServiceClient` singleton compartido |

## Graph auth

- Credencial: `ClientSecretCredential` (permiso Application, no Delegated).
- Permiso requerido: `Calendars.Read` con admin consent en Entra ID.
- Secrets: User Secrets en desarrollo (`dotnet user-secrets`), variables de entorno en CI/CD.
- **Nunca en appsettings.** La sección `GraphConnector` en `appsettings.Development.json` solo contiene claves vacías como referencia de estructura.
- Proyectos que necesitan User Secrets: `Aura.Api` y `Aura.Workers`.

## SignalR hub

```
Aura.Api/Hubs/MeetingAlertHub.cs
  — Grupos por UserId
  — Método AcknowledgeAlert(eventId, trigger) para deduplicación entre tabs
```

## Deduplicación entre tabs

```
Worker detecta alerta due
  → IMeetingAlertStore.MarkSentAsync()      ← escribe ANTES de enviar
  → IMeetingAlertDispatcher.DispatchAsync()
      → SignalR → todos los tabs del usuario
          → JS: Web Notification API + Audio.play()
          → Tab llama AcknowledgeAlert() en el Hub
          → Los demás tabs ignoran (flag local en JS)
```

La deduplicación real la garantiza `IMeetingAlertStore`: si el worker ya marcó la alerta como enviada, no vuelve a disparar en el siguiente ciclo de polling aunque haya lag.

## UI

| Componente | Responsabilidad |
|------------|----------------|
| `UpcomingMeetingsPanel.razor` | Lista reuniones del día: título, hora, duración, link de Teams si existe. Refresco cada 5 min. Degrada con mensaje si falla el provider. |
| `MeetingAlertToast.razor` | Recibe push de SignalR, llama JS interop para notification + sonido |
| `wwwroot/js/meetingAlert.js` | Web Notification API + `Audio.play()`. Solicita permiso al cargar. |

## Slices de implementación

| Slice | Contenido |
|-------|-----------|
| W2-H7-T1 | Graph shared client (`GraphClientFactory`, DI) + User Secrets setup en Api y Workers |
| W2-H7-T2 | Domain (`CalendarEvent`, `MeetingAlertTrigger`, `MeetingAlert`) + puertos + use cases + `GraphCalendarEventProvider` |
| W2-H7-T3 | `SqliteMeetingAlertStore` + `MeetingAlertHub` + `MeetingAlertWorker` |
| W2-H7-T4 | `meetingAlert.js` + `MeetingAlertToast.razor` + deduplicación JS |
| W2-H7-T5 | `UpcomingMeetingsPanel.razor` en dashboard |

## Dependencias entre slices

```
T1 → T2 → T3 → T4
               T3 → T5
```

T4 y T5 dependen de T3 (hub + store listos) pero son independientes entre sí y pueden implementarse en paralelo.
