# Tasks: Calendar Browser Notifications (T4)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~420 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

## Phase 1: Token Service Interface & Implementations

- [x] 1.1 RED: Write architecture test verifying `ITokenAcquisitionService` exists in `Aura.UI.Services` namespace, not in Infrastructure
- [x] 1.2 GREEN: Create `src/Aura.UI/Services/ITokenAcquisitionService.cs` — interface with `Task<string> AcquireTokenAsync(CancellationToken)`
- [x] 1.3 RED: Write unit test for `MsalTokenAcquisitionService` — mock `IConfidentialClientApplication`, verify `AcquireTokenInteractive` called with `MeetingAlerts` scope
- [x] 1.4 GREEN: Create `src/Aura.UI/Services/MsalTokenAcquisitionService.cs` — MSAL implementation using same App Registration as Graph
- [x] 1.5 RED: Write unit test for `DevTokenAcquisitionService` — verify returns non-null token, logs dev-fallback warning
- [x] 1.6 GREEN: Create `src/Aura.UI/Services/DevTokenAcquisitionService.cs` — mock JWT fallback delegating to `DevAccessTokenHandler` pattern
- [x] 1.7 Add `Microsoft.Identity.Client` NuGet to `src/Aura.UI/Aura.UI.csproj`
- [x] 1.8 REFACTOR: Extract shared test fixtures for mocked `IConfidentialClientApplication`

## Phase 2: JS Interop Layer

- [x] 2.1 Create `src/Aura.UI/wwwroot/meetingAlert.js` — `window.meetingAlert` object with `showNotification(title, body)`, `playSound()`, `primeAudio()`; guard Notification API unsupported browsers gracefully
- [x] 2.2 Add `<script src="meetingAlert.js"></script>` to `src/Aura.UI/Components/App.razor` before `blazor.web.js`

## Phase 3: Toast Component

- [x] 3.1 RED: Write unit test for `MeetingAlertToast` — verify renders meeting title, time, and Acknowledge button on `MeetingAlert` event
- [x] 3.2 GREEN: Create `src/Aura.UI/Components/Dashboard/MeetingAlertToast.razor` — SignalR `HubConnection.On("MeetingAlert")`, in-page toast markup, `IJSRuntime.InvokeAsync` for notification + audio, `AcknowledgeAlert` invoke on button click, toast persists until manual dismiss
- [x] 3.3 Add `Microsoft.AspNetCore.SignalR.Client` NuGet to `src/Aura.UI/Aura.UI.csproj`
- [x] 3.4 Add `<MeetingAlertToast />` to `src/Aura.UI/Components/Layout/MainLayout.razor` below `@Body`, inside `dashboard-shell__main`
- [x] 3.5 RED: Write test verifying acknowledge sends `AcknowledgeAlert` back to server via SignalR
- [x] 3.6 GREEN: Wire `HubConnection.InvokeAsync("AcknowledgeAlert", alertId)` in toast component

## Phase 4: Integration Wiring

- [x] 4.1 Modify `src/Aura.UI/Program.cs` — register `ITokenAcquisitionService` as `MsalTokenAcquisitionService` when MSAL config present, else `DevTokenAcquisitionService`; register SignalR `HubConnectionBuilder` service
- [x] 4.2 RED: Write integration test verifying SignalR hub connection established with token from `ITokenAcquisitionService`
- [x] 4.3 GREEN: Wire `HubConnection` with `WithUrl("/hubs/meeting-alerts")` and `WithAccessToken` from token service in `Program.cs`

## Phase 5: Testing & Verification

- [x] 5.1 Run `dotnet test Aura.sln` — verify all unit and architecture tests pass
- [x] 5.2 Run `dotnet build Aura.sln` — verify zero errors, zero new warnings
- [ ] 5.3 Manual verification: load dashboard, confirm toast renders on SignalR `MeetingAlert` event, confirm browser notification fires (if permission granted), confirm `meeting-alert.wav` plays
