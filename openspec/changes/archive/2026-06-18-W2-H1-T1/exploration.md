## Exploration: W2-H1-T1 — Refine mandatory WorkItem fields

### Current State
`WorkItem` is a Domain entity with `Id`, `Title`, `Source`, `Status`, `CreatedAt`, and optional `FaultReason`. Today, the constructor only enforces non-empty `title` and `source`, and every new item starts as `Pending`. State transitions are guarded in the entity itself (`MarkProcessing`, `MarkCompleted`, `MarkFaulted`).

The pipeline currently assumes this minimal shape: `PluginRegistry` moves items to `Processing`, then `Completed` or `Faulted`, and `HelloKernelWorker` creates a `WorkItem` directly. Existing unit tests already lock in the current constructor and transition behavior.

### Affected Areas
- `src/Aura.Domain/WorkItems/WorkItem.cs` — source of the entity invariants and mandatory fields.
- `src/Aura.Domain/WorkItems/WorkItemStatus.cs` — status model is unchanged, but must stay compatible with new invariants.
- `src/Aura.Application/Kernel/PluginRegistry.cs` — consumes `WorkItem` and depends on constructor/transition behavior.
- `src/Aura.Workers/HelloKernelWorker.cs` — creates a sample `WorkItem` directly.
- `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` — currently constrains constructor validation and lifecycle transitions.
- `tests/Aura.UnitTests/Kernel/PluginRegistryTests.cs` — constrains pipeline behavior when a `WorkItem` is processed.

### Approaches
1. **Extend the domain entity directly** — add `Priority`, `Metadata`, and any other mandatory fields to `WorkItem` and enforce them in the constructor.
   - Pros: simplest boundary; keeps invariants in the entity; aligns with current direct-construction usage.
   - Cons: may force wider test updates if metadata shape is still unclear.
   - Effort: Medium

2. **Introduce a canonical factory/value object first** — move normalization and mandatory-field assembly into a domain factory, then keep `WorkItem` narrower.
   - Pros: better if future ingestion sources need normalization/idempotency.
   - Cons: adds a new abstraction before requirements are fully pinned down.
   - Effort: High

### Recommendation
Extend the domain entity directly for this slice. The current codebase already treats `WorkItem` as the canonical object used across Domain, Application, and Workers, so the least risky change is to refine its constructor and invariants in place, then update the unit tests to pin the new mandatory fields.

### Risks
- `StoryBacklog.md` defines the DoD broadly (“origen, prioridad, timestamp y metadatos mínimos”), but the exact metadata contract is not yet specified.
- Adding required constructor parameters will ripple into tests and worker/bootstrap code immediately.

### Ready for Proposal
Yes — the change is clear enough to move to proposal, but the proposal must define the exact mandatory field contract and normalization rules before implementation.
