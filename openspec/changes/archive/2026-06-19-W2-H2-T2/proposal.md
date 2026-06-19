# Proposal: W2-H2-T2 — Teams-First Connector Execution Flow

## Intent

Aura has the W2-H2-T1 checkpoint contract but no runtime orchestration that drives ingestion per connector. Operators cannot yet trigger or observe a single connector pulling data. This change introduces a narrow, testable connector execution use case at the Application layer, exercised end-to-end with **Microsoft Teams** as the first and only connector, so the team has a runnable, observable ingestion loop without committing to checkpoint persistence or multi-provider abstractions yet.

## Scope

### In Scope
- Application-level connector execution use case (orchestrates "run one connector").
- Domain-capability port(s)/DTOs the use case needs to invoke a connector and receive a canonical result.
- A Teams connector adapter contract wired as the single execution target.
- Minimal host wiring in `Aura.Workers` + `Aura.Infrastructure` DI to run the use case.
- Telemetry (trace/metric/log) for one execution run and unit/architecture test coverage.

### Out of Scope
- Checkpoint persistence and delta/idempotency logic (W2-H2-T3) — read-only consumption of existing checkpoint shape at most.
- Outlook, Calendar, and GitHub connectors.
- Scheduling, retries/resilience policies, and UI surfaces.

## Capabilities

### New Capabilities
- `connector-execution`: Orchestrate execution of a single ingestion connector through a domain-capability port, producing a canonical result with telemetry.

### Modified Capabilities
- None.

## Approach

Apply the exploration's recommended option: an **Application use case + worker orchestration**. Define a provider-neutral execution port by domain capability (not "Teams"), map the Teams SDK strictly inside Infrastructure to a canonical model, and keep `Workers` as a thin host that invokes the use case. Follows aura-plugin-design (capability-named port) and aura-clean-arch-guard (no SDK types above Infrastructure).

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Application/UseCases/` | New | Connector execution use case |
| `src/Aura.Application/Ports/` | New | Capability-named connector execution port + DTOs |
| `src/Aura.Infrastructure/` | New | Teams adapter + DI registration |
| `src/Aura.Workers/Program.cs` | Modified | Thin invocation of the use case |
| `tests/Aura.UnitTests`, `tests/Aura.ArchitectureTests` | New | Use-case + boundary coverage |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Orchestration logic leaks into `Workers` | Med | Keep host thin; assert via ArchitectureTests |
| Teams SDK types escape Infrastructure | Med | Canonical model at adapter boundary |
| Scope creep into checkpoint/idempotency | Med | Explicit non-goal; read-only checkpoint use |

## Rollback Plan

Revert the change branch: new use case, port, Teams adapter, and DI/host wiring are additive. Removing the DI registration and worker invocation restores prior no-op ingestion behavior; no schema or persisted state is introduced.

## Dependencies

- W2-H2-T1 checkpoint contract (`IIngestionCheckpointStore`, `IngestionCheckpoint`) — consumed read-only.
- Microsoft Teams API access via Graph (Infrastructure adapter only).

## Success Criteria

- [ ] Use case runs the Teams connector once and returns a canonical result.
- [ ] No provider SDK types appear above Infrastructure (ArchitectureTests pass).
- [ ] One execution emits correlated trace, metric, and log.
- [ ] `dotnet test Aura.sln` passes with new unit + architecture coverage.

## Proposal Assumptions (orchestrator-resolved)

- W2-H2-T2 = connector execution flow; first connector = Microsoft Teams.
- Outlook/Calendar/GitHub and checkpoint persistence (W2-H2-T3) deferred.
- Doc-vs-code note: ingestion docs treat runtime orchestration as a placeholder; this change fills that gap without altering checkpoint contracts.
