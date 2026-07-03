# Proposal: PR UI — Stitch Design Alignment

## Intent

Align PR connector v1 UI (dashboard card + detail page) with Stitch designs. Dashboard PR card reuses generic inbox template; detail table columns don't match Stitch; model missing 6 fields Stitch requires.

## Scope

### In Scope
- `PullRequestResponse` — add `BranchName`, `BuildStatus`, `ReviewApprovals`, `ReviewRequired`, `ReviewChangesRequested`, `SourceBranchName`
- New `PrPreviewItemResponse` record (replaces `InboxItemPreviewResponse` shoehorning)
- `PrioritySummaryService` — PR card uses `PrPreviewItemResponse`; `IsPrCard` flag on `PrioritySummaryCard`
- `PrioritySummaryCards.razor` — conditional `IsPrCard` branch (Approach 2: co-located, flag-based)
- `PullRequests.razor` — Stitch columns, filter bar, pagination
- `AzureDevOpsPrClient` mock data — populate new fields
- `stitch-dashboard.css` — CI badges, mini-table, filter bar, pagination
- Tests — update unit/E2E for new model shape + columns

### Out of Scope
- Right panel "Rule Engine" (post-v1)
- Real ADO API integration (still mock data)
- Login popup auth regression

## Capabilities

### New Capabilities
- `pr-connector-ui`: PR connector's UI data model, dashboard card, detail page

### Modified Capabilities
- None

## Approach

- **Model**: `PullRequestResponse` gets 6 new fields. Id doubles as PR number.
- **Dashboard card**: `PrPreviewItemResponse` with PR-specific fields. `IsPrCard` flag triggers dedicated rendering in existing cards component.
- **Detail page**: `PullRequests.razor` rebuilt with Stitch table columns + filter + pagination. `data-testid` attributes preserved.
- **CSS**: New classes in `stitch-dashboard.css`. Dark theme already applied.
- **Mock data**: Updated with realistic values. `IAzureDevOpsPrClient` interface unchanged.
- **Tests**: Updated constructors + new column assertions; no removals.

## Affected Areas

| Area | Impact |
|------|--------|
| `src/Aura.UI/Models/PullRequestResponse.cs` | Modified |
| `src/Aura.UI/Models/PrPreviewItemResponse.cs` | New |
| `src/Aura.UI/Services/PrioritySummaryService.cs` | Modified |
| `src/Aura.UI/Services/IPrioritySummaryService.cs` | Modified |
| `src/Aura.UI/Services/AzureDevOpsPrClient.cs` | Modified |
| `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor` | Modified |
| `src/Aura.UI/Pages/PullRequests.razor` | Modified |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Modified |
| `tests/Aura.UnitTests/Pages/PullRequestsPageTests.cs` | Modified |
| `tests/Aura.E2E/PullRequests/PullRequestsPageSmokeTests.cs` | Modified |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Narrow viewports break cards | Med | Test at 320px+; CSS `min-width` |
| Test breakage from new fields | Low | Preserve `data-testid`; update stubs |
| E2E smoke test HTML structure shift | Low | Keep `pr-row`, `pr-list`, `pr-pending-count` |

## Rollback Plan

`git revert` on the merge commit. `IAzureDevOpsPrClient` unchanged — zero consumer churn. Model add-only — old consumers compile.

## Dependencies

- None — self-contained in `Aura.UI`

## Success Criteria

- [ ] All 6 new `PullRequestResponse` fields populated in mock data
- [ ] Dashboard PR card renders PR-specific layout (not inbox template)
- [ ] Detail page shows Stitch columns with filter bar + pagination
- [ ] Existing unit + E2E tests pass with updated models
- [ ] `dotnet test Aura.sln` green
