# Tasks: Azure DevOps PR Reviewer Identity & Attention Scope

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 560–740 |
| Configured review budget | 800 lines |
| 400-line budget risk | High |
| 800-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR with 3 work units |
| Delivery strategy | auto-forecast |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Application contracts + attention derivation | PR 1 | RED/GREEN/REFACTOR in mapper tests; verifies direct/group/both/none/unknown/fallback |
| 2 | ADO reviewer identity ingestion + metadata persistence | PR 1 | Keep SDK parsing in Infrastructure; verify `pr.reviewer.<n>.*` keys |
| 3 | API wiring + integration + architecture guard | PR 1 | Endpoint passes current user oid; NetArchTest prevents SDK leak to Application/Domain |

### Risk Notes

- Positional `PullRequestDto` change can break callers if `AttentionScope` is not appended last.
- ADO payload may omit reviewer ids; fallback behavior MUST stay explicit (`unknown` or flagged `displayName`).
- Clean Architecture risk if ADO/Graph types leak outside Infrastructure.

## Phase 1: Foundation (Application Contracts)

- [x] 1.1 RED: Extend `tests/Aura.UnitTests/Application/Mapping/PullRequestMapperTests.cs` with AttentionScope matrix scenarios from spec.
- [x] 1.2 GREEN: Create `src/Aura.Application/Models/PrMetadataKeys.cs` for `pr.attentionScope` and reviewer identity key builders.
- [x] 1.3 GREEN: Update `src/Aura.Application/Models/PullRequestDto.cs` to append `AttentionScope` as last field with safe default.
- [x] 1.4 REFACTOR: Update `src/Aura.Application/Mapping/PullRequestMapper.cs` to use `PrMetadataKeys`, accept `string? currentUserOid = null`, and keep pure string-based derivation.

## Phase 2: Infrastructure Identity Capture

- [x] 2.1 RED: Create `tests/Aura.UnitTests/Infrastructure/PrReviewWorkItemMapperIdentityTests.cs` for oid-present, oid-null, and `isContainer` persistence scenarios.
- [x] 2.2 GREEN: Create `src/Aura.Infrastructure/Adapters/Connectors/PrReview/PrReviewerIdentity.cs` and extend `PrReviewDto.cs` with `ReviewerIdentities` while preserving `Reviewers`.
- [x] 2.3 GREEN: Modify `src/Aura.Infrastructure/Adapters/Connectors/AzureDevOps/AzureDevOpsPrProvider.cs` to parse reviewer `id` and `isContainer` into `ReviewerIdentities`.
- [x] 2.4 REFACTOR: Modify `src/Aura.Infrastructure/Adapters/Connectors/PrReview/PrReviewWorkItemMapper.cs` and `PrReviewConnectorAdapter.cs` to emit `pr.reviewer.<n>.oid|displayName|isContainer` and keep backward-compatible metadata.

## Phase 3: API Wiring and Architecture Guard

- [x] 3.1 RED: Extend `tests/Aura.IntegrationTests/PullRequests/PullRequestsEndpointTests.cs` to assert `attentionScope` mapping and `unknown` default behavior.
- [x] 3.2 GREEN: Modify `src/Aura.Api/Endpoints/PullRequestsEndpoints.cs` to resolve current user oid via `ICurrentUserService` and pass it to `PullRequestMapper.ToDto`.
- [x] 3.3 GREEN: Create `tests/Aura.ArchitectureTests/ReviewerIdentityArchitectureTests.cs` to block `Microsoft.TeamFoundation.*` dependencies in Application/Domain.

## Phase 4: Verification

- [x] 4.1 Run targeted RED→GREEN suite: `dotnet test tests/Aura.UnitTests --filter "PullRequestMapperTests|PrReviewWorkItemMapperIdentityTests"`.
- [x] 4.2 Run boundary and API verification: `dotnet test tests/Aura.ArchitectureTests --filter "ReviewerIdentityArchitectureTests|PullRequestApiArchitectureTests"` and `dotnet test tests/Aura.IntegrationTests --filter "PullRequestsEndpointTests"`.
- [x] 4.3 Run regression safety net: `dotnet test Aura.sln` and confirm no spec-scenario regressions in PR mapping.

## Phase 5: Verify Remediation (Spec Gap Closure)

- [x] 5.1 RED: Extend `PullRequestMapperTests` and `PullRequestsEndpointTests` to prove real display-name fallback path and metadata persistence (`pr.attentionScope`, `pr.attentionScope.fallback`).
- [x] 5.2 GREEN: Update `PullRequestMapper` + `PullRequestsEndpoints` to pass current user display name for fallback and persist derived/fallback metadata while preserving OID-first precedence.
- [x] 5.3 RED: Add `AzureDevOpsPrProvider` runtime parsing tests for reviewer `id` present, `id` missing, and `isContainer=true`.
- [x] 5.4 GREEN: Keep provider parsing compliant and prove with targeted unit/integration runs for mapper/provider/endpoint slices.
