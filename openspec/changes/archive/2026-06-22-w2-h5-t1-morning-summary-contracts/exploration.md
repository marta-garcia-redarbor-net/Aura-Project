# Exploration: W2-H5-T1 Morning Summary Contracts

## Current State

- Triage architecture (`docs/architecture/triage/00-overview.md`) defines a two-stage model:
  connectors normalize into canonical `WorkItem`s and pre-score; a global triage engine owns
  the final interrupt-vs-queue decision. Governance MUST be explainable, auditable, and
  user-adjustable.
- `docs/ai/02-architecture-map.md` already names the target ports: `IMorningSummaryScheduler`,
  `IMorningSummaryComposer`, plus `IPriorityScoringService` (future).
- `docs/architecture/triage/01-morning-summary.md` is a placeholder: it asks for the two
  contracts, prioritization/grouping rules, idempotent daily-window generation, delivery
  telemetry, and composition tests.
- `WorkItem` (`src/Aura.Domain/WorkItems/WorkItem.cs`) is a sealed domain entity with
  `Priority`, `SourceType`, `Metadata`, `CapturedAtUtc`, `CorrelationId`, `Status`.
- Ports live in `src/Aura.Application/Ports/` (e.g. `IWorkItemStore`, `IWorkItemBuffer`).
  DTOs live in `src/Aura.Application/Models/` (e.g. `InitialDashboardDto`, `SystemStatusDto`).
- No morning-summary code, ports, or specs exist yet. No `IWorkItemReader` exists today.

## Affected Areas

- `src/Aura.Application/Ports/` — new ports: `IMorningSummaryComposer`,
  `IMorningSummaryScheduler`, `IWorkItemReader` (port only, no adapter).
- `src/Aura.Application/Models/` — new payload/explanation DTOs.
- `tests/Aura.UnitTests/` — contract-shape tests for the DTOs and a fake composer.
- `tests/Aura.ArchitectureTests/` — boundary test (ports stay free of Infrastructure/SDKs).
- `docs/architecture/triage/01-morning-summary.md` — may be referenced; not rewritten here.

## Approaches

1. **Contract-first ports + DTOs in Application (recommended)** — define the three ports and
   the summary/ranking-explanation DTOs as pure Application contracts; no runtime engine,
   scheduler execution, Redis, or UI.
   - Pros: matches existing Ports/Models convention; unblocks T2 (ranking) and T3 (timezone)
     and W2-H6 (UI card) without committing implementation; keeps Clean Architecture intact.
   - Cons: contracts may need minor revision once T2 ranking lands.
   - Effort: Low.

2. **Define contracts in Domain** — place summary types in `Aura.Domain`.
   - Pros: summary is arguably a domain concept.
   - Cons: breaks the established pattern (DTOs/orchestration contracts live in Application);
     couples Domain to read/query shapes. Rejected.
   - Effort: Low-Medium.

## Recommendation

Approach 1. Define `IMorningSummaryComposer`, `IMorningSummaryScheduler`, and `IWorkItemReader`
as ports under `Aura.Application.Ports`, and the summary payload + ranking-explanation DTOs
under `Aura.Application.Models`. Keep scoring as a *contract shape* (explainable factor
contributions) without an engine (Decision B). Keep caching a future Infrastructure concern
behind the reader/composer boundary (Decision C). No implementation in T1 (Decision A).

## Risks

- Over-specifying DTOs now could force churn when T2 ranking is implemented — mitigate by
  keeping the ranking explanation factor-based and open (Impact/Deadline/Risk).
- Coupling contracts to UI/transport shapes — mitigate with architecture boundary test.

## Ready for Proposal

Yes. Scope is clear and tightly bounded to contracts/artifacts. The user already approved the
Morning Summary analysis and Decisions A/B/C; no blocking questions remain.
