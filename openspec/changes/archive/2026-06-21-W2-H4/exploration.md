# Exploration: W2-H4 — Outlook Plugin Implementation

### Current State
W2-H4 has no active OpenSpec artifacts yet, and the repo currently has **no Outlook-specific adapter files**. The established ingestion pattern is adapter-local normalization in `Aura.Infrastructure`: Teams already uses a DTO + mapper + adapter flow that keeps provider types out of `Application` and maps into canonical `WorkItem` instances.

The domain already supports `WorkItemSourceType.OutlookEmail`, and the canonical `WorkItem` contract already covers mandatory fields, `correlationId`, `capturedAtUtc`, and metadata shape. The backlog for W2-H4 explicitly asks for Outlook DTO/mock payloads, mapping emails to `WorkItem`, and initial classification tests.

### Affected Areas
- `src/Aura.Infrastructure/Adapters/Connectors/Outlook/` — new Outlook DTO, mapper, and adapter should live here.
- `src/Aura.Domain/WorkItems/WorkItem.cs` and `WorkItemSourceType.cs` — canonical target model already exists; likely no change needed.
- `src/Aura.Application/Ports/IConnectorAdapter.cs` — current provider-neutral connector contract appears sufficient.
- `tests/Aura.UnitTests/Ingestion/Outlook/` — Outlook mapping/classification tests should mirror the Teams pattern.
- `docs/architecture/ingestion/00-overview.md` and `docs/ai/02-architecture-map.md` — currently still describe Graph adapters as pending and need alignment later.
- `openspec/specs/teams-connector-mapping/spec.md` — useful as the closest mapping spec pattern for the new Outlook slice.

### Approaches
1. **Adapter-local Outlook ACL** — create Outlook DTOs, mock payloads, and mapping logic inside Infrastructure, returning canonical `WorkItem` only.
   - Pros: matches current Clean Architecture pattern, minimal surface area, keeps Outlook/Graph types contained.
   - Cons: Outlook-specific mapping rules may be duplicated later if a shared normalization layer is introduced.
   - Effort: Medium

2. **Shared normalization contract first** — introduce a shared Application-level Outlook/Graph normalization abstraction before building the adapter.
   - Pros: could reduce duplication across Outlook/Teams/Calendar.
   - Cons: premature abstraction for a single backlog slice, higher wiring cost, more architectural risk.
   - Effort: High

### Recommendation
Use the adapter-local Outlook ACL. W2-H4 is a provider-specific slice, the domain already has `OutlookEmail`, and the repo’s existing Teams implementation shows the right boundary: DTOs and mapping stay in Infrastructure, while `WorkItem` remains the canonical output. Start with mock Outlook payloads, map to `WorkItem`, and cover source/priority/deadline-style classification with focused unit tests.

### Risks
- Outlook payload shape may differ enough from Teams that overly generic DTOs hide real mapping gaps.
- If the slice starts assuming a shared ingestion normalizer, scope can expand beyond the backlog story.
- Existing docs still say Graph adapters are pending, so proposal/design must call out doc-vs-code mismatch explicitly.

### Ready for Proposal
Yes — the boundary is clear enough for proposal work. The next phase should define the Outlook payload shape, mapping rules to `WorkItem`, and the initial test scenarios.
