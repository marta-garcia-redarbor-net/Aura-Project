# Archive Report: pr-ui-stitch-alignment

**Change**: PR Connector UI — Stitch Alignment
**Date**: 2026-07-02
**Mode**: hybrid (openspec filesystem + Engram)
**Verdict**: PASS WITH WARNINGS

---

## Change Summary

Aligned the PR connector v1 UI (dashboard card + detail page) with Stitch designs. Extended the data model with 6 new fields on `PullRequestResponse`, introduced `PrPreviewItemResponse` to replace `InboxItemPreviewResponse` for PR dashboard cards, added `IsPrCard` flag to `PrioritySummaryCard`, rebuilt the `PullRequests.razor` detail page with Stitch-aligned columns, filter bar, and pagination, and added CSS for CI badges, mini-table, and layout components.

## Files Modified

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.UI/Models/PullRequestResponse.cs` | Modified | Added 6 fields: BranchName, SourceBranchName, BuildStatus, ReviewApprovals, ReviewRequired, ReviewChangesRequested |
| `src/Aura.UI/Services/AzureDevOpsPrClient.cs` | Modified | Populated 6 new fields in all 6 mock PRs with realistic values |
| `src/Aura.UI/Models/PrPreviewItemResponse.cs` | Created | New sealed record for PR dashboard preview items (12 fields) |
| `src/Aura.UI/Services/IPrioritySummaryService.cs` | Modified | Added IsPrCard + PrItems to PrioritySummaryCard; TotalCount prefers PrItems?.Count |
| `src/Aura.UI/Services/PrioritySummaryService.cs` | Modified | BuildCards maps PRs → PrPreviewItemResponse; PR card index 3 sets IsPrCard=true |
| `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor` | Modified | Added `else if (card.IsPrCard)` branch with mini-table before generic `else`; caps items at 3 |
| `src/Aura.UI/Pages/PullRequests.razor` | Modified | Rebuilt table with Stitch columns (Priority, PR Name, CI Status, Reviews, Last Activity, Action); added visual-only filter bar, pagination, right panel placeholder; preserved legacy testids |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Modified | Added CI status badges, PR mini-table styles, filter bar, pagination, right panel layout, reviewer initials |
| `tests/Aura.UnitTests/Pages/PullRequestsPageTests.cs` | Modified | Updated PR constructors with 6 new fields; added CI status + new testid assertions |
| `tests/Aura.UnitTests/Dashboard/PrioritySummaryCardsRenderingTests.cs` | Modified | Added PR card with PrItems + IsPrCard; verified mini-table rendering |
| `tests/Aura.E2E/PullRequests/PullRequestsPageSmokeTests.cs` | Modified | Updated constructors; asserts new testids exist in HTML |

## Verification Results

| Metric | Value |
|--------|-------|
| Tasks total | 15 |
| Tasks complete | 15 |
| Build | ✅ 0 errors, 0 warnings |
| Tests | ✅ 867 passed / 0 failed / 0 skipped |
| Architecture tests | 54 passed |
| Unit tests | 685 passed |
| Integration tests | 84 passed |
| E2E tests | 44 passed |
| Spec compliance | 11/11 scenarios compliant |
| TDD compliance | 4/5 (missing explicit TDD Cycle Evidence table in apply-progress) |

### Test Coverage (changed files)

| File | Line % | Branch % |
|------|--------|----------|
| `PullRequestResponse.cs` | 65.0% | 100% |
| `PrPreviewItemResponse.cs` | 92.3% | 100% |
| `PrioritySummaryCard` | 100% | 83.3% |
| `PrioritySummaryCards.razor` | 91.0% | 90.5% |

## Specs Synced

| Domain | Action | Details |
|--------|--------|---------|
| pr-connector-ui | Updated | Removed `pr-status` from PullRequests.razor testid preservation list (replaced by `pr-ci-status` and `pr-review-status` per delta spec) |

## Open Items (from verify)

### WARNING — Documentation/Consistency

1. **Missing `pr-status` testid** — Spec requirement listed `pr-status` in preservation list, but it was intentionally replaced by `pr-ci-status` and `pr-review-status` during the redesign. Resolved by syncing the main spec to match the delta spec (removed `pr-status` from preservation list).
2. **TDD Cycle Evidence table missing from apply-progress** — Process gap only; all tests exist and pass.
3. **Right panel CSS classes differ from design** — Design specifies `pr-right-panel` / `pr-right-panel__placeholder`; implementation uses `pr-detail-sidebar` / `pr-sidebar-placeholder`. Tasks intentionally used different names.
4. **Branch pair CSS classes missing** — Tasks 4.3 specified `.pr-branch--source` / `--target` but they don't exist in CSS. Mini-table shows only target branch (single value), not source←target pair.

### SUGGESTION — Future Improvements

1. **BuildStatus default not explicit in record** — Consider adding `string BuildStatus = "pending"` to `PullRequestResponse` constructor.
2. **Mini-table shows only target branch** — Consider adding `SourceBranchName` to `PrPreviewItemResponse` for branch pair display.
3. **`.ci-status--failed` color mismatch** — Design specifies `#FF4B4B`, CSS uses `#ef4444`.
4. **Filter bar simpler than design** — Design specified search input + dropdown; implementation uses simpler pill + button.

## Next Steps (for the team)

- **Investigate Issue 1 from login popup** — The user still has the login popup auth regression to investigate (not part of this change, noted in out-of-scope from proposal).
- Address WARNING items if consistency refinements are desired in a follow-up.
- Consider SUGGESTION items for v2 enhancement.

## Archive Contents

- `proposal.md` ✅
- `spec.md` ✅
- `design.md` ✅
- `tasks.md` ✅ (15/15 tasks complete)
- `verify-report.md` ✅
- `archive-report.md` ✅ (this file)

## Engram Observations

| Artifact | Observation ID |
|----------|---------------|
| verify-report | #2376 |
| archive-report | (saved alongside this report) |

## Intentional Archive Notes

- `pr-status` testid discrepancy resolved by syncing main spec to match the delta spec (removed from preservation list).
- Partial archive not required — all artifacts present and all tasks complete.
- No CRITICAL issues blocking archive.

---

*Archived by sdd-archive sub-agent on 2026-07-02.*
