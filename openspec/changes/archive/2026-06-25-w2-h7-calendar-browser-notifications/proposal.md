# Proposal: Calendar Browser Notifications (T4)

## Intent

Users miss meeting alerts because there's no browser-level notification. T3 delivers alerts to a SignalR hub, but the UI has no client-side listener — alerts vanish without visual or audible feedback. This change adds browser notifications with sound so users are alerted even when the tab is backgrounded.

## Scope

### In Scope
- SignalR client connection in Blazor UI (real MSAL token, not mock JWT)
- `MeetingAlertToast.razor` — in-page toast with acknowledge action
- `meetingAlert.js` — Web Notification API + audio playback via JS interop
- MSAL token acquisition service replacing mock JWT for SignalR auth
- MSAL configuration wired into `Program.cs` using same Entra ID App Registration as Graph
- Notification permission request on user gesture; fallback to in-page toast

### Out of Scope
- Custom notification sound upload (uses default audio file bundled in wwwroot)
- Multi-tab notification coordination
- Push notification via service worker (future enhancement)
- Desktop-native notification settings UI

## Capabilities

### New Capabilities
- `browser-notifications`: Browser Notification API integration, audio playback, permission management, and in-page toast fallback for meeting alerts

### Modified Capabilities
- `api-authentication`: SignalR hub connection now uses real MSAL token acquisition instead of mock JWT; same Entra ID App Registration with additional API scope

## Approach

**Hybrid SignalR + JS Interop**: Server pushes `MeetingAlert` event via SignalR → Blazor component receives event → calls `IJSRuntime.InvokeAsync` → JavaScript handles Web Notification API and audio playback. This keeps browser APIs out of C# and leverages the existing `MeetingAlertHub`.

**MSAL real**: `MsalTokenAcquisitionService` implements `ITokenAcquisitionService` using `MSAL.Net` with `AcquireTokenInteractive` for browser flow. Replaces mock JWT used in SignalR connection. Same App Registration as Graph with scope `api://<app-id>/MeetingAlerts`.

**Audio autoplay**: Silent audio prime on first user interaction to satisfy browser autoplay policy. Alert sound played via `new Audio("/meeting-alert.mp3").play()`.

**Deduplication**: Handled server-side by `AcknowledgeAlert` (T3). Client sends acknowledge back via SignalR after showing notification.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.UI/wwwroot/meetingAlert.js` | New | Web Notification API + Audio interop functions |
| `src/Aura.UI/wwwroot/meeting-alert.mp3` | New | Alert sound asset |
| `src/Aura.UI/Components/Dashboard/MeetingAlertToast.razor` | New | Toast component receiving SignalR events |
| `src/Aura.UI/Services/MsalTokenAcquisitionService.cs` | New | Real MSAL token acquisition |
| `src/Aura.UI/Program.cs` | Modified | MSAL + SignalR client registration |
| `src/Aura.Api/Hubs/MeetingAlertHub.cs` | No change | Already implemented in T3 |
| `src/Aura.Api/Adapters/SignalRMeetingAlertDispatcher.cs` | No change | Already implemented in T3 |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Browser blocks notification permission silently | Med | Graceful fallback to in-page toast; request permission only on user gesture |
| MSAL interactive flow breaks in headless/CI environments | Med | Keep mock JWT as dev fallback; MSAL config is optional via feature flag |
| Audio autoplay blocked by browser policy | High | Silent audio prime pattern on first click; audio plays only after user gesture |

## Rollback Plan

1. Remove `MeetingAlertToast.razor` from dashboard layout
2. Remove `MsalTokenAcquisitionService` and MSAL DI registration from `Program.cs`
3. Remove `meetingAlert.js` and `meeting-alert.mp3` from `wwwroot`
4. Revert SignalR client connection to mock JWT (if previously changed)
5. No data migration needed — all changes are additive UI/auth wiring

## Dependencies

- T3 backend (MeetingAlertHub, SignalRMeetingAlertDispatcher) — already implemented
- MSAL.Net NuGet package for token acquisition
- Same Entra ID App Registration as Graph API with `MeetingAlerts` scope added
- Browser Notification API (progressive enhancement — not all browsers support equally)

## Success Criteria

- [ ] User receives browser notification with sound when meeting alert fires via SignalR
- [ ] In-page toast appears with acknowledge button even if browser notification is denied
- [ ] MSAL acquires real token for SignalR hub connection (no mock JWT in production path)
- [ ] Notification permission is requested only on user gesture, never on page load
- [ ] Acknowledge action propagates back to server via SignalR

## Proposal Question Round

**Assumptions needing your review:**

1. **Notification sound**: Using a single bundled `.mp3` file. Should we support configurable sounds per alert type (e.g., urgent vs. normal), or is one sound sufficient for v1?

2. **Permission UX**: Requesting notification permission on first dashboard load (via user gesture). Is this acceptable, or should we defer permission request until the first alert actually fires?

3. **Token fallback**: Keeping mock JWT as dev fallback when MSAL config is absent. This means `Program.cs` branches based on config presence. Acceptable for this slice?

4. **Toast persistence**: Toast auto-dismisses after 30 seconds if unacknowledged. Should it persist until manually dismissed, or is auto-dismiss acceptable?

5. **Scope boundary**: The SignalR hub already uses `[Authorize]`. Are we assuming the same Entra ID tenant as Graph, or do we need to handle multi-tenant scenarios?
