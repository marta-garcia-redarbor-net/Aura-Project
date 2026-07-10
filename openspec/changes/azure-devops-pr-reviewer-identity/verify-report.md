# Verification Report

**Change**: azure-devops-pr-reviewer-identity  
**Version**: 2.0  
**Mode**: Strict TDD  
**Date**: 2026-07-09

---

## Verdict First

**FAIL**

The change implementation now satisfies the proposal/spec/design checks that are specific to reviewer identity and `AttentionScope`, but the required strict-TDD regression gate is **not current-pass** for the present workspace because `dotnet test Aura.sln` fails in `Aura.E2E` with 6 host-reachability failures.

**Next**: fixes-required

---

## Completeness

| Metric | Value |
|--------|-------|
| Tasks total | 14 |
| Tasks checked complete | 14 |
| Tasks unchecked | 0 |

Artifact completeness is 14/14, but verification currency is red because the current rerun of task **4.3** (`dotnet test Aura.sln`) is failing now.

---

## Build & Test Execution

### Targeted verification

```text
dotnet test tests/Aura.UnitTests --filter "PullRequestMapperTests|PrReviewWorkItemMapperIdentityTests|AzureDevOpsPrProviderTests"
Passed: 44, Failed: 0

dotnet test tests/Aura.ArchitectureTests --filter "ReviewerIdentityArchitectureTests|PullRequestApiArchitectureTests"
Passed: 4, Failed: 0

dotnet test tests/Aura.IntegrationTests --filter "PullRequestsEndpointTests"
Passed: 9, Failed: 0
```

### Full regression

```text
dotnet test Aura.sln
Architecture: 78/78 passed
Unit: 1102/1102 passed
Integration: 155/155 passed
E2E: 39/45 passed
```

### Current regression blockers

```text
Aura.E2E.PlaywrightTests.PlaywrightBootstrapTests.DashboardShell_RendersWithExpectedMarkers
Aura.E2E.Browser.HealthRouteBrowserTests.HealthRoute_SidebarLinkNavigatesToHealthPage_WithPanels
Aura.E2E.Browser.DashboardRootBrowserTests.DashboardRoot_ShellVisibleAndStateTransition
Aura.E2E.PlaywrightTests.PlaywrightBootstrapTests.SyncStatusPanel_RendersOnDashboard
Aura.E2E.PlaywrightTests.PlaywrightBootstrapTests.FocusStateBadge_RendersOnDashboard
Aura.E2E.PlaywrightTests.PlaywrightBootstrapTests.InboxPreviewPanel_RendersOnDashboard

Failure shape: HostNotReachable from PlaywrightWebApplicationFactory
```

**Coverage**: Coverage analysis skipped — no coverage tool detected.

---

## TDD Compliance

| Check | Result | Details |
|-------|--------|---------|
| TDD evidence reported | ✅ | `apply-progress.md` includes a TDD Cycle Evidence table |
| Test files exist | ✅ | Verified change-related test files exist: `PullRequestMapperTests.cs`, `PrReviewWorkItemMapperIdentityTests.cs`, `AzureDevOpsPrProviderTests.cs`, `PullRequestsEndpointTests.cs`, `ReviewerIdentityArchitectureTests.cs` |
| GREEN confirmed (tests pass) | ⚠️ | Targeted change suites pass, but the required full regression command from task 4.3 currently fails |
| Triangulation adequate | ✅ | Mapper, provider, persistence, endpoint, and architecture scenarios are covered |
| Safety net reported | ✅ | `apply-progress.md` records baseline/safety-net execution evidence |

**TDD Compliance**: 4/5 checks passed.

---

## Test Layer Distribution

| Layer | Tests | Files | Tools |
|-------|-------|-------|-------|
| Unit | 44 | 3 | xUnit |
| Integration | 9 | 1 | xUnit + WebApplicationFactory |
| Architecture | 2 | 1 | xUnit + NetArchTest |
| E2E | 0 | 0 | Not change-specific |
| **Total** | **55** | **5** | |

