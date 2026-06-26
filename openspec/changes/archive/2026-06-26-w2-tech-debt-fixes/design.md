# Design: Week 2 Tech Debt Fixes

## Technical Approach

Four-phase incremental fix addressing 5 MUST-FIX blockers and 2 SHOULD-FIX items. Each phase is independently deployable and testable. The critical path is Phase 2 (worker fixes) since it unblocks the Morning Summary end-to-end flow.

**Phase 1 — Quick Wins**: Delete 4 placeholder files, deduplicate E2E csproj, sanitize `.env`. Pure subtractive changes with zero runtime impact.

**Phase 2 — Worker Fixes**: Restructure `ConnectorExecutionWorker` from one-shot to continuous polling (matching the existing `MorningSummarySchedulingWorker` pattern). Wire `IMorningSummaryComposer` into `MorningSummarySchedulingWorker` with isolated error handling.

**Phase 3 — NuGet Resolution**: Downgrade v10.x packages to .NET 9-compatible v9.x equivalents. Verify with full restore + build + test.

**Phase 4 — Quality**: Enable `TreatWarningsAsErrors`, fix surfaced warnings, add auth integration tests (extend existing `AuthorizationFlowTests`).

## Architecture Decisions

### Decision: Continuous polling pattern for ConnectorExecutionWorker

**Choice**: Replicate the `while (!stoppingToken.IsCancellationRequested)` + `Task.Delay` loop from `MorningSummarySchedulingWorker`.

**Alternatives considered**: Timer-based (`System.Threading.Timer`), Hangfire scheduler, Quartz.NET.

**Rationale**: The existing worker already uses this pattern. Consistency across workers reduces cognitive load. Timer-based approaches add complexity without benefit for a simple poll loop. External schedulers are over-engineered for this scope.

### Decision: Remove IHostApplicationLifetime dependency

**Choice**: Delete the `_lifetime` field and constructor parameter entirely. The host lifecycle is managed by `IHostApplicationLifetime` at the runtime level — no worker should call `StopApplication()`.

**Alternatives considered**: Keep lifetime but guard the call, use `IHostedService.StopAsync` signaling.

**Rationale**: A worker killing the host after one cycle is a design bug. The host shutdown should only be triggered by Ctrl+C / SIGTERM. Removing the dependency makes the contract explicit and eliminates the class of bugs where one worker kills all others.

### Decision: Fresh DI scope per iteration (not per-adapter)

**Choice**: Create one `IServiceScope` per polling iteration. Resolve adapters from that scope. Dispose scope in `finally`.

**Alternatives considered**: Scope per adapter, singleton scope, root scope.

**Rationale**: Per-adapter scopes add unnecessary complexity since adapters are lightweight. A root scope risks capturing stale state across iterations. Per-iteration scope matches the existing pattern in `MorningSummarySchedulingWorker` and ensures fresh `IConnectorAdapter` instances (registered as `AddScoped`).

### Decision: Composer error isolation via try/catch in ProcessIterationAsync

**Choice**: Wrap `ComposeAsync()` in its own try/catch within `ProcessIterationAsync`, separate from the outer catch that handles iteration failures.

**Alternatives considered**: Let exceptions propagate to outer catch, use a decorator pattern.

**Rationale**: Composition failure is a recoverable error — the emission is already marked. A separate catch ensures the log message is specific (composition vs scheduling failure) and the worker loop continues. The outer catch handles truly unexpected errors.

### Decision: Configurable polling interval via IConfiguration

**Choice**: Read `ConnectorExecution:PollingIntervalSeconds` from `IConfiguration` with a 5-minute default. Bind via `IOptions<ConnectorExecutionOptions>` pattern.

**Alternatives considered**: Hardcoded constant, environment variable direct read.

**Rationale**: Configuration binding is the established .NET pattern. `IOptions<T>` provides type safety and testability. The 5-minute default balances freshness with resource usage.

## Data Flow

### ConnectorExecutionWorker — Continuous Mode

```
while (!stoppingToken.IsCancellationRequested)
│
├─ Create scope ─────────────────────────────┐
│  ├─ Resolve ExecuteConnectorUseCase        │
│  ├─ Resolve IConnectorAdapter[]            │
│  └─ Resolve IPublicClientApplication       │
│                                            │
├─ Resolve user OID from MSAL cache          │
│  └─ (log warning if no cached account)     │
│                                            │
├─ foreach adapter                           │
│  ├─ Build CheckpointIdentity               │
│  ├─ useCase.ExecuteAsync(identity, ct)     │
│  └─ Log success/failure per adapter        │
│                                            │
├─ finally: dispose scope ◄──────────────────┘
│
├─ Task.Delay(PollingInterval, stoppingToken)
│  └─ Cancellation breaks immediately
│
└─ Next iteration
```

