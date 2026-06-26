# Tasks: Week 2 Tech Debt Fixes

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 280–350 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr |
| Chain strategy | size-exception |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | All 4 phases in one PR | PR 1 | Pure deletes + small modifications + new options class + test updates. Under budget. |

## Phase 1: Quick Wins

- [ ] 1.1 **RED**: Write a compilation test in `ArchitectureTests` asserting `Aura.E2E.csproj` has exactly 0 duplicate `PackageReference` for `Microsoft.Playwright` (NetArchTest-style or raw XML parse)
- [ ] 1.2 **GREEN**: Remove line 17 (`Microsoft.Playwright` v1.52.0) from `tests/Aura.E2E/Aura.E2E.csproj`, keeping line 13 (v1.54.0)
- [ ] 1.3 Delete `tests/Aura.UnitTests/UnitTest1.cs`
- [ ] 1.4 Delete `tests/Aura.IntegrationTests/UnitTest1.cs`
- [ ] 1.5 Delete `tests/Aura.ArchitectureTests/UnitTest1.cs`
- [ ] 1.6 Delete `tests/Aura.E2E/UnitTest1.cs`
- [ ] 1.7 Replace real Azure AD GUIDs in `.env` lines 10–11 with `YOUR_CLIENT_ID` and `YOUR_TENANT_ID`
- [ ] 1.8 Verify `.gitignore` contains `.env` entry

## Phase 2: Worker Fixes

### Fix 2a — ConnectorExecutionWorker

- [ ] 2.1 **RED**: Write test `ConnectorExecutionOptionsTests.DefaultPollingInterval_Is300Seconds` asserting `new ConnectorExecutionOptions().PollingIntervalSeconds == 300`
- [ ] 2.2 **GREEN**: Create `src/Aura.Workers/ConnectorExecutionOptions.cs` with `PollingIntervalSeconds` property (default 300)
- [ ] 2.3 **RED**: Write test `ConnectorExecutionWorkerTests.Constructor_DoesNotAcceptIHostApplicationLifetime` — compile-time check that constructor `(IServiceScopeFactory, ILogger)` compiles without lifetime
- [ ] 2.4 **GREEN**: Modify `src/Aura.Workers/ConnectorExecutionWorker.cs`: remove `_lifetime` field and constructor param; add `IOptions<ConnectorExecutionOptions>` param; add `while (!stoppingToken.IsCancellationRequested)` loop; create fresh scope per iteration in `using`; remove `_lifetime.StopApplication()` from `finally`; add `Task.Delay(PollingInterval, stoppingToken)` at loop end
- [ ] 2.5 **RED**: Write test `ConnectorExecutionWorkerTests.ExecuteAsync_Continuous_RunsMultipleIterations` — verify `CreateScope` called ≥2 times within 500ms with short polling interval
- [ ] 2.6 **RED**: Write test `ConnectorExecutionWorkerTests.ExecuteAsync_Continuous_AdapterFailureDoesNotStopLoop` — adapter throws, verify loop continues to next iteration
- [ ] 2.7 **RED**: Write test `ConnectorExecutionWorkerTests.ExecuteAsync_Continuous_CancellationStopsGracefully` — cancel after 1 iteration, verify clean exit, no `StopApplication` call
- [ ] 2.8 **GREEN**: Update existing 4 tests in `ConnectorExecutionWorkerTests.cs`: replace `lifetime` mock with `IOptions<ConnectorExecutionOptions>` mock, update constructor calls, remove `lifetime.Received(1).StopApplication()` assertions
- [ ] 2.9 Add `ConnectorExecution` section to `src/Aura.Workers/appsettings.json` with `PollingIntervalSeconds: 300`
- [ ] 2.10 Add `builder.Services.Configure<ConnectorExecutionOptions>(builder.Configuration.GetSection("ConnectorExecution"))` in `src/Aura.Workers/Program.cs` before worker registrations

### Fix 2b — MorningSummarySchedulingWorker

- [ ] 2.11 **RED**: Write test `MorningSummarySchedulingWorkerTests.ProcessIterationAsync_WhenDue_ComposerCalled` — verify `composer.ComposeAsync` called with correct request after `MarkEmittedAsync`
- [ ] 2.12 **RED**: Write test `MorningSummarySchedulingWorkerTests.ProcessIterationAsync_WhenNotDue_ComposerNotCalled` — verify `composer.DidNotReceive().ComposeAsync()`
- [ ] 2.13 **RED**: Write test `MorningSummarySchedulingWorkerTests.ProcessIterationAsync_WhenDue_ComposerFailureLoggedNotThrown` — composer throws, verify no exception propagates, worker continues
- [ ] 2.14 **GREEN**: Modify `src/Aura.Workers/MorningSummarySchedulingWorker.cs`: add `IMorningSummaryComposer` constructor param; call `_composer.ComposeAsync(request, ct)` after `MarkEmittedAsync` in `ProcessIterationAsync`; wrap in try/catch, log failure at Error level
- [ ] 2.15 Update existing 2 tests in `MorningSummarySchedulingWorkerTests.cs`: add `composer` mock to constructor calls, verify `MarkedEmitted` assertion still holds

## Phase 3: NuGet Resolution

- [ ] 3.1 Audit `src/Aura.Infrastructure/Aura.Infrastructure.csproj` v10.x packages: `Microsoft.Extensions.AI` 10.6.0, `Microsoft.Extensions.Diagnostics.HealthChecks` 10.0.8, `Microsoft.Extensions.Options.ConfigurationExtensions` 10.0.8, `Microsoft.Extensions.Resilience` 10.6.0
- [ ] 3.2 Audit `tests/Aura.UnitTests/Aura.UnitTests.csproj` v10.x packages: `Microsoft.Extensions.DependencyInjection` 10.0.8, `Microsoft.Extensions.Diagnostics.HealthChecks` 10.0.8
- [ ] 3.3 Downgrade incompatible packages to .NET 9-compatible v9.x equivalents (or verify 10.x compatibility with `net9.0` TFM)
- [ ] 3.4 Run `dotnet restore Aura.sln` and `dotnet build Aura.sln` — zero errors expected

## Phase 4: Quality

- [ ] 4.1 Set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Directory.Build.props`
- [ ] 4.2 Run `dotnet build Aura.sln` — fix any warnings that surface (document each fix)
- [ ] 4.3 Run `dotnet test Aura.sln` — full suite passes (637+ tests)
- [ ] 4.4 Verify existing `AuthorizationFlowTests` pass: 401 without token, 200 with mock token, 401 with invalid token
