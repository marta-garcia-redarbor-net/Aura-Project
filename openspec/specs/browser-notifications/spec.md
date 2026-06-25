# Browser Notifications Specification

## Purpose

Browser-level notification and audio alert system for meeting alerts delivered via SignalR. Provides Web Notification API integration, audio playback, permission management, and in-page toast fallback.

## Requirements

### Requirement: SignalR Client Connection

The system MUST establish a SignalR hub connection from the Blazor UI using an MSAL-acquired access token. The connection MUST target the `MeetingAlertHub` endpoint. When MSAL configuration is absent, the system SHOULD fall back to a mock JWT token for local development.

#### Scenario: SignalR connection with MSAL token

- GIVEN MSAL configuration is present in `Program.cs`
- WHEN the dashboard loads
- THEN a SignalR connection is established to `MeetingAlertHub` using an MSAL access token

#### Scenario: SignalR connection with mock JWT fallback

- GIVEN MSAL configuration is absent
- WHEN the dashboard loads
- THEN a SignalR connection is established using a mock JWT token
- AND a dev-only warning is logged

### Requirement: Meeting Alert Toast

The system MUST provide a `MeetingAlertToast.razor` Blazor component that renders an in-page toast when a `MeetingAlert` event arrives via SignalR. The toast MUST display meeting title, time, and an Acknowledge button. The toast MUST persist until the user manually dismisses it or acknowledges it.

#### Scenario: Toast appears on alert

- GIVEN a SignalR `MeetingAlert` event is received
- WHEN the event payload contains meeting details
- THEN the toast renders with meeting title, time, and Acknowledge button

#### Scenario: Toast persists until dismissed

- GIVEN a toast is displayed
- WHEN the user does not interact with it
- THEN the toast remains visible indefinitely

#### Scenario: Acknowledge dismisses toast

- GIVEN a toast is displayed
- WHEN the user clicks Acknowledge
- THEN the toast is removed from the UI
- AND an acknowledge message is sent back to the server via SignalR

### Requirement: Browser Notification Permission

The system MUST request browser notification permission only on a user gesture (e.g., first dashboard load click). Permission MUST NOT be requested on page load without user interaction. If permission is denied, the system MUST fall back to in-page toast only.

#### Scenario: Permission granted

- GIVEN the user has not yet granted notification permission
- WHEN the user interacts with the dashboard (user gesture)
- THEN the browser permission prompt is shown
- AND subsequent alerts trigger browser notifications

#### Scenario: Permission denied fallback

- GIVEN the user denied notification permission
- WHEN a meeting alert arrives
- THEN only the in-page toast is shown
- AND no browser notification is attempted

### Requirement: Audio Alert Playback

The system MUST play a bundled `.wav` sound file when a meeting alert arrives. Audio MUST only play after a user gesture to satisfy browser autoplay policy. A silent audio prime MUST be triggered on first user interaction.

#### Scenario: Audio plays after user gesture

- GIVEN the user has interacted with the page at least once
- WHEN a meeting alert arrives
- THEN the alert sound plays via `new Audio("/meeting-alert.wav").play()`

#### Scenario: Audio blocked before user gesture

- GIVEN the user has not yet interacted with the page
- WHEN a meeting alert arrives
- THEN no audio is played
- AND the in-page toast still appears

### Requirement: Notification Display via JS Interop

The system MUST invoke JavaScript interop functions in `meetingAlert.js` to display browser notifications and play audio. The JS layer MUST handle Web Notification API calls and audio playback, keeping browser APIs out of C#.

#### Scenario: JS interop shows browser notification

- GIVEN notification permission is granted
- WHEN the Blazor component calls the JS interop notification function
- THEN a browser notification is displayed with meeting title and time

#### Scenario: JS interop handles unsupported browser

- GIVEN the browser does not support the Web Notification API
- WHEN the Blazor component calls the JS interop notification function
- THEN the function returns gracefully without error
- AND the in-page toast serves as the sole notification mechanism
