# Design: Azure DevOps PR Reviewer Identity & Attention Scope

## Technical Approach

Enrich ADO PR ingestion with stable reviewer identity (`oid`, `isContainer`) persisted as
`pr.reviewer.<n>.*` metadata keys in `WorkItem`. At request time, `PullRequestMapper` derives
`AttentionScope` from those keys matched against the current user's `oid`, surfaced via the
existing `ICurrentUserService` port. No ADO or Graph SDK types cross into `Application` or
`Domain`. `WorkItem.Metadata` is `IReadOnlyDictionary` and is never mutated by the mapper;
derivation is inline with a pre-computed-key read-first path.

## Architecture Decisions

| # | Option | Tradeoff | Decision |
|---|--------|----------|----------|
| A | Group membership: resolve via Graph API at ingestion vs. flag `isContainer` only | Graph expansion crosses ADO provider boundary, adds per-PR latency, and introduces infra coupling. Spec marks live expansion out of scope. | **Flag `isContainer` only.** Group match = current user oid matches a reviewer record with `isContainer=true`. |
| B | Current user context: inject `ICurrentUserService` into `PullRequestMapper` (make non-static) vs. pass `string? currentUserOid` as a parameter | Injecting a service into a static mapper couples the mapping contract to infrastructure resolution and complicates unit tests. | **Pass `string? currentUserOid = null` to `ToDto`.** Endpoint resolves oid via `ICurrentUserService`; mapper remains pure and parameter-injectable. |
| C | WorkItem.Metadata write: cast underlying `Dictionary` and mutate vs. derive inline | `WorkItem.Metadata` is `IReadOnlyDictionary<string,string>`. Mutating via cast violates the contract and would break if the type is ever wrapped. | **Derive inline.** Mapper reads `pr.attentionScope` first (pre-computed override), then derives from `pr.reviewer.*` keys. Spec clause "written to Metadata" is a deferred concern for a future async enrichment worker. |
| D | `PrReviewDto.Reviewers`: replace `IReadOnlyList<string>` vs. add `ReviewerIdentities` field | Replacement breaks fixture initialization syntax in `PrReviewConnectorAdapter.LoadDefaultFixtures`. | **Add `ReviewerIdentities: IReadOnlyList<PrReviewerIdentity>?`.** Keep `Reviewers` for backward compat. Mapper reads from `ReviewerIdentities` when populated; `Reviewers` field continues to feed the display-name fallback and `pr.reviewers` metadata key. |
| E | `AttentionScope` type in `PullRequestDto`: string vs. enum | Existing DTO uses `string` for all status-like fields (`Status`, `Priority`, `BuildStatus`). Enum adds type safety but requires conversion on both read and serialize paths. | **String.** Consistent with existing pattern. Magic strings prevented by `PrMetadataKeys` static constants in `Aura.Application.Models`. |

## Data Flow

