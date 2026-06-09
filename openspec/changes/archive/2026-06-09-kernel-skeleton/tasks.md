# Tasks: Kernel Skeleton

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 300-400 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Full kernel skeleton + unit tests | PR 1 | Single PR; split only if budget exceeded at apply time |

## Phase 1: Domain Foundation (TDD)

- [x] 1.1 RED — `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs`: valid transitions (`Pending->Processing->Completed`, `Processing->Faulted`) and invalid transitions (`Pending->Completed`, `Completed->Processing`) throw `InvalidOperationException`.
- [x] 1.2 GREEN — `src/Aura.Domain/WorkItems/WorkItemStatus.cs`: enum with `Pending`, `Processing`, `Completed`, `Faulted`.
- [x] 1.3 GREEN — `src/Aura.Domain/WorkItems/WorkItem.cs`: entity with `Id`, `Title`, `Source`, `Status`, `CreatedAt`, `FaultReason`; methods `MarkProcessing()`, `MarkCompleted()`, `MarkFaulted(string)` with transition guards.

## Phase 2: Kernel Pipeline (TDD)

- [x] 2.1 RED — `tests/Aura.UnitTests/Kernel/PluginRegistryTests.cs`: sequential execution order verified, empty registry completes without error, abort-on-failure marks WorkItem faulted and skips remaining plugins.
- [x] 2.2 GREEN — `src/Aura.Application/Kernel/IPlugin.cs` + `src/Aura.Application/Kernel/IPluginRegistry.cs`: contracts per design interfaces.
- [x] 2.3 GREEN — Add `Microsoft.Extensions.Logging.Abstractions` package to `src/Aura.Application/Aura.Application.csproj`.
- [x] 2.4 GREEN — `src/Aura.Application/Kernel/PluginRegistry.cs`: sequential loop, try/catch per plugin, `ILogger`, abort on first failure.
- [x] 2.5 `src/Aura.Application/Kernel/Plugins/HelloPlugin.cs`: no-op plugin that logs execution to prove pipeline wiring.

## Phase 3: Wiring and Worker

- [x] 3.1 RED — `tests/Aura.UnitTests/Kernel/KernelDiTests.cs`: build `ServiceCollection` with `AddAuraApplication()`, assert `IPluginRegistry` and at least one `IPlugin` resolve.
- [x] 3.2 GREEN — Modify `src/Aura.Application/DependencyInjection.cs`: add private `AddKernel()` registering `IPluginRegistry -> PluginRegistry` (singleton) and `IPlugin -> HelloPlugin`. Call from `AddAuraApplication()`.
- [x] 3.3 `src/Aura.Workers/HelloKernelWorker.cs`: `BackgroundService`, runs once — creates dummy WorkItem, executes pipeline, logs result, stops host.
- [x] 3.4 Modify `src/Aura.Workers/Program.cs`: register `HelloKernelWorker` as hosted service.

## Improvement Batch: Verify Warning Resolution

- [x] W1 Add direct `HelloPlugin` unit tests to cover `ExecuteAsync` behavior (no-op contract, constructor guard, multi-item independence)
- [x] W2 Add `--kernel-only` startup profile to `Program.cs` and kernel-only composition tests (turnkey startup, no infrastructure config needed)