`PullRequestApiArchitectureTests` was also executed as a safety-net architecture dependency, but it is not counted as a change-owned test file in the distribution above.

---

## Changed File Coverage

Coverage analysis skipped — no coverage tool detected.

---

## Assertion Quality

**Assertion quality**: ✅ All inspected assertions verify real behavior.

No tautologies, ghost loops, empty-only assertions, or mock-heavy fake coverage patterns were found in the change-related test files.

---

## Spec Compliance Matrix

### `openspec/changes/azure-devops-pr-reviewer-identity/specs/pr-reviewer-identity/spec.md`

| Requirement | Scenario | Evidence | Result |
|-------------|----------|----------|--------|
| Reviewer Identity Capture | Reviewer with oid parsed | `AzureDevOpsPrProviderTests > FetchAsync_WithReviewerId_MapsReviewerIdentityOid` | ✅ COMPLIANT |
| Reviewer Identity Capture | Reviewer without oid | `AzureDevOpsPrProviderTests > FetchAsync_WithoutReviewerId_MapsNullOidAndPreservesDisplayName` | ✅ COMPLIANT |
| Reviewer Identity Capture | Group reviewer detected | `AzureDevOpsPrProviderTests > FetchAsync_WithContainerReviewer_MapsIsContainerTrue` | ✅ COMPLIANT |
| Reviewer Identity Persistence | Identity keys persisted | `PrReviewWorkItemMapperIdentityTests > TryMap_WithReviewerIdentityOid_PersistsIdentityKeys` | ✅ COMPLIANT |
| Reviewer Identity Persistence | Null oid key omitted | `PrReviewWorkItemMapperIdentityTests > TryMap_WithNullReviewerOid_OmitsOidKeyButPersistsDisplayName` | ✅ COMPLIANT |
| AttentionScope Derivation | Direct match by oid | `PullRequestMapperTests > ToDto_AttentionScope_WithDirectOidMatch_ReturnsDirect`; `PullRequestsEndpointTests > GetPullRequests_WhenReviewerIdentityMatchesCurrentUser_MapsAttentionScope` | ✅ COMPLIANT |
| AttentionScope Derivation | Group match by oid | `PullRequestMapperTests > ToDto_AttentionScope_WithGroupOidMatch_ReturnsGroup` | ✅ COMPLIANT |
| AttentionScope Derivation | No match returns none | `PullRequestMapperTests > ToDto_AttentionScope_WithoutMatches_ReturnsNone` | ✅ COMPLIANT |
| AttentionScope Derivation | Unknown when user oid unavailable | `PullRequestMapperTests > ToDto_AttentionScope_WhenCurrentUserOidUnavailable_ReturnsUnknown`; `PullRequestsEndpointTests > GetPullRequests_WhenCurrentUserOidUnavailable_DefaultsAttentionScopeToUnknown` | ✅ COMPLIANT |
| AttentionScope Derivation | Display-name fallback is flagged | `PullRequestMapperTests > ToDto_AttentionScope_WhenOidMissingAndDisplayNameMatches_ReturnsDirectFallback` | ✅ COMPLIANT |
| Architecture Boundary | SDK types absent from Application/Domain | `ReviewerIdentityArchitectureTests`; targeted source inspection found no Azure DevOps/Graph SDK references in `src/Aura.Application` or `src/Aura.Domain` | ✅ COMPLIANT |

### `openspec/changes/azure-devops-pr-reviewer-identity/specs/pull-request-api/spec.md`

