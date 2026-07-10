# Tasks: PR Attention UI Filter

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 200–300 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | single PR |
| Delivery strategy | auto-forecast |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Model fields + filter logic + demo fix + badge UI + tests | PR 1 | Single PR; all layers, tests included |

## Phase 1: Foundation — Model Extensions (TDD)

- [x] 1.1 **RED** — Add test in `tests/Aura.UnitTests/` verifying `PullRequestResponse` defaults `AttentionScope` to `"unknown"` when omitted.
- [x] 1.2 **RED** — Add test verifying `PrPreviewItemResponse` defaults `AttentionScope` to `"unknown"` when omitted.
- [x] 1.3 **GREEN** — Add `string AttentionScope = "unknown"` as last positional param in `src/Aura.UI/Models/PullRequestResponse.cs`.
- [x] 1.4 **GREEN** — Add `string AttentionScope = "unknown"` as last positional param in `src/Aura.UI/Models/PrPreviewItemResponse.cs`.
- [x] 1.5 **VERIFY** — `dotnet test Aura.sln --filter "FullyQualifiedName~PullRequestResponse|FullyQualifiedName~PrPreviewItemResponse"` passes.

## Phase 2: Core Logic — BuildCards Filter (TDD)

- [x] 2.1 **RED** — Add test in `tests/Aura.UnitTests/Dashboard/` with 5 PRs (2 direct, 1 group, 1 both, 1 unknown); assert card contains exactly 4 items and unknown is absent.
- [x] 2.2 **RED** — Add test with all-unknown input and empty list; assert 0 items and empty state.
- [x] 2.3 **RED** — Add test asserting `PrPreviewItemResponse.AttentionScope` matches the source PR's value after mapping.
- [x] 2.4 **GREEN** — In `src/Aura.UI/Services/PrioritySummaryService.cs`, add static `HashSet<string>` allowlist `{"direct","group","both"}`; filter `prs` before mapping; pass `AttentionScope` into `PrPreviewItemResponse` constructor.
- [x] 2.5 **VERIFY** — `dotnet test Aura.sln --filter "FullyQualifiedName~PrioritySummary"` passes.

## Phase 3: Demo Fix — Fixture Attention Scope (TDD)

- [x] 3.1 **RED** — Add test in `tests/Aura.UnitTests/Adapters/Connectors/PrReview/PrReviewConnectorAdapterTests.cs`: null source provider → fixture WorkItems have `Metadata["pr.attentionScope"] == "direct"`.
- [x] 3.2 **RED** — Add test: real source provider → `AttentionScope` is NOT overridden (comes from mapper).
- [x] 3.3 **GREEN** — In `src/Aura.Infrastructure/Adapters/Connectors/PrReview/PrReviewConnectorAdapter.cs`, after `TryMap` in fixture path (`_sourceProvider is null`), set `workItem.Metadata[PrMetadataKeys.AttentionScope] = "direct"`.
- [x] 3.4 **VERIFY** — `dotnet test Aura.sln --filter "FullyQualifiedName~PrReviewConnectorAdapter"` passes.

## Phase 4: UI — Badge Rendering + Styles

- [x] 4.1 In `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor`, add attention badge `<span>` in PR name `<td>`: `"direct"` → `"You"`, `"group"` → `"Group"`, `"both"` → `"Both"`; skip rendering for `"unknown"`.
- [x] 4.2 In `src/Aura.UI/wwwroot/css/stitch-dashboard.css`, add `.attention-badge` base + `.attention-badge--direct`, `--group`, `--both` pill styles (distinct colors per variant).
- [x] 4.3 **VERIFY** — `dotnet build Aura.sln` succeeds with no warnings.

## Phase 5: Full Verification

- [x] 5.1 Run `dotnet test Aura.sln --collect:"XPlat Code Coverage"` — all tests pass, coverage ≥ 80%.
- [x] 5.2 Run `dotnet test Aura.sln --filter "FullyQualifiedName~ArchitectureTests"` — no layer violation.
- [ ] 5.3 Manual check: demo mode shows fixture PRs with "You" badge on dashboard.
