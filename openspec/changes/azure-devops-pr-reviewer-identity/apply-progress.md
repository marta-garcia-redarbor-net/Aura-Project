# Apply Progress: azure-devops-pr-reviewer-identity

## Mode

Strict TDD

## Completed Tasks

- [x] 1.1 RED: Extend `PullRequestMapperTests` with AttentionScope matrix scenarios.
- [x] 1.2 GREEN: Create `PrMetadataKeys` constants and helpers.
- [x] 1.3 GREEN: Append `AttentionScope` to `PullRequestDto` as the last positional field.
- [x] 1.4 REFACTOR: Refactor `PullRequestMapper` to use metadata keys and derive attention scope from string metadata.
- [x] 2.1 RED: Add infrastructure tests for reviewer identity metadata persistence.
- [x] 2.2 GREEN: Add `PrReviewerIdentity` and extend `PrReviewDto` with `ReviewerIdentities`.
- [x] 2.3 GREEN: Parse ADO reviewer `id` and `isContainer` into `ReviewerIdentities`.
- [x] 2.4 REFACTOR: Persist `pr.reviewer.<n>.oid|displayName|isContainer` metadata and keep compatibility fields.
- [x] 3.1 RED: Extend pull-requests integration tests for `attentionScope` mapping and `unknown` default.
- [x] 3.2 GREEN: Wire `ICurrentUserService` into pull-requests endpoint mapping.
- [x] 3.3 GREEN: Add architecture guard against `Microsoft.TeamFoundation.*` dependencies in Application/Domain.
- [x] 5.1 RED: Extend mapper + endpoint tests to prove reachable display-name fallback and required metadata persistence.
- [x] 5.2 GREEN: Update mapper + endpoint flow to pass current user display name and persist `pr.attentionScope` / `pr.attentionScope.fallback`.
- [x] 5.3 RED: Add Azure DevOps provider runtime parsing tests for reviewer `id` present, `id` missing, and `isContainer=true`.
- [x] 5.4 GREEN: Run targeted remediation verification for mapper/provider/endpoint slices.

## Remaining Tasks

