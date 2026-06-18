# Apply Progress: W2-H1-T2

## Run Context

- Mode: Strict TDD
- Store: OpenSpec
- Delivery: single low-risk apply batch (approved)
- Scope guard: no Playwright/E2E additions, no dedup/idempotency seam, no enum-whitespace scenarios (`sourceType`, `priority`)

## Completed Tasks

- [x] 1.1 Verify constructor behavior for `capturedAtUtc` before final assertions
- [x] 1.2 Add RED whitespace tests for `externalId`, `title`, `source`
- [x] 1.3 Add RED whitespace test for `correlationId` auto-generation
- [x] 1.4 Add RED boundary tests for `capturedAtUtc` MinValue + local-offset preservation
- [x] 1.5 Add RED test for populated `metadata` preservation
- [x] 2.1 Upgrade string guards to `IsNullOrWhiteSpace` for required string fields
- [x] 2.2 Upgrade `correlationId` normalization to treat whitespace as absent
- [x] 2.3 Apply minimal `capturedAtUtc` production change required by RED tests
- [x] 3.1 Keep tests aligned with existing helper/InlineData patterns
- [x] 3.2 Confirm enum whitespace scenarios were not added
- [x] 3.3 Confirm duplicate handling remains risk-only documentation
- [x] 4.1 Run `dotnet test Aura.sln`
- [x] 4.2 Verify no Playwright/E2E implementation was introduced

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | ⚠️ Existing baseline run before edits was not captured separately | ✅ Constructor inspected first (`DateTimeOffset?` + null-coalescing) | ✅ Behavior codified by 1.4 tests and passing after minimal fix | ✅ MinValue + local-offset cases cover distinct paths | ➖ None needed |
| 1.2 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | ⚠️ See 1.1 note | ✅ Added whitespace InlineData for `externalId`, `title`, `source` | ✅ Passing after guard updates | ✅ Null/empty/whitespace paths covered | ✅ Reused existing `[Theory]` pattern |
| 1.3 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | ⚠️ See 1.1 note | ✅ Added whitespace InlineData for `correlationId` | ✅ Passing after whitespace-aware normalization | ✅ Null/empty/whitespace generation paths covered | ➖ None needed |
| 1.4 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | ⚠️ See 1.1 note | ✅ Added MinValue fallback + local-offset preservation tests | ✅ Passing after minimal `capturedAtUtc` adjustment | ✅ Fallback vs preserve paths covered | ➖ None needed |
| 1.5 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | ⚠️ See 1.1 note | ✅ Added populated metadata preservation test | ✅ Passes without production change | ✅ Complemented existing empty-metadata acceptance test | ➖ None needed |
| 2.1 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | N/A (implementation step) | ✅ Driven by failing whitespace rejection tests | ✅ Targeted WorkItem tests pass (35/35) | ✅ Multiple field cases prevent fake implementation | ✅ Minimal constructor-only changes |
| 2.2 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | N/A (implementation step) | ✅ Driven by failing whitespace correlation test | ✅ Targeted WorkItem tests pass (35/35) | ✅ Null/empty/whitespace all covered | ➖ None needed |
| 2.3 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | N/A (implementation step) | ✅ Driven by failing MinValue boundary test | ✅ Targeted WorkItem tests pass (35/35) | ✅ MinValue fallback + local-offset preserve | ✅ Minimal conditional check only |
| 3.1 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | N/A | ✅ Test structure extension done in RED additions | ✅ File remains green after cleanup pass | ✅ Existing helper and theory patterns reused | ✅ No helper duplication introduced |
| 3.2 | `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Unit | N/A | ✅ Constraint asserted from design/spec limits | ✅ Green (no enum-whitespace tests added) | ➖ Not applicable (scope constraint) | ✅ Scope preserved |
| 3.3 | `openspec/changes/W2-H1-T2/*` | Unit (scope control) | N/A | ✅ Constraint asserted from proposal/spec risk note | ✅ Green (no dedup behavior introduced) | ➖ Not applicable (scope constraint) | ✅ Scope preserved |
| 4.1 | `Aura.sln` | Mixed suite | N/A | ✅ Verification task defined in tasks | ✅ `dotnet test Aura.sln` passed | ➖ Not applicable | ➖ None needed |
| 4.2 | N/A | N/A | N/A | ✅ Verification task defined in tasks | ✅ Confirmed no new Playwright/E2E implementation files | ➖ Not applicable | ➖ None needed |

## Test Summary

- Total tests written: 8 new unit tests/scenarios in `WorkItemTests` (3 whitespace InlineData expansions + 1 metadata positive + 1 correlationId whitespace + 2 capturedAtUtc boundary tests + 1 local-offset preservation)
- Total tests passing:
  - Targeted: `Aura.UnitTests.WorkItems.WorkItemTests` → 35/35 passing
  - Full suite: 345/345 passing (`Unit 250`, `Architecture 19`, `Integration 55`, `E2E 21`)
- Layers used in this apply batch: Unit (implementation changes), full-suite verification run as required
- Approval tests: None — behavior change intentionally introduced for whitespace/MinValue contract hardening
- Pure functions created: 0

## Production Changes (Minimal)

- `src/Aura.Domain/WorkItems/WorkItem.cs`
  - `externalId`, `title`, `source`: `IsNullOrEmpty` → `IsNullOrWhiteSpace`
  - `correlationId`: whitespace now treated as absent for auto-generation
  - `capturedAtUtc`: `DateTimeOffset.MinValue` now treated as absent and replaced with `UtcNow`

## Spec/Design Tension Resolution

- Confirmed constructor signature is `DateTimeOffset? capturedAtUtc`.
- Delta spec mentions `default(DateTime)`, but the actual boundary-equivalent input at this constructor is `DateTimeOffset.MinValue`.
- Apply implementation uses that constructor-truth while remaining within approved scope.

## Verification Evidence

1. RED evidence (targeted run before production fix):
   - `Constructor_EmptyExternalId_ThrowsArgumentException("   ")` failed
   - `Constructor_EmptyTitle_ThrowsArgumentException("   ")` failed
   - `Constructor_EmptySource_ThrowsArgumentException("   ")` failed
   - `Constructor_EmptyCorrelationId_GeneratesCorrelationId("   ")` failed
   - `Constructor_CapturedAtUtcMinValue_FallsBackToCurrentUtc` failed
2. GREEN evidence:
   - `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~Aura.UnitTests.WorkItems.WorkItemTests"` → 35/35 passing
3. VERIFY evidence:
   - `dotnet test Aura.sln` → 345/345 passing

## Scope Confirmation

- No Playwright/E2E implementation work added.
- No ingestion idempotency seam or deduplication behavior added.
- No `sourceType`/`priority` whitespace cases introduced.
- Changed implementation/test files limited to:
  - `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs`
  - `src/Aura.Domain/WorkItems/WorkItem.cs`
  - `openspec/changes/W2-H1-T2/tasks.md`
  - `openspec/changes/W2-H1-T2/apply-progress.md`
