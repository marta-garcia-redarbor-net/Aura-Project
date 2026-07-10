# Proposal: PR Attention UI Filter

## Intent

API already computes `AttentionScope` per PR, but the UI ignores it — all PRs appear regardless of relevance. Users waste cognitive load scanning PRs that don't need them.

## Scope

### In Scope
- Add `AttentionScope` to `PullRequestResponse` and `PrPreviewItemResponse`
- Filter dashboard PR card to `AttentionScope ∈ {direct, group, both}`
- Attention-reason badge per row in the dashboard mini-table
- Fix demo mode: fixture PRs survive the filter despite fake OIDs

### Out of Scope
- Detail page (`PullRequests.razor`) attention filter — deferred
- Priority-score re-weighting based on attention
- Ingestion changes (done in `azure-devops-pr-reviewer-identity`)

## Capabilities

### New Capabilities
- `pr-attention-filter`: UI filter, demo-mode fix, and visual indicator for PR attention scope

### Modified Capabilities
- `pr-connector-ui`: `PullRequestResponse`/`PrPreviewItemResponse` gain `AttentionScope`; `BuildCards` applies filter; dashboard renders attention badge
- `demo-mode`: Fixture PRs bypass OID matching to survive the filter

## Approach

1. Add `string AttentionScope` (`"unknown"` default) as last param on `PullRequestResponse` and `PrPreviewItemResponse`.
2. In `BuildCards()`, filter `prs` before mapping: keep `AttentionScope ∈ {direct, group, both}`.
3. Map `AttentionScope` into `PrPreviewItemResponse`; render pill in dashboard table ("You"/"Group"/"Both").
4. **Demo fix**: In `PrReviewConnectorAdapter.ExecuteAsync`, when using `_fixtureProvider`, pre-set `pr.attentionScope=direct` on work items after mapping (tag fixture WorkItem metadata).

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `PullRequestResponse.cs` | Modified | Add `AttentionScope` field |
| `PrPreviewItemResponse.cs` | Modified | Add `AttentionScope` field |
| `PrioritySummaryService.cs` | Modified | Filter + map attention scope |
| `PrReviewConnectorAdapter.cs` | Modified | Fix demo fixture OID mismatch |
| `PrioritySummaryCards.razor` | Modified | Attention badge rendering |
| `stitch-dashboard.css` | Modified | Badge pill styles |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Positional record breaks on new last field | Low | Default `"unknown"`; existing callers compile |
| Demo fix couples fixtures to attention logic | Low | Confined to `fixtureProvider` path only |
| "Group" badge unclear | Med | Use explicit labels ("via group"); refine post-ship |

## Rollback Plan

Revert commit. `AttentionScope` defaults to `"unknown"` — removing filter restores prior behavior. Demo fix is additive; removing it returns to pre-existing bug.

## Dependencies

- `PullRequestDto.AttentionScope` (already deployed — no API change needed)

## Success Criteria

- [ ] Dashboard PR card only shows PRs with `AttentionScope ∈ {direct, group, both}`
- [ ] Each visible PR shows a visual reason badge
- [ ] Demo mode fixture PRs visible on dashboard (regression test)
- [ ] All existing PR card tests pass with new field
