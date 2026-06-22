# Proposal: W2-H5-T1 Morning Summary Contracts

## Intent

Establish the contract foundation for the 09:00 Morning Summary engine without implementing
runtime behavior. Today only placeholders exist (`docs/architecture/triage/01-morning-summary.md`)
and the architecture map names `IMorningSummaryScheduler` / `IMorningSummaryComposer` but no
code backs them. Defining ports and payload DTOs first unblocks ranking (T2), timezone (T3),
and the UI card (W2-H6) while protecting Clean Architecture boundaries.

## Scope

### In Scope
- Port `IMorningSummaryComposer` (compose a summary payload for a user/window).
- Port `IMorningSummaryScheduler` (resolve and evaluate the daily summary window).
- Port `IWorkItemReader` (read work items for a window) — **contract only, no adapter**.
- DTOs: summary payload, ranked entry, ranking explanation (Impact/Deadline/Risk factors),
  window, and request/query contracts under `Aura.Application.Models`.
- Contract-shape unit tests + an architecture boundary test for the new ports.

### Out of Scope
- Any port implementation/adapter (reader, composer, scheduler) — deferred.
- Ranking engine logic (W2-H5-T2), timezone resolution logic (W2-H5-T3).
- Scheduler execution/hosted worker, Redis/cache adapter, UI rendering (W2-H6).
- Delivery/read telemetry implementation (contract may name it; no wiring).

## Capabilities

### New Capabilities
- `morning-summary-contracts`: ports and DTO contracts for composing and scheduling the daily
  Morning Summary, including an explainable ranking-explanation shape.

### Modified Capabilities
- None.

## Approach

Add three ports to `Aura.Application.Ports` and the payload/explanation DTOs to
`Aura.Application.Models`, following the existing Ports/Models split. Scoring is expressed as a
contract shape only — an explainable, factor-based ranking explanation (Decision B) — with no
engine. Caching stays a future Infrastructure concern behind the reader/composer boundary;
contracts are designed to be deterministic given inputs so a later cache/Redis adapter is safe
(Decision C). No runtime code (Decision A). Strict TDD: contract tests are written first.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/` | New | `IMorningSummaryComposer`, `IMorningSummaryScheduler`, `IWorkItemReader` |
| `src/Aura.Application/Models/` | New | Summary payload, ranked entry, ranking explanation, window, request/query |
| `tests/Aura.UnitTests/` | New | Contract-shape tests + fake composer |
| `tests/Aura.ArchitectureTests/` | New | Ports free of Infrastructure/SDK dependencies |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Contract churn when T2 ranking lands | Med | Keep ranking explanation factor-based and open-ended |
| Contracts leak UI/transport concerns | Low | Architecture boundary test on ports |
| Over-scoping beyond contracts | Low | Out-of-scope list + reviewer budget guard |

## Rollback Plan

All deliverables are new, additive files with no behavior wired into the running system. Roll
back by deleting the new ports, DTOs, and tests; no migration or data impact.

## Dependencies

- Existing `WorkItem` domain entity (`src/Aura.Domain/WorkItems/WorkItem.cs`).
- W2 has no blocking prerequisites.

## Success Criteria

- [ ] Three ports and the summary/ranking DTOs compile under `Aura.Application`.
- [ ] Architecture test proves ports carry no Infrastructure/SDK dependency.
- [ ] Contract tests assert DTO shape and an explainable ranking explanation.
- [ ] `dotnet test Aura.sln` passes; no runtime/adapter code introduced.

## Proposal Question Round (internal — assumptions documented)

The user already approved the Morning Summary analysis and Decisions A/B/C, so no blocking
questions are raised now. Assumptions captured for review:

- **Business rule**: the summary is generated per user per daily window and MUST be explainable
  (aligns with triage governance: explainable, auditable, user-adjustable).
- **Idempotency**: the composer contract is assumed deterministic given the same window + inputs,
  so repeated composition yields the same payload (enables safe caching — Decision C).
- **Ranking factors**: assumed to be Impact, Deadline, and Risk (per W2-H5-T2 backlog wording).
- **Edge cases (contract-level only)**: empty work-item set yields an empty-but-valid summary;
  ties in ranking are allowed; timezone is carried in the window but resolution is deferred (T3).
- **Non-goals**: no scoring math, no scheduler execution, no Redis, no UI, no telemetry wiring.