### MorningSummarySchedulingWorker — Updated Flow

```
while (!stoppingToken.IsCancellationRequested)
│
├─ ProcessIterationAsync(ct)
│  ├─ scheduler.ResolveAsync("system", ct)
│  ├─ if !due → return
│  ├─ emissionStore.MarkEmittedAsync("system", localDate, ct)
│  ├─ Build MorningSummaryWindow from due state
│  ├─ Build MorningSummaryRequest(userId, window)
│  └─ try: composer.ComposeAsync(request, ct)
│     └─ catch: log error, continue
│
├─ Task.Delay(PollingInterval, stoppingToken)
└─ Next iteration
```

**Key**: The `MorningSummaryWindow` is reconstructed from `MorningSummaryDueState` fields: `LocalDate` → `WindowDate`, `ResolvedTimezoneId` → `UserTimeZoneId`, `TargetLocalTime` → `ScheduledLocalTime`. `ScheduledInstantUtc` is computed by converting the local time to UTC via `TimeZoneInfo`.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Modify | Add polling loop, remove `_lifetime` dependency, add `IOptions<ConnectorExecutionOptions>`, scope per iteration, configurable interval |
| `src/Aura.Workers/ConnectorExecutionOptions.cs` | Create | Options class with `PollingIntervalSeconds` property (default 300) |
| `src/Aura.Workers/MorningSummarySchedulingWorker.cs` | Modify | Add `IMorningSummaryComposer` constructor param, call `ComposeAsync()` after `MarkEmittedAsync()`, add composition logging |
| `src/Aura.Workers/Program.cs` | Modify | Add `Configure<ConnectorExecutionOptions>(config.GetSection("ConnectorExecution"))` binding |
| `src/Aura.Workers/appsettings.json` | Modify | Add `ConnectorExecution` section with `PollingIntervalSeconds` |
| `tests/Aura.E2E/Aura.E2E.csproj` | Modify | Remove duplicate `Microsoft.Playwright` v1.52.0 reference (line 17) |
| `tests/Aura.UnitTests/UnitTest1.cs` | Delete | Placeholder test file |
| `tests/Aura.IntegrationTests/UnitTest1.cs` | Delete | Placeholder test file |
| `tests/Aura.ArchitectureTests/UnitTest1.cs` | Delete | Placeholder test file |
| `tests/Aura.E2E/UnitTest1.cs` | Delete | Placeholder test file |
| `.env` | Modify | Replace real Azure AD GUIDs with `YOUR_CLIENT_ID` / `YOUR_TENANT_ID` placeholders |
| `Directory.Build.props` | Modify | Set `TreatWarningsAsErrors` to `true` |
| `src/Aura.Infrastructure/Aura.Infrastructure.csproj` | Modify | Downgrade v10.x NuGet packages to v9.x equivalents |
| `tests/Aura.UnitTests/Aura.UnitTests.csproj` | Modify | Downgrade `Microsoft.Extensions.Diagnostics.HealthChecks` and `Microsoft.Extensions.DependencyInjection` to v9.x |
| `tests/Aura.UnitTests/Workers/ConnectorExecutionWorkerTests.cs` | Modify | Update tests: remove `IHostApplicationLifetime` mock, add continuous polling tests (multi-iteration, cancellation, adapter failure isolation) |
| `tests/Aura.UnitTests/Workers/MorningSummarySchedulingWorkerTests.cs` | Modify | Add composer wiring tests (called when due, not called when not due, failure logged) |
| `tests/Aura.IntegrationTests/Auth/AuthorizationFlowTests.cs` | Verify | Existing tests already cover 401/200/invalid-token scenarios — confirm they pass after all changes |

## Interfaces / Contracts

### ConnectorExecutionOptions

```csharp
namespace Aura.Workers;

public sealed class ConnectorExecutionOptions
{
    public int PollingIntervalSeconds { get; set; } = 300;
}
```

### Updated ConnectorExecutionWorker constructor

```csharp
public ConnectorExecutionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ConnectorExecutionOptions> options,
    ILogger<ConnectorExecutionWorker> logger)
```

Note: `IHostApplicationLifetime` is **removed** from the constructor.

