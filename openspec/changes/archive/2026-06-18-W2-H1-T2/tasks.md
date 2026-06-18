# Tasks: W2-H1-T2 — Harden WorkItem Contract Test Coverage

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 90–170 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | single PR |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Harden WorkItem contract tests + minimal guard fix | PR 1 | Base `main`; include RED→GREEN→REFACTOR and `dotnet test Aura.sln` |

## Phase 1: Foundation / RED

- [x] 1.1 Verify constructor behavior in `src/Aura.Domain/WorkItems/WorkItem.cs` for `capturedAtUtc` (`DateTimeOffset?`, null-coalescing) before finalizing MinValue assertions in tests.
- [x] 1.2 In `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs`, add RED whitespace theories for `externalId`, `title`, and `source` (`"   "`) expecting argument validation errors.
- [x] 1.3 In `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs`, add RED test for whitespace `correlationId` (`"   "`) requiring auto-generation (`!string.IsNullOrWhiteSpace`).
- [x] 1.4 In `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs`, add RED `capturedAtUtc` boundary tests: `DateTimeOffset.MinValue` behavior (after 1.1 verification) and local-offset value accepted/preserved.
- [x] 1.5 In `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs`, add RED positive test for populated metadata dictionary preserving keys/values.

## Phase 2: Core Implementation / GREEN

- [x] 2.1 Update string guards in `src/Aura.Domain/WorkItems/WorkItem.cs` from `IsNullOrEmpty` to `IsNullOrWhiteSpace` for `externalId`, `title`, and `source` so RED whitespace tests pass.
- [x] 2.2 Update `correlationId` normalization in `src/Aura.Domain/WorkItems/WorkItem.cs` to treat whitespace as absent using `IsNullOrWhiteSpace`.
- [x] 2.3 For `capturedAtUtc` in `src/Aura.Domain/WorkItems/WorkItem.cs`, apply only the minimal change required by verified behavior and RED tests; do not introduce unrelated constructor rules.

## Phase 3: Refactor / Constraint Alignment

- [x] 3.1 Refactor `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` to reuse helpers/InlineData while keeping each new scenario mapped to spec requirements.
- [x] 3.2 Confirm no tests/tasks are added for `sourceType` or `priority` whitespace (enum whitespace cases out of scope).
- [x] 3.3 Confirm duplicate-input/idempotency remains risk-only documentation; do not add deduplication production or test seams.

## Phase 4: Verification

- [x] 4.1 Run `dotnet test Aura.sln` and ensure all `WorkItem` unit tests pass with strict TDD completion evidence.
- [x] 4.2 Verify no Playwright/E2E work was introduced and only intended files changed: `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` and conditional `src/Aura.Domain/WorkItems/WorkItem.cs`.
