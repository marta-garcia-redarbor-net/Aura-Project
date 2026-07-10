# Proposal: Azure DevOps PR Reviewer Identity & Attention Scope

## Intent

Aura's Azure DevOps PR path only carries reviewer **display names** and vote summaries, so it cannot tell whether a PR needs the current user's attention. Users must open each PR to learn if they are a direct reviewer, a member of a required reviewer group, both, or neither. This change captures stable reviewer identity at ingestion and derives an `attentionScope` (`direct` | `group` | `both` | `none` | `unknown`) so a PR's relevance to the logged-in user is explicit and verifiable.

## Scope

### In Scope
- Enrich `PrReviewDto` and ADO parsing to capture reviewer identity metadata: canonical `oid` (when available), display name, and raw ADO identifier + `isContainer`/group flag.
- Persist reviewer identity signals via `PrReviewWorkItemMapper` as `pr.*` metadata keys.
- Derive `attentionScope` in `PullRequestMapper` by matching the current user's canonical `oid` against reviewer/group metadata; expose one enum field on `PullRequestDto`.

### Out of Scope
- Full UI rendering (attention labels/badges in `PrioritySummaryService`) — deferred to a follow-up UI slice.
- Live Graph group-membership expansion beyond identity already present in the ADO payload.
- Non-ADO providers and priority-score re-weighting.

## Capabilities

### New Capabilities
- `pr-reviewer-identity`: Ingestion captures stable reviewer identity metadata and derives per-user `attentionScope` from canonical `oid`, not display-name matching.

### Modified Capabilities
- `pull-request-api`: `PullRequestDto` gains an `AttentionScope` field mapped from persisted reviewer identity metadata.

## Approach

Follow exploration Approach 1: keep provider parsing and any identity resolution in **Infrastructure** ingestion, persist normalized metadata on `WorkItem`, and let **Application** only consume metadata to compute a simple derived enum. Match on canonical `oid`; use `display-name` only as a last-resort, clearly-marked fallback. No SDK types cross into Application/Domain.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Aura.Infrastructure/.../AzureDevOps/AzureDevOpsPrProvider.cs` | Modified | Parse reviewer identity fields |
| `Aura.Infrastructure/.../PrReview/PrReviewDto.cs` | Modified | Richer reviewer identity model |
| `Aura.Infrastructure/.../PrReview/PrReviewWorkItemMapper.cs` | Modified | Persist identity metadata keys |
| `Aura.Application/Mapping/PullRequestMapper.cs` | Modified | Derive `attentionScope` |
| `Aura.Application/Models/PullRequestDto.cs` | Modified | Add `AttentionScope` field |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| ADO payload lacks stable `oid`/group fields | Med | Explicit `unknown`/`none` fallback; no false positives |
| Positional records source-break on new field | Med | Append field last with safe default |
| Display-name reliance creeps in | Low | `oid` primary; fallback flagged in metadata |

## Rollback Plan

Revert the change commits. New `pr.*` metadata keys are additive and ignored by existing readers; `AttentionScope` defaults to `unknown`, so removing the mapping restores prior behavior with no data migration.

## Dependencies

- Canonical `oid` resolution pattern from `GraphClientFactory` (existing Infrastructure convention).

## Success Criteria

- [ ] Reviewer identity metadata (`oid` when available) persisted at ingestion.
- [ ] `PullRequestDto.AttentionScope` returns `direct`/`group`/`both`/`none`/`unknown` from metadata, never SDK types.
- [ ] Attention derivation uses `oid` as primary key; display-name only as flagged fallback.
- [ ] Clean Architecture tests pass — no Infrastructure/SDK leakage into Application.
