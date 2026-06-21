# Proposal: W2-H4 — Outlook Plugin Mapping and Initial Classification

## Intent

Aura has no Outlook adapter yet, so Outlook emails never become canonical work items even though the domain already supports `WorkItemSourceType.OutlookEmail`. W2-H4 adds the Outlook anti-corruption layer in `Aura.Infrastructure`, mirroring the proven Teams slice (W2-H3), so Outlook emails enter triage as canonical `WorkItem`s.

Initial classification scope is now expanded by product decision: priority MUST NOT rely only on the `Importance` flag or subject keywords. `Importance` may be unset or unreliable, and subject-only heuristics miss real signal. W2-H4 therefore derives priority from a multi-signal score combining `Importance`, subject cues, **sender weight**, and **body cues**, so classification is robust when any single signal is weak.

## Scope

### In Scope
- Outlook DTOs and mock email payloads contained in `Aura.Infrastructure`.
- Map valid Outlook email payloads to canonical `WorkItem` with `SourceType = OutlookEmail`.
- Multi-signal initial classification: priority derived from `Importance`, subject cues, sender weight, and body cues (no single signal is authoritative).
- Sender scoring input (`SenderAddress`) and body scoring input (`BodyPreview`) as first-class classification signals, not just metadata.
- Deterministic, explainable scoring: every contributing signal recorded in `WorkItem.Metadata` for traceability.
- Partial-payload tolerance: degrade recoverable fields, skip unrecoverable items, never abort the batch.
- Initial classification tests covering each signal independently and in combination (including absent/unreliable `Importance`).

### Out of Scope
- Calendar and GitHub connectors (later slices).
- Real Microsoft Graph HTTP calls, auth, checkpoint/delta sync, idempotency, retry.
- Full NLP / ML body analysis — body scoring is keyword/pattern-based at this slice.
- A trained or externally tuned sender-reputation service; sender weight is rule-based and local.
- Changes to `IConnectorAdapter`, `WorkItem`, or persistence (`work-item-persistence` delivered in W2-H3).

## Capabilities

### New Capabilities
- `outlook-connector-mapping`: Outlook email payload → canonical `WorkItem` mapping rules, partial-payload tolerance, `Metadata` preservation, and multi-signal initial classification (importance + subject + sender + body scoring). Name retained — it already covers mapping and classification; the scoring scope widens inside it.

### Modified Capabilities
- None. This capability is introduced by W2-H4; the expanded scoring scope is captured in its own (still-unfinished) spec, not a delta against an archived capability. `connector-execution` and `work-item-contract` requirements are unchanged.

## Approach

Adapter-local anti-corruption layer (exploration recommendation). Outlook DTOs, mock fixtures, and mapping live in `Aura.Infrastructure`; translate to canonical `WorkItem` there. No Outlook or Microsoft Graph SDK type escapes Infrastructure. Reuse the provider-neutral `IConnectorAdapter` contract; do not introduce a shared normalizer for a single provider slice.

Classification uses an additive scoring function `ResolvePriority(importance, subject, sender, body)` that maps a weighted score to `Priority`. Each signal contributes a bounded weight; the resolved priority and every contributing signal are written to `Metadata` so the decision is explainable and testable. When `Importance` is absent or unrecognized, the remaining signals still produce a defensible priority — no silent default to `Medium` when sender/body carry signal.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/` | New | Outlook DTO, mock payloads, mapper (multi-signal scoring), adapter |
| `src/Aura.Infrastructure/.../DependencyInjection.cs` | Modified | Register Outlook mapper/adapter |
| `tests/Aura.UnitTests/Ingestion/Outlook/` | New | Mapping + per-signal + combined-scoring classification tests |
| `tests/Aura.ArchitectureTests` | Modified | Assert no Outlook/Graph leakage above Infrastructure |
| `docs/architecture/ingestion/00-overview.md` | Doc-debt | Still lists Graph adapters as pending; flag mismatch |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Scoring weights produce surprising priorities (combined signals) | Med | Bounded weights; record each signal in `Metadata`; combination tests assert thresholds |
| Sender scoring becomes a hidden reputation service (scope creep) | Med | Keep sender weight rule-based and local; no external lookup this slice |
| Body scanning drifts toward NLP/regex engine | Med | Keyword/pattern scan only; spec names the patterns |
| `Importance`-absent paths under-tested | Med | Explicit test cases where `Importance` is unset and sender/body drive priority |
| Scope creep into shared ingestion normalizer | Med | Keep slice provider-specific; one port, one adapter |
| Outlook/Graph SDK type leaks above Infrastructure | Low | Architecture tests fail fast |

## Rollback Plan

Revert the W2-H4 commits. The Outlook folder, DI registration, and tests are removed. No schema or external state changes, so revert is clean.

## Dependencies

- Canonical `WorkItem` and `WorkItemSourceType.OutlookEmail` (already present).
- `work-item-persistence` port (delivered in W2-H3).

## Success Criteria

- [ ] Outlook mock payloads map to canonical `WorkItem`s with `SourceType = OutlookEmail`.
- [ ] Priority is derived from a combination of `Importance`, subject, sender, and body — never `Importance`- or subject-only.
- [ ] When `Importance` is absent or unreliable, sender and body signals still drive a defensible priority.
- [ ] Every contributing classification signal is recorded in `WorkItem.Metadata` and is testable.
- [ ] Partial/invalid payloads degrade gracefully; degraded values recorded in `Metadata`.
- [ ] Per-signal and combined-signal classification tests pass.
- [ ] Architecture-boundary tests confirm no Outlook/Graph type leakage.
