# Design: Calendar Browser Notifications (T4)

## Technical Approach

Hybrid SignalR + JS Interop: the Blazor UI establishes a SignalR connection to `MeetingAlertHub` using an MSAL-acquired access token. When a `MeetingAlert` event arrives, the Blazor component calls `IJSRuntime.InvokeAsync` to delegate browser Notification API and audio playback to `meetingAlert.js`. The in-page toast always renders as fallback. MSAL token acquisition uses `AcquireTokenInteractive` for the same Entra ID App Registration as Graph, with a mock JWT fallback for local dev when MSAL config is absent.

## Architecture Decisions

### Decision: JS Interop for browser APIs

| Option | Tradeoff | Decision |
|--------|----------|----------|
| C# Blazor component calls Notification API via IJSRuntime | Clean separation; browser APIs stay in JS; C# handles only state | **Chosen** |
| C# direct interop (e.g. WebNotifications package) | Adds dependency; limits control over permission flow | Rejected |
| Service Worker push notifications | Requires PWA setup; out of scope for v1 | Rejected |

**Rationale**: The existing codebase already uses JS interop for static files (`blazor.web.js`). Keeping browser APIs in JS avoids coupling C# to browser quirks and follows the project's adapter pattern — JS is the adapter for browser capabilities.

### Decision: MSAL interactive flow for SignalR token

| Option | Tradeoff | Decision |
|--------|----------|----------|
| `AcquireTokenInteractive` (popup/redirect) | Real browser auth; matches Graph pattern; requires Entra config | **Chosen** |
| Client credentials flow | No user context; hub `[Authorize]` needs user identity | Rejected |
| Keep mock JWT always | No real auth; unacceptable for production | Rejected |

**Rationale**: `GraphClientFactory` already uses `IConfidentialClientApplication` with MSAL.Net. `MsalTokenAcquisitionService` reuses the same App Registration and adds `api://<app-id>/MeetingAlerts` scope. Dev fallback mirrors `DevAccessTokenHandler` pattern — conditional registration in `Program.cs` when config is absent.

### Decision: Toast placement in MainLayout

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Render inside `MainLayout.razor` below `@Body` | Always visible regardless of route; simple | **Chosen** |
| Render inside each dashboard page | Repeated code; breaks if user navigates | Rejected |
| Portal via JS overlay | Overcomplicates Blazor rendering | Rejected |

**Rationale**: `MainLayout` already wraps all routes with `DashboardViewState`. A single `MeetingAlertToast` instance at the layout level receives SignalR events globally without coupling to individual pages.

### Decision: Single bundled .wav, no configurability

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Single bundled .wav in wwwroot | Simple; one sound for all alerts; no config surface | **Chosen** |
| Configurable sounds per alert type | Adds UI/config complexity; deferred to v2 | Rejected |
| Multiple built-in sounds with enum selector | Over-engineered for v1 | Rejected |

**Rationale**: Proposal explicitly scoped this out. The `.wav` file is a static asset served from `wwwroot/`. Audio playback is `new Audio("/meeting-alert.wav").play()` — trivial to extend later.

### Decision: Permission request timing

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Request on first user gesture (dashboard load click) | Respects browser autoplay policy; prompt appears early | **Chosen** |
| Request on first alert arrival | Delayed prompt; user may miss first alert | Rejected |
| Request on explicit settings toggle | Requires additional UI; delays value delivery | Rejected |

**Rationale**: Browsers require a user gesture for notification permission. Requesting on dashboard load click (when the component mounts interactively) is the earliest acceptable moment. Fallback to in-page toast is always available.

