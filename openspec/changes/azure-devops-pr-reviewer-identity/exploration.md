# Exploration: Azure DevOps PR reviewer identity

### Current State
Aura's Azure DevOps PR path only carries reviewer display names and vote-like summary data. `AzureDevOpsPrProvider` reads ADO reviewers as `DisplayName` + `Vote`, `PrReviewDto` stores only display-name reviewers, and `PrReviewWorkItemMapper` persists `pr.reviewers` plus `pr.reviewerCount`. Downstream, `PullRequestMapper`/`PullRequestDto`/`PullRequestResponse` only expose reviewer count and review summary fields, so the system cannot tell whether a PR is directly assigned to the logged-in user, assigned to one of their groups, both, or neither. Identity handling elsewhere in Aura already prefers canonical `oid` for user identity, and Graph connector code shows that object-id matching belongs in infrastructure-bound token/identity resolution, not in Application.

### Affected Areas
- `src/Aura.Infrastructure/Adapters/Connectors/AzureDevOps/AzureDevOpsPrProvider.cs` — current ADO payload parsing drops reviewer identity metadata.
- `src/Aura.Infrastructure/Adapters/Connectors/PrReview/PrReviewDto.cs` — reviewer model is display-name only; needs richer identity fields.
- `src/Aura.Infrastructure/Adapters/Connectors/PrReview/PrReviewWorkItemMapper.cs` — writes review metadata to `WorkItem`; this is the right place to persist reviewer identity signals.
- `src/Aura.Application/Mapping/PullRequestMapper.cs` — maps persisted metadata to API DTOs; needs new attention semantics derived from metadata, not SDK types.
- `src/Aura.Application/Models/PullRequestDto.cs` — contract for PR API responses; likely needs reviewer-attention fields.
- `src/Aura.UI/Models/PullRequestResponse.cs` — UI response contract must mirror any new API fields.
- `src/Aura.UI/Services/PrioritySummaryService.cs` — PR card rendering depends on review state and will need attention labels/flags.
- `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` — establishes the existing pattern for canonical `oid` matching in Infrastructure.

### Approaches
1. **Persist identity-rich reviewer records in ingestion** — Extend `PrReviewDto` and `PrReviewWorkItemMapper` to capture reviewer identity metadata (canonical `oid` when available, plus display name and raw ADO identifiers), then derive `attentionScope` (`direct`/`group`/`both`/`none`) when mapping to API/UI DTOs.
   - Pros: keeps provider-specific parsing in Infrastructure; preserves Clean Architecture; canonical identity can be stored once and reused; enables future group-aware logic without re-reading ADO.
   - Cons: depends on what ADO actually returns in the PR reviewer payload; if ADO does not expose stable identity/group fields, some cases remain ambiguous.
   - Effort: Medium

2. **Resolve reviewer membership at read time** — Keep ingestion mostly as-is and add an Application service that resolves the current user against reviewer metadata and group membership whenever PRs are read.
   - Pros: less change to ingestion path; can evolve membership logic independently.
   - Cons: pushes identity resolution into Application, which clashes with current boundary rules; risks N+1 lookups and repeated Graph/group calls; harder to cache and audit.
   - Effort: High

### Recommendation
Use Approach 1. Reviewer identity resolution should live in Infrastructure ingestion, where external ADO payloads and any group/identity lookups belong. Application should only consume normalized reviewer-attention metadata and expose a simple derived flag/enum for UI consumption.

### Risks
- Azure DevOps may not expose enough reviewer identity data in the current endpoint to fully distinguish direct vs group reviewers without an extra lookup.
- If canonical `oid` is unavailable for some reviewers, the model needs explicit fallback semantics (`unknown`/`none`) to avoid false positives.
- The existing API/UI contracts are positional records, so adding fields must be done carefully to avoid source breaks.

### Ready for Proposal
Yes — the evidence supports a focused proposal to enrich PR reviewer identity at ingestion and derive attention semantics downstream.
