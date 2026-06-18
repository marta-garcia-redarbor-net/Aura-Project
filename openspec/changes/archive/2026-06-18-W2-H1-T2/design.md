# Design: W2-H1-T2 — Harden WorkItem Contract Test Coverage

## Technical Approach

Extend `WorkItemTests.cs` with new `[Theory]` / `[Fact]` cases mapped to the spec scenarios.
Most coverage is tests-only. **Three string guards in `WorkItem.cs` use `IsNullOrEmpty` and
will need upgrading to `IsNullOrWhiteSpace`** to make whitespace scenarios pass — this is the
only expected production touch. Two spec scenarios (`sourceType`/`priority` whitespace) are
type-unsafe against the enum constructor and must be reformulated or dropped at apply time.
`capturedAtUtc` boundary behavior requires verification against `DateTimeOffset?` semantics
before writing those tests.

> Apply strategy: attempt tests-only first. If whitespace assertion tests fail (they will for
> `externalId`, `title`, `source`, `correlationId`), upgrade the four guards as described below.

## Architecture Decisions

| Decision | Options | Tradeoff | Choice |
|----------|---------|----------|--------|
| String guard scope | Keep `IsNullOrEmpty` | Whitespace spec scenarios cannot pass | ❌ Rejected |
| String guard scope | Upgrade to `IsNullOrWhiteSpace` for all four string params | Non-breaking tightening; aligns code with spec | ✅ Selected |
| `sourceType`/`priority` whitespace | Test with string inputs | Impossible: params are typed enums, no string path exists | ❌ Spec inaccuracy — skip or reformulate |
| `capturedAtUtc` MinValue guard | No guard (keep `??` null-only) | MinValue would be stored as-is; spec says fallback to UtcNow | Verify during apply |
| `capturedAtUtc` Local kind | Test with `new DateTimeOffset(DateTime.Now)` | `DateTimeOffset` normalises Kind internally; likely passes today | Write test; verify no throw |
| Metadata populated | Dedicated `[Fact]` test | Positive coverage not explicitly isolated; clean to add | ✅ Add test |

## Data Flow

No runtime data flow change. Only the validation seam moves:

    Caller → WorkItem ctor
        │─ IsNullOrWhiteSpace(externalId)  ← was IsNullOrEmpty
        │─ IsNullOrWhiteSpace(title)        ← was IsNullOrEmpty
        │─ IsNullOrWhiteSpace(source)       ← was IsNullOrEmpty
        │─ IsNullOrWhiteSpace(correlationId)← was IsNullOrEmpty in null-coalesce
        └─ (all other guards unchanged)

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Modify | Add ~9 new test methods (whitespace `[Theory]` cases, `correlationId` whitespace, `capturedAtUtc` boundary, metadata populated) |
| `src/Aura.Domain/WorkItems/WorkItem.cs` | Modify (conditional) | Upgrade four `IsNullOrEmpty` guards to `IsNullOrWhiteSpace`: `externalId`, `title`, `source`; `correlationId` normalization check |

## Interfaces / Contracts

No public API surface change. Guard tightening is backwards-compatible: whitespace-only
strings were previously accepted incorrectly and would have propagated as garbage data.
Tightening validation is not a breaking change for well-formed callers.

## Testing Strategy

All tests are unit layer in the existing class. Follow the established `[Theory][InlineData]`
and `CreateValidWorkItem` helper patterns.

| Layer | What to Test | Test method pattern |
|-------|-------------|---------------------|
| Unit | `externalId` whitespace-only rejected | `[Theory][InlineData("   ")]` extending existing null/empty theory |
| Unit | `title` whitespace-only rejected | Same pattern |
| Unit | `source` whitespace-only rejected | Same pattern |
| Unit | `correlationId` whitespace → auto-generates | `[Theory][InlineData("   ")]`, assert `!IsNullOrWhiteSpace` |
| Unit | `capturedAtUtc = DateTimeOffset.MinValue` → UtcNow | `[Fact]`, conditional on production guard; timestamp range assertion |
| Unit | `capturedAtUtc` Local-offset `DateTimeOffset` accepted | `[Fact]`, pass `new DateTimeOffset(DateTime.Now)`, assert no throw and value preserved |
| Unit | Metadata populated dictionary accessible | `[Fact]`, supply `{ "key": "value" }`, assert entry count and value |

> `sourceType` and `priority` whitespace scenarios: **omit** — both params are typed enums;
> no string input path exists in the constructor. Spec scenarios are inaccurate for these fields.

## Migration / Rollout

No migration required. Whitespace guard upgrade is a single-commit, tests-only-gated change
with no downstream impact. Rollback is a test-file and guard revert with no data concerns.

## Open Questions

- [ ] **`capturedAtUtc` MinValue semantics**: Spec says `default(DateTime)` should fall back
  to UtcNow, but the constructor takes `DateTimeOffset?`. Should `DateTimeOffset.MinValue`
  (the closest mapping) trigger the fallback? If yes, a production guard is needed:
  `if (capturedAtUtc == DateTimeOffset.MinValue) capturedAtUtc = null;`. Confirm before writing
  the MinValue test.
- [ ] **`sourceType`/`priority` whitespace scenarios**: Confirm they are dropped (enum type
  mismatch) and that the spec text is treated as erroneous rather than a signal that a
  string-parsing layer should be added at this stage.
