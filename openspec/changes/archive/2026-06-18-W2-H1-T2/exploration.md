## Exploration: W2-H1-T2 — Add normalization and idempotency tests

### Current State
`WorkItem` already enforces the canonical contract in production: mandatory fields, closed `sourceType`, correlationId fallback generation, `capturedAtUtc` fallback, fixed `schemaVersion`, and non-null metadata. Existing unit tests cover most of that contract in `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs`.

What is missing today is explicit coverage for duplicate/null-field normalization behavior at the ingestion boundary and any real idempotency behavior beyond the domain constructor. There is also no active OpenSpec change folder yet.

### Affected Areas
- `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` — already covers the base contract; needs tighter boundary/edge-case assertions if we want the change to land as tests-only.
- `src/Aura.Domain/WorkItems/WorkItem.cs` — production normalization already exists for `correlationId` and `capturedAtUtc`; no new behavior is obviously required from this exploration alone.
- `docs/architecture/ingestion/05-normalization-checkpoints.md` — still a placeholder; it confirms normalization/idempotency policy is incomplete at the architecture level.
- `tests/Aura.IntegrationTests` — likely the right place if we need duplicate/replay behavior across ingestion or persistence seams, but no concrete idempotency seam was found yet.

### Approaches
1. **Tests-only hardening** — add focused unit tests for null/empty normalization edge cases and duplicate-candidate scenarios around the existing `WorkItem` contract.
   - Pros: smallest change, matches current production behavior, low risk.
   - Cons: does not prove end-to-end idempotency; may only validate constructor semantics.
   - Effort: Low

2. **Add test-support seam for ingestion idempotency** — introduce a small abstraction around deduplication/checkpointing and test it with integration-style cases.
   - Pros: validates real idempotency behavior, not just entity construction.
   - Cons: requires production seam work before tests can be meaningful.
   - Effort: Medium

### Recommendation
Start with tests-only hardening unless the upcoming story explicitly includes an ingestion deduplication seam. The codebase already normalizes the core `WorkItem` fields, so W2-H1-T2 looks more like missing coverage than missing domain logic.

### Risks
- “Idempotency tests” can become fake coverage if they only restate constructor defaults.
- The architecture doc still lacks a defined dedup/checkpoint contract, so integration tests may drift without a seam.

### Ready for Proposal
Yes — but the proposal should state that current evidence points to coverage gaps, not a required domain rewrite, and it should exclude Playwright/E2E.
