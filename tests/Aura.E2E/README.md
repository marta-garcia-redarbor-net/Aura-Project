# Aura.E2E — End-to-End Test Suite

This project contains two categories of end-to-end tests for Aura.UI:

## Test Categories

### Host-Level Smoke Tests (`Dashboard/`, `Auth/`, `GraphConnector/`)

Fast HTTP-only tests using `WebApplicationFactory<UiMarker>` with an in-memory
TestServer. They validate rendered HTML markers and state without a real browser.

```bash
# Run smoke tests only (excludes browser tests)
dotnet test tests/Aura.E2E --filter "FullyQualifiedName~Dashboard|FullyQualifiedName~Auth|FullyQualifiedName~GraphConnector"
```

### Browser Tests (`Browser/`)

Playwright-based tests that launch a real Chromium browser against a real Kestrel
server. They validate actual DOM rendering, Blazor hydration, and state transitions.

```bash
# Run browser tests only
dotnet test tests/Aura.E2E --filter "FullyQualifiedName~Browser"
```

## Prerequisites

### Playwright Browser Installation

Before running browser tests for the first time, install Chromium:

```bash
pwsh tests/Aura.E2E/bin/Debug/net9.0/playwright.ps1 install chromium
```

### No Port Conflicts

Browser tests use port 0 (OS-assigned) for Kestrel. The actual port is discovered
automatically after startup, so there are no hardcoded port conflicts even when
running tests consecutively.

## Running All Tests

```bash
dotnet test tests/Aura.E2E
```

## Failure Artifacts

When a browser test fails, the following artifacts are captured to `TestResults/`:

- **Screenshot**: `failure-{timestamp}.png` — page state at failure
- **Trace**: `trace-{timestamp}.zip` — full Playwright trace (open with `npx playwright show-trace`)

## Architecture Notes

- Browser tests use a standalone `WebApplication` host (not `WebApplicationFactory`)
  because .NET 9's minimal hosting forces TestServer as the transport
- All external API clients are stubbed with deterministic responses
- Tests run headless Chromium by default
- Port is dynamically assigned (port 0) to avoid conflicts on consecutive runs
- Browser tests are local-only — not yet configured for CI
