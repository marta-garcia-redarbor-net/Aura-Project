# Exploration: triage global policy foundation

### Current State
The docs already describe a split between ingestion and triage, but the triage docs are still placeholders. Ingestion docs show Teams and Outlook connectors mapping provider payloads into canonical `WorkItem` entities, and Outlook already computes source-specific preliminary scoring from multiple signals. What is still missing is an explicit global policy boundary that states: connectors normalize and score locally, then a shared triage engine makes the final interrupt-vs-queue decision.

Focus Mode is documented as a future triage concern, but it is not ready to be introduced in this change and should remain deferred.

### Affected Areas
- `docs/architecture/triage/00-overview.md` — needs the global triage model and decision boundary stated clearly.
- `docs/architecture/triage/02-proactive-interruptions.md` — should describe auditable interrupt vs defer rules.
- `docs/architecture/triage/03-focus-state-machine.md` — must remain deferred and explicitly out of scope for now.
- `docs/architecture/triage/04-priority-scoring.md` — should reflect explainable, adjustable global scoring and learned preferences.
- `docs/architecture/ingestion/00-overview.md` — already hints at connector normalization; should be aligned with the canonical WorkItem + preliminary scoring wording.
- `docs/architecture/ingestion/01-microsoft-graph-teams.md` — future Teams content-based preliminary scoring needs architectural contrast.
- `docs/architecture/ingestion/02-microsoft-graph-outlook.md` — useful contrast because Outlook already exposes source-specific scoring signals.
- `docs/ai/02-architecture-map.md` — may need contract naming updates if the triage engine boundary is formalized.
- `StoryBacklog.md` — needs a new future task for Teams connector content-based preliminary scoring.

### Approaches
1. **Document the two-stage model with explicit boundary** — connectors normalize and preliminarily score; triage owns the final decision.
   - Pros: matches current docs and future implementation shape; keeps responsibilities clean.
   - Cons: requires careful wording so triage does not look like it owns source-specific parsing.
   - Effort: Low

2. **Document source-owned policies per connector** — Teams/Outlook each own their own interrupt rules.
   - Pros: simple to explain locally.
   - Cons: fragments policy, duplicates logic, and conflicts with the desired global decision model.
   - Effort: Medium

3. **Document a global engine plus explicit user adjustment loop** — the engine decides, but preferences, feedback, and history refine it transparently.
   - Pros: best fit for explainability, auditability, and user control.
   - Cons: needs stricter terminology so “learning” is not mistaken for opaque self-training.
   - Effort: Medium

### Recommendation
Use approach 1 with the refinement from approach 3: define a global triage engine as the final decision authority, and state that refinement starts from explicit user preferences, feedback, and history only. Keep Focus Mode deferred and add the Teams backlog task as a future connector follow-up, not part of the triage foundation change.

### Risks
- The docs may still imply source-specific decision ownership if the boundary is not stated plainly.
- “Learning” language can drift into opaque automation unless the docs anchor it to explicit user inputs.
- Focus Mode scope creep could pull this change away from the intended foundation work.

### Ready for Proposal
Yes — the exploration is sufficient to draft the proposal and the backlog update, provided Focus Mode stays explicitly out of scope.