| Requirement | Scenario | Evidence | Result |
|-------------|----------|----------|--------|
| PullRequestDto Mapping | Metadata fields extracted correctly | `PullRequestsEndpointTests > GetPullRequests_MapsMetadataFieldsCorrectly` | ✅ COMPLIANT |
| PullRequestDto Mapping | Missing metadata keys default safely | `PullRequestMapperTests` missing-key default cases | ✅ COMPLIANT |
| PullRequestDto Mapping | Numeric metadata parsed safely | `PullRequestMapperTests` invalid numeric cases | ✅ COMPLIANT |
| PullRequestDto Mapping | AttentionScope extracted from metadata | `PullRequestMapperTests > ToDto_AttentionScope_UsesPrecomputedMetadataValue` | ✅ COMPLIANT |
| PullRequestDto Mapping | AttentionScope defaults to unknown when absent | `PullRequestMapperTests > ToDto_AttentionScope_WhenCurrentUserOidUnavailable_ReturnsUnknown`; `PullRequestsEndpointTests > GetPullRequests_WhenCurrentUserOidUnavailable_DefaultsAttentionScopeToUnknown` | ✅ COMPLIANT |

**Compliance summary**: 16/16 scenarios compliant.

---

## Correctness (Source Inspection)

| Check | Status | Notes |
|------|--------|-------|
| `PrReviewDto` carries identity-rich reviewer data | ✅ | `ReviewerIdentities` added while preserving `Reviewers` |
| Azure DevOps parsing stays in Infrastructure | ✅ | `AzureDevOpsPrProvider` maps reviewer `Id` and `IsContainer` into `PrReviewerIdentity` |
| `PullRequestDto.AttentionScope` is additive and appended last | ✅ | New field is last positional parameter with default `"unknown"` |
| Application derives from normalized metadata only | ✅ | `PullRequestMapper` consumes `WorkItem.Metadata` + `PrMetadataKeys`; no SDK types used |
| Derived scope metadata is persisted when metadata is mutable | ✅ | `TryPersistDerivedAttentionMetadata` writes `pr.attentionScope` |
| Display-name fallback metadata is persisted | ✅ | `TryPersistDerivedAttentionMetadata` writes `pr.attentionScope.fallback=displayName` on fallback |
| Endpoint makes fallback reachable in real flow | ✅ | `PullRequestsEndpoints` passes both `currentUser.Oid` and `currentUser.DisplayName` |
| Reviewer identity metadata is persisted with count retained | ✅ | `PrReviewWorkItemMapper` writes `pr.reviewer.<n>.*` and keeps `pr.reviewerCount` |

---

## Design Coherence

| Decision | Followed? | Notes |
|----------|-----------|-------|
| Keep reviewer parsing in Infrastructure | ✅ | Provider + mapper responsibilities remain in Infrastructure |
| Pass current user context into mapper instead of injecting a service | ✅ | Endpoint passes values into `PullRequestMapper.ToDto(...)` |
| Keep `WorkItem.Metadata` read-only at the contract boundary | ✅ | Mutation only occurs when the concrete metadata instance is mutable; read-only wrappers are respected |
| Add `ReviewerIdentities` while preserving `Reviewers` | ✅ | Backward compatibility maintained |
| Model `AttentionScope` as appended string field | ✅ | Matches existing DTO style |
| Support explicit fallback metadata path | ✅ | Mapper persists `pr.attentionScope.fallback` during display-name fallback |

---

## Issues Found

### CRITICAL

1. **Current workspace regression gate is failing.** `dotnet test Aura.sln` exits non-zero because 6 `Aura.E2E` browser/playwright tests fail with `HostNotReachable` from `PlaywrightWebApplicationFactory`.

### WARNING

1. The workspace is dirty beyond this change, so the failing full regression may be influenced by concurrent modifications outside `azure-devops-pr-reviewer-identity`.
2. Architecture test execution emits existing analyzer/code-analysis warnings unrelated to this change; they are non-blocking for the change but add noise to the verification signal.

### SUGGESTION

1. Triage `tests/Aura.E2E/Browser/PlaywrightWebApplicationFactory.cs` host startup/reachability for the current workspace before re-running the strict regression gate.
2. Once `dotnet test Aura.sln` is green again, refresh this verify report and move directly to archive.

---

## Final Verdict

## FAIL

The **change-specific implementation is currently compliant** with proposal, specs, design, and completed tasks, and all targeted unit/integration/architecture checks pass. However, **Strict TDD verification is still failing overall** because the required full regression command for the present workspace (`dotnet test Aura.sln`) is red.

**Next**: fixes-required