```
Ingestion (connector execution time)

  ADO REST payload
    └── AzureDevOpsPrProvider
          parses { id, displayName, isContainer } per reviewer
          → PrReviewDto { ReviewerIdentities: [{ Oid?, DisplayName, IsContainer }] }
              └── PrReviewWorkItemMapper.BuildMetadata
                    writes pr.reviewer.<n>.oid (omit when null)
                    writes pr.reviewer.<n>.displayName
                    writes pr.reviewer.<n>.isContainer
                    → WorkItem.Metadata (IReadOnlyDictionary — immutable after construction)

Request (per GET /api/pull-requests)

  PullRequestsEndpoints.GetPullRequestsAsync
    resolves currentUserOid via ICurrentUserService.GetCurrentUser()?.Oid
      └── ordered.Select(wi => PullRequestMapper.ToDto(wi, currentUserOid))
              reads pr.attentionScope if present  ← pre-computed override path
              else derives from pr.reviewer.* keys + currentUserOid
              writes pr.attentionScope.fallback="displayName" when oid unavailable
              → PullRequestDto { AttentionScope }
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Adapters/Connectors/PrReview/PrReviewerIdentity.cs` | Create | `internal sealed record PrReviewerIdentity(string? Oid, string DisplayName, bool IsContainer)` |
| `src/Aura.Infrastructure/Adapters/Connectors/PrReview/PrReviewDto.cs` | Modify | Add `ReviewerIdentities: IReadOnlyList<PrReviewerIdentity>?`; keep `Reviewers` |
| `src/Aura.Infrastructure/Adapters/Connectors/AzureDevOps/AzureDevOpsPrProvider.cs` | Modify | Add `Id` + `IsContainer` to `AdoReviewer`; populate `ReviewerIdentities` in DTO construction |
| `src/Aura.Infrastructure/Adapters/Connectors/PrReview/PrReviewWorkItemMapper.cs` | Modify | Write per-reviewer identity keys using `PrMetadataKeys` helpers; keep `pr.reviewerCount` |
| `src/Aura.Application/Models/PrMetadataKeys.cs` | Create | Static constants + index helpers: `AttentionScope`, `AttentionScopeFallback`, `ReviewerOid(n)`, `ReviewerDisplayName(n)`, `ReviewerIsContainer(n)` |
| `src/Aura.Application/Models/PullRequestDto.cs` | Modify | Append `string AttentionScope` as last positional parameter; default `"unknown"` |
| `src/Aura.Application/Mapping/PullRequestMapper.cs` | Modify | Add `string? currentUserOid = null` param; add `DeriveAttentionScope` private method; read pre-computed key first |
| `src/Aura.Api/Endpoints/PullRequestsEndpoints.cs` | Modify | Add `ICurrentUserService` DI parameter; resolve oid; switch method group to lambda |
| `tests/Aura.ArchitectureTests/ReviewerIdentityArchitectureTests.cs` | Create | NetArchTest rule: no `Microsoft.TeamFoundation.*` in `Aura.Application` or `Aura.Domain` |
| `tests/Aura.UnitTests/Application/Mapping/PullRequestMapperTests.cs` | Modify | Add `AttentionScope` scenarios: direct/group/both/none/unknown/fallback/pre-computed override |
| `tests/Aura.UnitTests/Infrastructure/PrReviewWorkItemMapperIdentityTests.cs` | Create | Identity key persistence scenarios per spec (oid present, oid null, isContainer) |

## Interfaces / Contracts

```csharp
// NEW — Infrastructure only (PrReview namespace)
internal sealed record PrReviewerIdentity(string? Oid, string DisplayName, bool IsContainer);

// NEW — Application.Models (accessible from Infrastructure via project reference)
public static class PrMetadataKeys
{
    public const string AttentionScope         = "pr.attentionScope";
    public const string AttentionScopeFallback = "pr.attentionScope.fallback";

    public static string ReviewerOid(int i)          => $"pr.reviewer.{i}.oid";
    public static string ReviewerDisplayName(int i)  => $"pr.reviewer.{i}.displayName";
    public static string ReviewerIsContainer(int i)  => $"pr.reviewer.{i}.isContainer";
}

// MODIFIED — Application.Models (AttentionScope appended last — no positional break)
public sealed record PullRequestDto(
    /* ...existing fields unchanged... */
    int? PriorityScore,
    string AttentionScope);        // ← new; defaults to "unknown" when absent

// MODIFIED — Application.Mapping
public static PullRequestDto ToDto(WorkItem item, string? currentUserOid = null);
```

### AttentionScope derivation priority (pure string logic in `DeriveAttentionScope`)

| Condition | Result |
|-----------|--------|
| `pr.attentionScope` key already in metadata | return key value (pre-computed override) |
| `currentUserOid` null | `"unknown"` |
| oid matches non-container reviewer | `"direct"` |
| oid matches container reviewer | `"group"` |
| oid matches both | `"both"` |
| display-name match only (oid absent) | `"direct"` + set `pr.attentionScope.fallback` |
| no match | `"none"` |

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `DeriveAttentionScope`: all six outcomes | Pure function, no mocks, parametrized |
| Unit | Pre-computed `pr.attentionScope` key wins over derivation | WorkItem with key already set |
| Unit | `PrReviewWorkItemMapper` writes identity keys; omits oid when null | Assert metadata keys present/absent per spec scenarios |
| Architecture | No `Microsoft.TeamFoundation.*` in `Aura.Application` / `Aura.Domain` | NetArchTest, follows `IngestionArchitectureTests` pattern |
| Integration | GET `/api/pull-requests` returns `attentionScope` field | Extend `PullRequestsEndpointTests` with mock `ICurrentUserService` |

## Migration / Rollout

Additive only. New metadata keys (`pr.reviewer.*`, `pr.attentionScope`) are ignored by existing
readers. `PullRequestDto.AttentionScope` defaults to `"unknown"` when the key is absent.
`Rollback = revert commits`; `AttentionScope` reverts to absent from the record and prior
serialization is unaffected. No data migration required.

## Open Questions

None — all blocking decisions resolved above.
