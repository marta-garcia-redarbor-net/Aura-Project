# Triage — Proactive Interruption Policy

This document defines how Aura decides when to interrupt the user and when to keep work in queue.

## Quick path

1. Receive canonical `WorkItem` plus connector-provided source signals and preliminary score.
2. Evaluate global interruption policy using severity, user context, and attentional budget.
3. Emit a final decision: `INTERRUPT`, `QUEUE`, or `DEFER`, with an explainable reason.

## Boundary and authority

- Connector scores are **input signals only**.
- The final interrupt-vs-queue decision is owned exclusively by `IInterruptionPolicyEngine`.
- No connector is allowed to decide final interruption behavior.

## Governance requirements

All interruption policy rules MUST be:

- **Explainable**: each decision includes a human-readable rationale.
- **Auditable**: decision signals, thresholds, and outcome are traceable.
- **User-adjustable**: users can modify policy preferences/overrides.

Rules MUST NOT change silently through opaque auto-learning.

## Policy inputs

Typical global inputs include:

- Source-agnostic severity and urgency indicators
- User preferences and explicit overrides
- Recent feedback on false positives/negatives
- Historical decision outcomes and context windows
- Meeting/focus constraints and attentional budget limits

## Observability contract

Each decision should capture enough data to verify policy behavior:

- Input signal snapshot
- Applied policy branch or rule key
- Final decision and rationale
- Correlation metadata for downstream review