## Data Flow

    ┌─────────────┐    SignalR "MeetingAlert"     ┌──────────────────┐
    │  Aura.Api    │ ──────────────────────────────▶│ MeetingAlertHub  │
    │  (Server)    │                               │  (group: userId) │
    └─────────────┘                               └────────┬─────────┘
                                                           │
                                                    SignalR client
                                                           │
    ┌──────────────────────────────────────────────────────▼─────────┐
    │  Aura.UI (Blazor Server)                                      │
    │                                                                │
    │  MeetingAlertToast.razor                                       │
    │    ├─ receives event via HubConnection.On("MeetingAlert", …)   │
    │    ├─ renders in-page toast (always)                           │
    │    ├─ IJSRuntime.InvokeAsync("meetingAlert.showNotification")  │
    │    │     └─ meetingAlert.js → Web Notification API             │
    │    └─ IJSRuntime.InvokeAsync("meetingAlert.playSound")        │
    │          └─ meetingAlert.js → new Audio("/meeting-alert.wav")  │
    │                                                                │
    │  Acknowledge → HubConnection.InvokeAsync("AcknowledgeAlert")   │
    └────────────────────────────────────────────────────────────────┘

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.UI/Services/ITokenAcquisitionService.cs` | Create | Interface: `Task<string> AcquireTokenAsync(CancellationToken)` |
| `src/Aura.UI/Services/MsalTokenAcquisitionService.cs` | Create | MSAL implementation using `IPublicClientApplication.AcquireTokenInteractive` |
| `src/Aura.UI/Services/DevTokenAcquisitionService.cs` | Create | Mock JWT fallback implementing `ITokenAcquisitionService` (delegates to existing `DevAccessTokenHandler` logic) |
| `src/Aura.UI/wwwroot/meetingAlert.js` | Create | JS interop: `showNotification(title, body)`, `playSound()`, `primeAudio()` |
| `src/Aura.UI/wwwroot/meeting-alert.wav` | Create | Alert sound asset |
| `src/Aura.UI/Components/Dashboard/MeetingAlertToast.razor` | Create | Blazor component: SignalR connection (wired in `OnInitializedAsync` with WithUrl + WithAccessToken from token service), toast rendering, JS interop calls |
| `src/Aura.UI/Program.cs` | Modify | Register `ITokenAcquisitionService` (MSAL or dev fallback), register SignalR client service |
| `src/Aura.UI/Components/App.razor` | Modify | Add `<script src="meetingAlert.js">` before `blazor.web.js` |
| `src/Aura.UI/Components/Layout/MainLayout.razor` | Modify | Add `<MeetingAlertToast />` below `@Body` |

## Interfaces / Contracts

```csharp
// New port — domain capability, not provider brand
public interface ITokenAcquisitionService
{
    Task<string> AcquireTokenAsync(CancellationToken cancellationToken = default);
}

// MSAL implementation — uses same App Registration as Graph
internal sealed class MsalTokenAcquisitionService : ITokenAcquisitionService
{
    private readonly IPublicClientApplication _msalApp;
    private readonly string[] _scopes = ["api://<app-id>/MeetingAlerts"];

    public async Task<string> AcquireTokenAsync(CancellationToken ct)
    {
        var accounts = await _msalApp.GetAccountsAsync();
        var result = await _msalApp.AcquireTokenInteractive(_scopes, accounts.FirstOrDefault())
            .ExecuteAsync(ct);
        return result.AccessToken;
    }
}

// JS interop contract (meetingAlert.js)
window.meetingAlert = {
    showNotification: (title, body) => { /* Web Notification API */ },
    playSound: () => { /* new Audio("/meeting-alert.wav").play() */ },
    primeAudio: () => { /* silent Audio element to unlock autoplay */ }
};
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `MsalTokenAcquisitionService` returns token; falls back to dev when config absent | Mock `IConfidentialClientApplication`; verify scope and flow |
| Unit | `MeetingAlertToast` renders toast on event; sends Acknowledge via SignalR | Mock `HubConnection`; verify DOM state via bUnit |
| Unit | `meetingAlert.js` functions (notification, audio, permission check) | Jest/JS test or manual browser verification |
| Integration | SignalR hub connection established with MSAL token | Integration test with `WebApplicationFactory` + test hub |
| Integration | Full flow: server dispatches alert → UI shows toast → JS shows notification | End-to-end SignalR test with mocked JS runtime |
| Architecture | `ITokenAcquisitionService` lives in Aura.UI (not Infrastructure) | NetArchTest rule: no dependency from UI → Infrastructure for auth |

## Migration / Rollout

No data migration required. Changes are additive UI/auth wiring. Rollback is removing files and reverting `Program.cs` registrations. MSAL config is optional — absent config triggers dev fallback automatically.

## Open Questions

- [ ] Entra ID App Registration: exact ClientId and TenantId values for MSAL config — needed before `MsalTokenAcquisitionService` can be tested against real Entra
- [ ] Meeting-alert.wav source: need a royalty-free alert sound file to bundle in wwwroot
