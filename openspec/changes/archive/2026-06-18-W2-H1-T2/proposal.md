# Proposal: W2-H1-T2 — Harden WorkItem Contract Test Coverage

## Intent

The `WorkItem` contract is already enforced in production (mandatory fields, closed `sourceType`, `correlationId`/`capturedAtUtc` normalization, fixed `schemaVersion`, non-null `metadata`). Coverage of boundary and normalization edge cases is uneven, and duplicate-input behavior is undocumented. This story closes the **test coverage gap** for the current contract. It is NOT a domain rewrite and does NOT introduce new business rules.

## Scope

### In Scope
- Add focused unit tests for already-specified field normalizations: `externalId`, `title`, `source`, `sourceType`, `priority`, `metadata`, `correlationId`, `capturedAtUtc`.
- Tighten boundary assertions (null/empty/whitespace) per existing contract behavior.
- Document duplicate cases only as candidate coverage / risk notes.

### Out of Scope
- New ingestion idempotency or deduplication seam (deferred to future backlog).
- Any new deduplication business rule.
- Combined partially-invalid multi-field input scenarios.
- Playwright / E2E.
- Production normalization changes in `WorkItem.cs`.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `work-item-contract`: No requirement rules change. Adds explicit edge-case **scenarios** for existing normalization/mandatory-field requirements to lock current behavior under test. Behavior is documented, not altered.

## Approach

Tests-only hardening (Exploration approach #1). Extend `WorkItemTests.cs` with edge-case assertions mapped one-to-one to existing spec requirements. Treat duplicate/replay as documented risk, not enforced behavior.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Modified | New boundary/normalization tests |
| `openspec/specs/work-item-contract/spec.md` | Modified | Added edge-case scenarios |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Tests restate constructor defaults (fake coverage) | Med | Assert observable normalization outcomes, not trivial defaults |
| Duplicate "idempotency" misread as enforced rule | Med | Document duplicates as risk-only; no dedup assertion |

## Rollback Plan

Revert the test-file commit and the spec scenario additions. No production code changes, so rollback is test-only and risk-free.

## Dependencies

- None. Operates against existing `WorkItem` domain entity.

## Success Criteria

- [ ] Each of the 8 contract fields has explicit normalization/boundary coverage.
- [ ] New tests pass against current `WorkItem` with no production changes.
- [ ] Duplicate handling captured as documented risk, not a new rule.
- [ ] No new idempotency/dedup seam introduced.
- [ ] Change stays within tests + spec scenarios only.
