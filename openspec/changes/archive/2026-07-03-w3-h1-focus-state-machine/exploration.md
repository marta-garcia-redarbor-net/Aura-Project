## Exploration: W3-H1 — Modelar estados de foco (Focus State Machine)

### Current State

No FocusState code exists in the codebase. The architecture document `docs/architecture/triage/03-focus-state-machine.md` explicitly defers this work, stating that the triage-global-policy boundary must stabilize first. That boundary is now in place (the `triage-global-policy-foundation` change is archived). The `IFocusStateResolver` port is listed in `docs/ai/02-architecture-map.md` as a future integration point in the Triage group, but no interface file exists yet.

The backlog defines four states: `DeepWork`, `WindowOfOpportunity`, `Away`, `Recovery`.

### Affected Areas

- `src/Aura.Domain/FocusState/` — new domain folder. A `FocusStateType` enum (the four states) and a `FocusState` sealed class with guarded transition methods, following the `WorkItem` pattern.
- `src/Aura.Application/Ports/IFocusStateResolver.cs` — the port interface (declared in the architecture map but not yet coded).
- `src/Aura.Application/Services/FocusStateResolver.cs` — the resolver implementation that determines the current state based on signals (calendar, activity, preferences).
- `src/Aura.Domain/FocusState/FocusStateTransition.cs` — (optional, depending on approach) a value object or record that captures transition rules: allowed origin → target, duration, cooldown.
- `tests/Aura.UnitTests/Triage/FocusStateMachineTests.cs` — new test file following existing patterns (manual stubs, `Method_Scenario_Expected` naming, helper factory methods).

### Approaches

1. **Simple enum + switch resolver** — FocusStateType enum with a static resolver that uses a switch/pattern-match on input signals (current calendar event, user activity, time since last interruption). No domain entity, no transition configuration.
   - Pros: Minimal code, fast to implement, easy to test
   - Cons: Transition rules are implicit (in the resolver switch), not modeled as domain knowledge; adding cooldowns or duration-based transitions requires refactoring; no domain validation for invalid transitions
   - Effort: Low (2-3 files, ~150 lines total)

2. **Rich domain state machine with guarded transitions** — A `FocusState` sealed class (mirroring `WorkItem`) with methods like `TryEnterDeepWork()`, `TryEnterRecovery()`, each enforcing transition guards. A separate `FocusStateResolver` consumes external signals and calls the appropriate transition. Transition rules are explicit in the domain.
   - Pros: Domain-visible transition rules (no hidden logic in a resolver); guards prevent illegal transitions (e.g., `Away` → `DeepWork` without going through `Recovery`); extensible for cooldowns, durations, observability; matches existing `WorkItem` pattern exactly
   - Cons: More code, more files to test; requires thinking through all valid/invalid transitions up front
   - Effort: Medium (4-5 files, ~350 lines total + tests)

3. **Configured transition matrix** — Same as Approach 2 but transition rules come from an injected `IFocusStateConfiguration` (allowing user configuration of cooldowns, allowed transitions per state). The domain validates against the configuration rather than hardcoded rules.
   - Pros: Maximum flexibility, user-adjustable per governance requirements
   - Cons: Premature — no user is asking for custom transition rules at this stage; over-engineering a problem we don't yet understand; adds complexity to tests
   - Effort: High (6-8 files, ~500 lines total)

### Recommendation

**Approach 2 — Rich domain state machine with guarded transitions.**

Why: It follows the established `WorkItem` pattern exactly (sealed class + guarded transition methods + domain enum), which means the team already understands this model. The transition rules land in the domain where they belong — not hidden in a resolver switch. The "configured" variant (Approach 3) can be layered on later when user-facing configuration is actually required, without breaking the domain contract.

The `IFocusStateResolver` port stays in `Aura.Application.Ports`, the `FocusState` entity lives in `Aura.Domain.FocusState`, and the resolver implementation lives in `Aura.Application.Services` — clean layer separation.

### Risks

- **Missing signals**: The resolver needs calendar data (from `ICalendarEventStore`), user activity, and focus preferences. If those ports aren't ready, the resolver will need stub/mock inputs for testing, and the initial implementation should document which integration points are placeholder.
- **Transition model assumptions**: The four states (`DeepWork`, `WindowOfOpportunity`, `Away`, `Recovery`) form a cycle, but the exact valid transitions need careful modeling. E.g., can you go from `Away` directly to `WindowOfOpportunity`, or must you pass through `Recovery`? These are domain questions that must be resolved in design.
- **Cooldown enforcement**: If cooldowns between transitions become a requirement, the simple `FocusState` entity grows a timer/duration dependency. Should be acknowledged but kept out of scope for now.
- **Duplicate signal resolution**: If both a calendar event (meeting) and a user DND setting signal different states, what wins? The resolver needs a clear priority ordering.

### Ready for Proposal

Yes. The domain model is straightforward and follows established patterns. The design phase should clarify the exact transition matrix (which transitions between the four states are valid) and the resolver signal priority rules.
