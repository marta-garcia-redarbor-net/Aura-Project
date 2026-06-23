# Proposal: W2-H5-T3 Morning Summary Timezone Scheduling

## Intent

The Morning Summary contract is documented but has no implementation for the timezone-aware scheduling slice. Without it, Aura cannot decide *when* a Morning Summary is due, in *which* timezone, or whether it was *already emitted today*. This change is needed now because downstream Morning Summary work (composition, delivery) depends on a trustworthy, DST-correct, idempotent scheduling signal as its foundation.

## Scope

### In Scope
- A single `Project/System Settings` source exposing at least `timezoneId` and a configurable `targetLocalTime`.
- A scheduler that resolves settings internally and returns a due-state result (`resolvedTimezoneId`, `localDate`, `targetLocalTime`, `isDue`).
- Timezone resolution chain: configured timezone -> system timezone -> UTC, using IANA/DST rules (never fixed offsets).
- Persisted daily emission guard: remember whether the Morning Summary was already emitted for the current local day, blocking same-day duplicates by default.
- A prepared technical path for forced re-emission by explicit user action (not wired to UI in this slice).

### Out of Scope
- Timezone-aware data-window semantics, ranking, and content composition.
- Same-day mutation of `targetLocalTime` (assumed stable in normal operation).
- The actual forced re-emission user action / UI (only the override-ready seam is prepared).

## Capabilities

### New Capabilities
- `morning-summary-scheduling`: settings-driven, DST-correct due-state resolution and persisted daily emission idempotence for the Morning Summary.

### Modified Capabilities
- None.

## Approach

Use the dedicated settings-provider + scheduler pattern (exploration recommendation). Add a settings-provider port and a scheduler port in `Application`, with a small result model for due-state. The scheduler resolves settings internally; the concrete project/system settings provider and config binding live in `Infrastructure`. `targetLocalTime` default (`09:00`) is config-supplied, never hardcoded into the contract.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/` | New | Scheduler port + settings-provider port |
| `src/Aura.Application/Models/` | New | Scheduler due-state / resolved-settings models |
| `src/Aura.Application/DependencyInjection.cs` | Modified | Register scheduler + provider wiring |
| `src/Aura.Infrastructure/Adapters/` | New | Concrete settings provider + config binding |
| `src/Aura.Workers/` | Modified | Host consuming the scheduler contract |
| `tests/Aura.UnitTests/` | New | DI, resolution, due-state, idempotence tests |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Vague result model makes idempotence ambiguous | Med | Explicit `isDue` + `localDate` contract with regression tests |
| Fixed offsets break DST | Med | Resolve via timezone rules only; DST test cases |
| Drift into data-window/composition scope | Med | Scope boundary enforced in spec + review |

## Rollback Plan

The slice is additive (new ports, models, provider, worker wiring). Revert by removing the new contracts and DI registrations and unwiring the worker entry point; no existing capability is modified, so no data migration or contract reversal is required.

## Dependencies

- Existing provider-based settings pattern in the codebase (shape to mirror).

## Success Criteria

- [ ] Scheduler resolves timezone via configured -> system -> UTC chain with DST correctness.
- [ ] `targetLocalTime` is config-driven; `09:00` default is not hardcoded in the contract.
- [ ] At most one Morning Summary per user per local day; re-runs report "already emitted".
- [ ] Persisted daily guard and an override-ready seam for forced re-emission exist.
- [ ] Unit tests cover resolution, due-state, and idempotence.
