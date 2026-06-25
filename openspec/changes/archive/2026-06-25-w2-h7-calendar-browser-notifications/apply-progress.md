# Apply Progress: Calendar Browser Notifications (T4)

## TDD Cycle Evidence

| Task | RED | GREEN | REFACTOR | Status |
|------|-----|-------|----------|--------|
| 1.1 Architecture test for ITokenAcquisitionService | ✅ Written in TokenAcquisitionArchitectureTests.cs | ✅ Interface created in Aura.UI.Services | — | PASS |
| 1.3 MsalTokenAcquisitionService unit test | ✅ Written in MsalTokenAcquisitionServiceTests.cs | ✅ Service created with IPublicClientApplication | — | PASS |
| 1.5 DevTokenAcquisitionService unit test | ✅ Written in DevTokenAcquisitionServiceTests.cs | ✅ Service created with mock JWT fallback | — | PASS |
| 2.1 meetingAlert.js | ✅ N/A (JS file, no test required per plan) | ✅ Created with showNotification, playSound, primeAudio | — | PASS |
| 2.2 Script tag in App.razor | ✅ N/A (markup change) | ✅ Added before blazor.web.js | — | PASS |
| 3.1 MeetingAlertToast render test | ✅ Written in MeetingAlertToastTests.cs | ✅ Component renders toast on event | — | PASS |
| 3.2 MeetingAlertToast component | ✅ Tests written first | ✅ Component created with SignalR, toast, JS interop | — | PASS |
| 3.5 Acknowledge behavioral test | ✅ MeetingAlertToastTests.cs: `ShouldDismissAlert_WhenAcknowledgeClicked` — verifies clicking acknowledge clears toast (UI behavior for `HubConnection.InvokeAsync("AcknowledgeAlert", id)`) | ✅ Code exists at MeetingAlertToast.razor:98 | — | PASS |
| 4.1 Program.cs registration | ✅ N/A (wiring change) | ✅ ITokenAcquisitionService registered conditionally | — | PASS |
| 4.2 SignalR integration test | ✅ SignalRMeetingAlertIntegrationTests.cs: `ShouldAcquireToken_DuringInitialization` (verifies token service called), `ShouldHandleConnectionFailure_Gracefully` (verifies component renders despite missing hub) | ✅ Hub wiring exists in MeetingAlertToast.razor:40-46 | — | PASS |

## Completed Tasks

- [x] 1.1 RED: Architecture test for ITokenAcquisitionService namespace
- [x] 1.2 GREEN: ITokenAcquisitionService interface
- [x] 1.3 RED: MsalTokenAcquisitionService unit test
- [x] 1.4 GREEN: MsalTokenAcquisitionService implementation
- [x] 1.5 RED: DevTokenAcquisitionService unit test
- [x] 1.6 GREEN: DevTokenAcquisitionService implementation
- [x] 1.7 Microsoft.Identity.Client NuGet added
- [x] 1.8 REFACTOR: Shared test fixtures
- [x] 2.1 meetingAlert.js created
- [x] 2.2 Script tag added to App.razor
- [x] 3.1 RED: MeetingAlertToast render test
- [x] 3.2 GREEN: MeetingAlertToast component
- [x] 3.3 SignalR Client NuGet added
- [x] 3.4 MeetingAlertToast added to MainLayout
- [x] 3.5 RED: Acknowledge behavioral test (bUnit: verifies UI dismiss on acknowledge click)
- [x] 3.6 GREEN: HubConnection.InvokeAsync("AcknowledgeAlert", alertId) wired
- [x] 4.1 GREEN: Program.cs registration (MSAL or dev fallback)
- [x] 4.2 RED: Integration test (token acquisition verified, graceful failure verified)
- [x] 4.3 GREEN: Hub wiring in component OnInitializedAsync (WithUrl + WithAccessToken)

## Files Changed in This Batch

| File | Action | What Was Done |
|------|--------|---------------|
| `tests/Aura.UnitTests/UI/MeetingAlertToastTests.cs` | Modified | Added `ShouldDismissAlert_WhenAcknowledgeClicked` behavioral test |
| `tests/Aura.UnitTests/UI/SignalRMeetingAlertIntegrationTests.cs` | Created | Integration tests: token acquisition, graceful connection failure |
| `openspec/changes/w2-h7-calendar-browser-notifications/design.md` | Modified | IConfidentialClientApplication → IPublicClientApplication, .mp3 → .wav, hub wiring location |
| `openspec/changes/w2-h7-calendar-browser-notifications/tasks.md` | Modified | Marked tasks 3.5, 3.6, 4.2, 4.3 as done |

## Test Results After Fixes

```
Aura.UnitTests: 488 passed, 0 failed (was 485 → +3 new tests)
Aura.ArchitectureTests: 45 passed, 0 failed
Total: 533 passed, 0 failed
```

## Design Doc Corrections

1. **IConfidentialClientApplication → IPublicClientApplication**: Fixed in File Changes table (MsalTokenAcquisitionService) and Interfaces/Contracts code sample. Rationale line about GraphClientFactory left as-is (different service).
2. **.mp3 → .wav**: Fixed in Decision heading, Option table, Rationale, Data Flow diagram, File Changes table, JS interop contract, and Open Questions.
3. **Hub wiring location**: Updated Program.cs description (removed "inject IJSRuntime into hub connection"), merged MeetingAlertToast.razor entries to note hub connection is wired in component `OnInitializedAsync`.