- None.

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.UnitTests/Application/Mapping/PullRequestMapperTests.cs` | Unit | ✅ Baseline command executed (build/test pipeline initially failed only on expected missing API) | ✅ Added failing AttentionScope scenarios first | ✅ `dotnet test ...PullRequestMapperTests` passed | ✅ direct/group/both/none/unknown/fallback/precomputed cases | ✅ Mapper extraction/derivation constants and loops cleaned |
| 1.2 | `tests/Aura.UnitTests/Application/Mapping/PullRequestMapperTests.cs` | Unit | ✅ | ✅ Tests already referenced new metadata behavior | ✅ New `PrMetadataKeys` compiled and tests passed | ✅ Multiple key families covered | ✅ Consolidated key usage in mapper |
| 1.3 | `tests/Aura.UnitTests/Application/Mapping/PullRequestMapperTests.cs` | Unit | ✅ | ✅ Tests expected `AttentionScope` field usage | ✅ DTO updated (field appended last + default) and tests passed | ✅ default + mapped scope paths validated | ✅ Kept additive-safe positional order |
| 1.4 | `tests/Aura.UnitTests/Application/Mapping/PullRequestMapperTests.cs` | Unit | ✅ | ✅ Derivation scenarios written first | ✅ Mapper implementation now passes | ✅ OID + fallback + precomputed override matrix | ✅ Pure string metadata derivation with helper methods |
| 2.1 | `tests/Aura.UnitTests/Infrastructure/PrReviewWorkItemMapperIdentityTests.cs` | Unit | ✅ Baseline PR mapper tests green before infra edits | ✅ New identity persistence tests added first | ✅ Identity tests pass | ✅ oid present / oid null / isContainer + reviewerCount | ✅ Assertions aligned to metadata key helpers |
| 2.2 | `tests/Aura.UnitTests/Infrastructure/PrReviewWorkItemMapperIdentityTests.cs` | Unit | ✅ | ✅ Tests depended on `PrReviewerIdentity` and `ReviewerIdentities` | ✅ Types added and tests passed | ✅ Non-null and null-oid variants | ✅ Backward-compatible `Reviewers` preserved |
| 2.3 | `tests/Aura.UnitTests/Infrastructure/PrReviewWorkItemMapperIdentityTests.cs` | Unit | ✅ | ✅ Tests required populated identity metadata from provider path | ✅ ADO parser now maps `Id` + `IsContainer` | ✅ Container vs non-container reviewer cases | ✅ Parser kept inside Infrastructure boundary |
| 2.4 | `tests/Aura.UnitTests/Infrastructure/PrReviewWorkItemMapperIdentityTests.cs` | Unit | ✅ | ✅ Tests asserted `pr.reviewer.<n>.*` keys | ✅ Mapper/adapter emit identity keys and pass tests | ✅ OID omission and container values both verified | ✅ Existing `pr.reviewers` and counters retained |
| 3.1 | `tests/Aura.IntegrationTests/PullRequests/PullRequestsEndpointTests.cs` | Integration | ✅ Existing pull-request integration tests green before wiring | ✅ Added `attentionScope` + `unknown` tests first | ✅ `dotnet test ...PullRequestsEndpointTests` passed | ✅ Matched identity and missing-oid contexts | ✅ Shared auth client helper extended safely |
| 3.2 | `tests/Aura.IntegrationTests/PullRequests/PullRequestsEndpointTests.cs` | Integration | ✅ | ✅ Integration tests depended on current user OID flow | ✅ Endpoint now resolves `currentUserService.GetCurrentUser()?.Oid` and passes to mapper | ✅ direct and unknown branches exercised | ✅ Maintained endpoint filtering/order behavior |
| 3.3 | `tests/Aura.ArchitectureTests/ReviewerIdentityArchitectureTests.cs` | Architecture | ✅ Existing architecture suite healthy for target slice | ✅ New boundary tests added first | ✅ `dotnet test ...ReviewerIdentityArchitectureTests|PullRequestApiArchitectureTests` passed | ✅ Application and Domain both checked | ✅ Reused architecture test formatting helpers |
| 4.1 | N/A (verification task) | Verification | ✅ | ✅ N/A (execution task) | ✅ `dotnet test tests/Aura.UnitTests --filter "PullRequestMapperTests|PrReviewWorkItemMapperIdentityTests"` passed (40 tests) | ➖ Single verification command | ➖ None needed |
| 4.2 | N/A (verification task) | Verification | ✅ | ✅ N/A (execution task) | ✅ Integration slice (8/8) + architecture slice (4/4) passed | ➖ Single verification command pair | ➖ None needed |
| 4.3 | N/A (verification task) | Verification | ✅ | ✅ N/A (execution task) | ✅ `dotnet test Aura.sln` passed (all test projects green) | ➖ Single verification command | ➖ None needed |
| 5.1 | `tests/Aura.UnitTests/Application/Mapping/PullRequestMapperTests.cs`, `tests/Aura.IntegrationTests/PullRequests/PullRequestsEndpointTests.cs` | Unit + Integration | ✅ Existing mapper and endpoint slices were green before remediation edits | ✅ Added failing/insufficient fallback-coverage tests first (mapper required new API shape + endpoint fallback path absent) | ✅ `dotnet test ...PullRequestMapperTests` and `dotnet test ...PullRequestsEndpointTests` passed after implementation | ✅ Added fallback path + metadata persistence + unknown branch coverage | ✅ Kept assertions focused on observable metadata/DTO behavior |
| 5.2 | `tests/Aura.UnitTests/Application/Mapping/PullRequestMapperTests.cs`, `tests/Aura.IntegrationTests/PullRequests/PullRequestsEndpointTests.cs` | Unit + Integration | ✅ | ✅ Tests required display-name fallback from real endpoint context and metadata persistence | ✅ Mapper and endpoint wiring pass with `currentUserDisplayName` + derived metadata persistence | ✅ OID-first, display-name fallback, unknown/no-match branches all exercised | ✅ Mapper remains metadata-only; no SDK leakage introduced |
| 5.3 | `tests/Aura.UnitTests/Infrastructure/AzureDevOpsPrProviderTests.cs` | Unit | ✅ Existing PR identity unit slices green before provider test addition | ✅ Added provider parsing tests first for id-present/id-missing/isContainer=true | ✅ `dotnet test ...AzureDevOpsPrProviderTests` passed | ✅ Three required ADO reviewer parsing scenarios covered | ✅ HTTP stub kept minimal and deterministic |
| 5.4 | N/A (verification task) | Verification | ✅ | ✅ N/A (execution task) | ✅ `dotnet test tests/Aura.UnitTests --filter "...PullRequestMapperTests|...AzureDevOpsPrProviderTests"` (41 passed) and `dotnet test tests/Aura.IntegrationTests --filter "...PullRequestsEndpointTests"` (9 passed) | ➖ Targeted verification commands per remediation scope | ➖ None needed |

## Test Summary

- **Targeted unit verification**: PASS (`PullRequestMapperTests`, `PrReviewWorkItemMapperIdentityTests`) — 40 passed
- **Targeted integration verification**: PASS (`PullRequestsEndpointTests`) — 8 passed
- **Targeted architecture verification**: PASS (`ReviewerIdentityArchitectureTests`, `PullRequestApiArchitectureTests`) — 4 passed
- **Full solution regression (`Aura.sln`)**: PASS
  - Architecture: 78/78
  - Unit: 1098/1098
  - Integration: 154/154
  - E2E: 45/45
- **Verify remediation targeted verification**: PASS
  - Unit (`PullRequestMapperTests` + `AzureDevOpsPrProviderTests`): 41/41
  - Integration (`PullRequestsEndpointTests`): 9/9

## Notes

- Implementation follows Clean Architecture boundaries: reviewer identity parsing remains in Infrastructure; Application derives attention scope from normalized metadata.
- `PullRequestDto.AttentionScope` was appended last to preserve additive-safe positional record compatibility.
- `WorkItem.Metadata` remains exposed as `IReadOnlyDictionary`; mapper persists derived keys only when the runtime metadata instance is mutable (`IDictionary<string,string>`), keeping safe behavior for read-only wrappers.
- For spec compliance, mapper now persists derived `pr.attentionScope` and conditional `pr.attentionScope.fallback=displayName` when metadata is mutable (the concrete ingestion path remains mutable dictionary-based).
