# Tasks: W3-H3 Focus State UI & Prioritized Queue

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

| Field | Value |
|-------|-------|
| Estimated changed lines | 1600–2400 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (Domain+Infra) → PR 2 (API) → PR 3 (UI+Tes) |
| Delivery strategy | single-pr-default |
| Chain strategy | pending |

## Phase 1: Domain Foundation

- [x] 1.1 Add `int? PriorityScore` to `WorkItem` entity + optional ctor param
- [x] 1.2 Add `GetDefaultScore()` extension on `WorkItemPriority` (Critical→100, High→75, Medium→50, Low→25)
- [x] 1.3 Add `int? PriorityScore` to `WorkItemDetailDto` and `InboxItemPreviewDto`
- [x] 1.4 Create `IFocusStateOverrideStore` port (GetAsync, SetAsync, ClearAsync)
- [x] 1.5 Create `IInterruptionDecisionStore` port (RecordAsync, QueryAsync)
- [x] 1.6 Create models: `InterruptionDecisionRecord`, `FocusStateResponse`, `PagedResult<T>`, `DashboardPriorityDto`

## Phase 2: Persistence & Engine

- [x] 2.1 Create `SqliteFocusStateOverrideStore` with `FocusStateOverrides` table
- [x] 2.2 Create `SqliteInterruptionDecisionStore` with `InterruptionDecisions` table
- [x] 2.3 Modify `SqliteWorkItemStore`: add `PriorityScore` column, COALESCE sort DESC
- [x] 2.4 Modify `FocusStateResolver`: inject override store, check before auto-compute
- [x] 2.5 Modify `InterruptionPolicyEngine`: inject decision store, persist ALL verdicts
- [x] 2.6 Register new stores in `DependencyInjection.cs`

## Phase 3: API Endpoints

- [x] 3.1 Create `FocusStateEndpoints`: GET, PUT, DELETE `/api/focus-state`
- [x] 3.2 Create `TriageEndpoints`: GET `/api/triage/decisions` with pagination
- [x] 3.3 Modify `DashboardEndpoints`: add priority counts + top-3 to preview
- [x] 3.4 Modify `WorkItemsEndpoints`: include `PriorityScore` in DTO, sort DESC
- [x] 3.5 Register endpoint groups in `Program.cs`

## Phase 4: UI Layer

- [x] 4.1 Create `FocusStateBadge.razor` (color-coded badge + dropdown with clear)
- [x] 4.2 Modify `Header.razor` to embed `FocusStateBadge`
- [x] 4.3 Create `IFocusStateApiClient` + `FocusStateApiClient`
- [x] 4.4 Create `IDecisionLogApiClient` + `DecisionLogApiClient`
- [x] 4.5 Create UI models: `FocusStateResponse`, `DecisionLogResponse`
- [x] 4.6 Modify `Sidebar.razor`: add "Interruption Log" entry
- [x] 4.7 Create `DecisionLog.razor` page at `/triage/decisions` (all 4 states)
- [x] 4.8 Register new HTTP clients in `UI/Program.cs`

## Phase 5: Testing

- [x] 5.1 Unit: `WorkItem` PriorityScore ctor + round-trip + default derivation
- [x] 5.2 Unit: `FocusStateResolver` override-check before auto-compute
- [x] 5.3 Unit: `InterruptionPolicyEngine` persists ALL verdicts (Interrupt, Queue, Defer)
- [x] 5.4 Unit: Decision log pagination boundaries
- [x] 5.5 Integration: `WebApplicationFactory` for focus-state + triage endpoints
- [x] 5.6 UI (bUnit): Header badge renders + dropdown override flow
- [x] 5.7 UI (bUnit): DecisionLog loading/empty/error/retry states
