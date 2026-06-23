# Exploration: W2-H5-T3 Morning Summary timezone scheduling

### Current State
The Morning Summary contract is documented, but there is no implementation yet for the timezone-aware scheduler/settings slice. The docs now define a single `Project/System Settings` source with `timezoneId` and `targetLocalTime`, internal timezone resolution (`configured -> system -> UTC`), DST via timezone rules, and daily idempotence. In code, Aura already uses a provider-based settings pattern for other capabilities, which is the closest existing shape to mirror.

### Affected Areas
- `src/Aura.Application/Ports/` — needs the scheduler port and a settings-provider port for Morning Summary.
- `src/Aura.Application/Models/` — likely needs scheduler result/settings models for resolved timezone and due-state.
- `src/Aura.Application/DependencyInjection.cs` — will need registration for the new scheduler/use case wiring.
- `src/Aura.Infrastructure/Adapters/...` — needs the concrete project/system settings provider and any config binding.
- `src/Aura.Workers/` — likely the host entry point that will consume the scheduler contract.
- `tests/Aura.UnitTests/` — needed for DI, settings resolution, and due-state/idempotence tests.
- `docs/architecture/triage/01-morning-summary.md` — current contract anchor for this slice.
- `StoryBacklog.md` — already states the DoD for W2-H5-T3 and should stay aligned.

### Approaches
1. **Dedicated settings provider + scheduler port/result models** — keep settings resolution inside the scheduler and expose a small result model for due-state.
   - Pros: matches existing provider pattern; respects the requirement that the scheduler resolves settings internally; keeps Application clean.
   - Cons: introduces several new small contracts at once.
   - Effort: Medium

2. **Pass resolved settings into the scheduler** — resolve settings in the caller and inject them into the scheduling method.
   - Pros: fewer internal dependencies inside the scheduler.
   - Cons: conflicts with the stated preference that the scheduler should resolve settings internally; spreads config knowledge into orchestration.
   - Effort: Low

### Recommendation
Use the dedicated settings-provider approach. It best fits Aura’s existing port/adapter pattern, keeps `Project/System Settings` as a project-level concern, and avoids hardcoding `09:00` or leaking timezone resolution into the caller.

### Risks
- If the result model is too vague, daily idempotence can become ambiguous in tests.
- If timezone resolution is implemented with fixed offsets instead of timezone rules, DST behavior will be wrong.
- The change can drift into data-window or composition semantics even though W2-H5-T3 is scheduling-only.

### Ready for Proposal
Yes — the codebase has a clear provider pattern to mirror, and the remaining work is to define the Morning Summary scheduler/settings contracts and their tests.
