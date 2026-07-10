# Design: PR Attention UI Filter

## Technical Approach

Propagate the existing `AttentionScope` value from `PullRequestDto` (Application layer, already deployed) through the UI layer, filter the dashboard PR card to relevant scopes only, render a visual badge per row, and fix demo-mode fixtures so they survive the filter.

No API changes required — `PullRequestDto.AttentionScope` is already serialized by the endpoint.

## Architecture Decisions

| Decision | Options | Tradeoff | Choice |
|----------|---------|----------|--------|
| UI model extension | Add field vs. new wrapper record | Positional record with default preserves all existing callers | Add `string AttentionScope = "unknown"` as last param on both records |
| Filter location | `BuildCards()` vs. Razor component vs. API query param | Service-level filter keeps razor dumb, is testable, and doesn't change API contract | Filter in `PrioritySummaryService.BuildCards()` before mapping |
| Demo fixture fix | Set scope in `PrReviewDto` vs. post-mapping metadata injection vs. change OID matching | Post-mapping metadata write is confined to fixture path, reuses existing `DeriveAttentionScope` precomputed check | Set `PrMetadataKeys.AttentionScope = "direct"` on WorkItem metadata after `TryMap` when `_sourceProvider is null` |
| Badge rendering | Separate column vs. inline in name cell | Inline in name `<td>` avoids table reflow; follows existing `demo-new-badge` pattern | Render badge `<span>` after author line in name cell |

## Data Flow

```
API (PullRequestDto.AttentionScope)
  │  JSON serialized
  ▼
PullRequestsApiClient → PullRequestResponse.AttentionScope
  │
  ▼
PrioritySummaryService.BuildCards()
  │  filter: scope ∈ {direct, group, both}
  │  map → PrPreviewItemResponse.AttentionScope
  ▼
PrioritySummaryCards.razor
  │  render badge pill in PR name cell
  ▼
stitch-dashboard.css  →  .attention-badge--{direct,group,both}
```

Demo path:
```
PrReviewConnectorAdapter (fixture mode, _sourceProvider == null)
  │  _fixtureProvider() → PrReviewDto[]
  │  _mapper.TryMap() → WorkItem
  │  workItem.Metadata["pr.attentionScope"] = "direct"  ← FIX
  ▼
WorkItem buffered → API reads metadata → PullRequestMapper precomputed check returns "direct"
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.UI/Models/PullRequestResponse.cs` | Modify | Add `string AttentionScope = "unknown"` as last positional param |
| `src/Aura.UI/Models/PrPreviewItemResponse.cs` | Modify | Add `string AttentionScope = "unknown"` as last positional param |
| `src/Aura.UI/Services/PrioritySummaryService.cs` | Modify | Filter `prs` by attention scope before mapping; pass `AttentionScope` into `PrPreviewItemResponse` constructor |
| `src/Aura.Infrastructure/Adapters/Connectors/PrReview/PrReviewConnectorAdapter.cs` | Modify | After `TryMap` in fixture path, set `workItem.Metadata[PrMetadataKeys.AttentionScope] = "direct"` |
| `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor` | Modify | Add attention badge `<span>` in PR name `<td>` with scope-dependent label and CSS class |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Modify | Add `.attention-badge` base + `.attention-badge--direct`, `--group`, `--both` pill styles |

## Interfaces / Contracts

No new interfaces. Existing records extended:

```csharp
// PullRequestResponse.cs — add last param
public sealed record PullRequestResponse(
    // ... existing 18 params ...
    int ReviewChangesRequested,
    string AttentionScope = "unknown");

// PrPreviewItemResponse.cs — add last param
public sealed record PrPreviewItemResponse(
    // ... existing 13 params ...
    string Priority,
    string AttentionScope = "unknown");
```

Badge label mapping (inline in razor):

| Scope | Label | CSS class |
|-------|-------|-----------|
| `direct` | You | `attention-badge--direct` |
| `group` | Group | `attention-badge--group` |
| `both` | Both | `attention-badge--both` |
| `unknown` | (none) | (not rendered) |

Filter allowlist: `HashSet` of `{"direct", "group", "both"}` — static, allocated once.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `BuildCards` filters out `unknown`/`none` scope PRs | xUnit test with mixed-scope input, assert count and absence |
| Unit | `BuildCards` maps `AttentionScope` into `PrPreviewItemResponse` | xUnit test asserting field propagation |
| Unit | Demo fixture path sets `pr.attentionScope` metadata | xUnit test on `PrReviewConnectorAdapter` with null source provider |
| Unit | Real data path does NOT override attention scope | xUnit test on adapter with mock source provider |
| Unit | Default `AttentionScope` on both records | xUnit test constructing without the param |
| Architecture | No new layer dependency violations | Existing NetArchTest rules cover this |

## Migration / Rollout

No migration required. `AttentionScope` defaults to `"unknown"` on both records — removing the filter reverts to prior behavior. Demo fix is additive and confined to the fixture code path.

## Rollback Safety

Single-commit revert. The `AttentionScope` field defaults to `"unknown"` so:
- Removing the filter in `BuildCards` restores all PRs appearing (pre-change behavior).
- Removing the demo metadata write returns fixtures to their pre-existing bug state (no regression for real data).
- CSS classes are additive — no existing styles removed.

## Open Questions

None.