### Updated MorningSummarySchedulingWorker constructor

```csharp
public MorningSummarySchedulingWorker(
    IMorningSummaryScheduler scheduler,
    IMorningSummaryEmissionStore emissionStore,
    IMorningSummaryComposer composer,
    ILogger<MorningSummarySchedulingWorker> logger)
```

### DI Registration (Program.cs addition)

```csharp
builder.Services.Configure<ConnectorExecutionOptions>(
    builder.Configuration.GetSection("ConnectorExecution"));
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit — ConnectorExecutionWorker | Multi-iteration loop runs, cancellation stops cleanly, adapter failure doesn't kill loop, scope disposed per iteration, no `_lifetime.StopApplication()` call | NSubstitute mocks for `IServiceScopeFactory`, `IPublicClientApplication`. Use `CancellationTokenSource` to trigger cancellation. Verify scope created N times for N iterations. |
| Unit — MorningSummarySchedulingWorker | Composer called when due, composer not called when not due, composition failure logged but doesn't break loop | NSubstitute mock for `IMorningSummaryComposer`. Verify `ComposeAsync` call count and argument. Inject throwing composer to test error path. |
| Unit — ConnectorExecutionOptions | Default value is 300 seconds | Simple Assert on `new ConnectorExecutionOptions().PollingIntervalSeconds` |
| Integration — Auth flow | 401 without token, 200 with mock token, 401 with invalid token | Existing `AuthorizationFlowTests` — verify they still pass. No new test files needed. |
| Integration — Worker DI composition | `ConnectorExecutionWorker` resolves with new constructor signature | Extend `WorkersHostCompositionTests` to resolve `ConnectorExecutionWorker` from container |
| Build quality | Zero warnings with `TreatWarningsAsErrors=true` | `dotnet build Aura.sln` |
| NuGet | No version conflicts after downgrade | `dotnet restore Aura.sln` + `dotnet build Aura.sln` + `dotnet test Aura.sln` |

### Existing test changes

The 4 existing `ConnectorExecutionWorkerTests` must be updated:
- Remove all `IHostApplicationLifetime` mock setup and `lifetime.Received(1).StopApplication()` assertions
- Constructor calls change from `(scopeFactory, lifetime, logger)` to `(scopeFactory, options, logger)`
- `ExecuteAsync_OneShot_ExecutesUseCaseAndStopsApplication` → renamed to `ExecuteAsync_Continuous_PollsAndStaysRunning`
- Add new test: `ExecuteAsync_Continuous_RunsMultipleIterations`
- Add new test: `ExecuteAsync_Continuous_AdapterFailureDoesNotStopLoop`
- Add new test: `ExecuteAsync_Continuous_CancellationStopsGracefully`

The 2 existing `MorningSummarySchedulingWorkerTests` must be updated:
- Constructor calls add `composer` parameter (NSubstitute mock)
- `ProcessIterationAsync_WhenDue_MarksEmissionWithFixedSystemUser` → also verify `composer.ComposeAsync` called
- Add new test: `ProcessIterationAsync_WhenDue_ComposerFailureLoggedNotThrown`
- Add new test: `ProcessIterationAsync_WhenNotDue_ComposerNotCalled`

## Migration / Rollback

**Breaking changes**:
- `ConnectorExecutionWorker` constructor signature changes (removes `IHostApplicationLifetime`, adds `IOptions<ConnectorExecutionOptions>`). Any manual DI registration must be updated. The existing `Program.cs` registration is the only consumer.
- `MorningSummarySchedulingWorker` constructor adds `IMorningSummaryComposer`. Already registered in `AddAuraApplication()` as scoped.

**Configuration migration**:
- New config section `ConnectorExecution:PollingIntervalSeconds` in `appsettings.json`. Default of 300s applies if absent — no migration required for existing deployments.

**Rollback approach per phase**:
- **Phase 1**: `git checkout -- tests/ .env` restores deleted files and credentials
- **Phase 2**: `git checkout -- src/Aura.Workers/` reverts worker changes. Composer remains independently tested.
- **Phase 3**: `git checkout -- src/Aura.Infrastructure/Aura.Infrastructure.csproj tests/Aura.UnitTests/Aura.UnitTests.csproj` reverts NuGet changes
- **Phase 4**: `git checkout -- Directory.Build.props` disables `TreatWarningsAsErrors`; remove new test files

## Open Questions

None — all technical decisions are resolved by existing codebase patterns and spec requirements.
