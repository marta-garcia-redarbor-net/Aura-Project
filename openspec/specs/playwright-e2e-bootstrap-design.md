# Design: playwright-e2e-bootstrap

**Change**: playwright-e2e-bootstrap  
**Phase**: design  
**Date**: 2026-06-24

---

## Architecture Decision

### Design Pattern

Use the **same test infrastructure pattern as existing smoke tests** to minimize new concepts:
- `IClassFixture<WebApplicationFactory<UiMarker>>` for app hosting
- `ConfigureTestServices()` to inject test doubles (stub API clients)
- xUnit `[Fact]` for test cases
- **Add browser automation layer** via Playwright without breaking the existing pattern

### Rationale

1. **Minimal context switch**: Developers already understand the smoke test setup
2. **Unified lifecycle**: Test host startup/shutdown managed by xUnit fixture
3. **Reusable stubs**: Existing `StubDashboardApiClient`, `DelayedDashboardApiClient` etc. can be reused
4. **No parallel concerns**: Browser tests run in same xUnit pipeline, same coverage collection

---

## Technology Choices

| Decision | Option A | Option B | Chosen | Why |
|----------|----------|----------|--------|-----|
| Playwright runner | Microsoft.Playwright NuGet | Playwright CLI external tool | **Microsoft.Playwright** | Native .NET integration, xUnit-friendly |
| Browser type | Chromium only | Multi-browser matrix | **Chromium only (MVP)** | Reduces flakiness; covers 90% of user base; other browsers deferred |
| Page navigation | Launch app in test host, then `page.goto()` | Launch Playwright dev server separately | **Test host + goto** | Reuses existing infrastructure; no external process |
| Failure artifacts | Always capture | Capture on failure only | **Capture on failure only** | Reduces disk/CI overhead; still has full diagnostics when needed |
| Selector strategy | `data-testid` preferred, role-based fallback | CSS/text-based | **`data-testid` first** | Already present; stable; maintainable |
| Wait strategy | Explicit `WaitForSelectorAsync()` | `Implicit` waits + timeouts | **Explicit waits** | More reliable; easier to debug timeouts |

---

## Component Layout

```
tests/Aura.E2E/
├── Aura.E2E.csproj
│   ├── <PackageReference Include="Microsoft.Playwright" Version="1.50.0+" />
│   └── <PackageReference Include="Microsoft.Playwright.NUnit" /> (optional for future)
│
├── Dashboard/
│   └── InitialDashboardSmokeTests.cs  (unchanged - host-level)
│
├── Browser/                            (NEW)
│   └── DashboardRootBrowserTests.cs   (NEW - browser automation)
│
├── Auth/
│   └── DevAccessTokenHandlerTests.cs  (unchanged)
│
├── GraphConnector/
│   └── GraphConnectorStatusSmokeTests.cs (unchanged)
│
└── TestResults/                        (generated - artifacts)
    └── trace-{timestamp}.zip
    └── screenshot-{timestamp}.png
```

---

## Test Fixture Design

### Class Structure

```csharp
public class DashboardRootBrowserTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<UiMarker> _factory;
    private IBrowser _browser;
    private IPage _page;
    private readonly ITestOutputHelper _output;

    public DashboardRootBrowserTests(ITestOutputHelper output)
    {
        _output = output;
        _factory = new WebApplicationFactory<UiMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("AuraApi:BaseUrl", "https://api.aura.test");
            });
    }

    // IAsyncLifetime: async setup/teardown for browser
    public async Task InitializeAsync()
    {
        _browser = await Playwright.CreateAsync().Chromium.LaunchAsync(
            new BrowserLaunchOptions { Headless = true }
        );
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
    }

    [Fact]
    public async Task DashboardRoot_WithControlledData_RenderShellAndLoadingTransition()
    {
        // Implementation in tasks phase
    }
}
```

### Stub/Mock Strategy

Reuse existing test doubles, inject via `ConfigureTestServices()`:

```csharp
private HttpClient CreateClient(IDashboardApiClient dashboardApiClient)
{
    var factory = _factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDashboardApiClient>();
            services.AddScoped(_ => dashboardApiClient);
        });
    });

    return factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("http://localhost")
    });
}
```

---

## Playwright Configuration

### Local Dev Config

Create `playwright.config.ts` (optional, minimal):

```typescript
import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests/Aura.E2E/Browser',
  fullyParallel: false,
  forbidOnly: false,
  retries: 0,
  workers: 1,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:5000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  webServer: {
    command: 'dotnet run --project src/Aura.UI',
    url: 'http://localhost:5000',
    reuseExistingServer: false,
  },
});
```

**Note**: For .NET xUnit integration, most of this is handled in C# code, not config file. Minimal config file is optional; can be omitted if all config is in code.

---

## State Transition Validation

### Sequence Diagram

```
Browser            App (via stub)
  |                    |
  +--GET /-----------> |
  |                    |
  | <--200 HTML------- | (immediate, shell only)
  |
  +--Wait for shell render
  |                    
  | <--loading marker appears
  |
  +--Wait for API response (delayed)
  |                    | (DelayedDashboardApiClient: 100-200ms)
  | <--API response----| 
  |
  | <--loading marker disappears
  | <--populated/empty marker appears
  |
  +--Assert transition complete
  |
```

---

## Failure Artifact Strategy

On test failure, capture:

1. **Screenshot**: Current state of page
2. **Trace**: Full Playwright trace (DOM, network, console)

Location: `tests/Aura.E2E/TestResults/`

Example:

```csharp
if (testFailed)
{
    await _page.ScreenshotAsync(new PageScreenshotOptions
    {
        Path = $"TestResults/failure-screenshot-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png"
    });
    
    // Trace already recorded via Playwright context
}
```

---

## Reusable Components

### Existing Code to Leverage

| Component | File | Usage |
|-----------|------|-------|
| `DelayedDashboardApiClient` | smoke tests | Inject delay for transition testing |
| `StubDashboardApiClient` | smoke tests | Stub response data |
| `InitialDashboardResponse` DTO | smoke tests | Sample payloads |
| `DashboardCardResponse` DTO | smoke tests | Card structure |
| `WebApplicationFactory<UiMarker>` | xUnit | App hosting |
| `data-testid` markers | Aura.UI components | Selectors |

**No new infrastructure to build; reuse existing patterns.**

---

## Implementation Phases (for Tasks)

1. **Phase 1**: Add Playwright NuGet, create Browser folder, scaffold test class
2. **Phase 2**: Wire test fixture (factory + stubs + browser lifecycle)
3. **Phase 3**: Implement test logic (navigation, waits, assertions)
4. **Phase 4**: Add failure artifact capture
5. **Phase 5**: Docs + verify all tests pass

---

## Risk Mitigation

| Risk | Mitigation | Owner |
|---|---|---|
| Browser timeout/flake | Use explicit waits, short delays (100-200ms), no random sleeps | Dev |
| Selector brittle/changes | Use stable `data-testid` only; review selectors in design review | Dev |
| Test env config wrong | Document local run procedure; validate against mock before CI | Dev |
| Playwright version conflict | Pin Microsoft.Playwright version in .csproj; test locally first | Dev |
| CI not ready | This slice is LOCAL ONLY; no CI changes in scope | Design |

---

## Next: Implementation Tasks

Tasks phase will break this design into atomic, reviewable work units with clear DoD.

Key deliverables:
- ✅ Playwright installed in Aura.E2E.csproj
- ✅ `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs` complete
- ✅ Test passes locally with controlled data
- ✅ Failure artifacts captured
- ✅ README updated with browser test instructions
- ✅ All existing smoke tests still passing
