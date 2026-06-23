# Morning Summary Scheduling Specification

## Purpose

Defines requirements for settings-driven, DST-correct due-state resolution and persisted
daily idempotence for the Morning Summary.

**Excluded:** content composition, ranking, timezone-aware data-window semantics.

---

## Requirements

### Requirement: Settings Resolution

The system MUST read `timezoneId` and `targetLocalTime` from `Project/System Settings`
before evaluating due-state. `targetLocalTime` MUST be config-supplied; hardcoding it
in any port or contract is forbidden. The scheduler MUST resolve settings internally.

#### Scenario: Settings resolved from configuration

- GIVEN `Project/System Settings` contains a valid `timezoneId` and `targetLocalTime`
- WHEN the scheduler is invoked
- THEN it reads both fields from settings for due-state evaluation

#### Scenario: Missing or invalid `timezoneId` falls back to system timezone

- GIVEN `Project/System Settings` contains a missing or unresolvable `timezoneId`
- WHEN the scheduler resolves the effective timezone
- THEN it uses the host system timezone as fallback

#### Scenario: System timezone unavailable falls back to UTC

- GIVEN `timezoneId` is invalid AND the host system timezone cannot be resolved
- WHEN the scheduler resolves the effective timezone
- THEN it falls back to UTC

---

### Requirement: DST-Correct Timezone Resolution

The system MUST evaluate `targetLocalTime` as local wall-clock time in the resolved
IANA timezone. Fixed UTC offsets MUST NOT be used. DST transitions MUST be applied
automatically by the timezone rules.

#### Scenario: Wall-clock comparison respects DST

- GIVEN a resolved IANA timezone with an active DST transition and `targetLocalTime` of `09:00`
- WHEN the scheduler evaluates due-state
- THEN the comparison uses IANA wall-clock time including DST; no fixed UTC offset is applied

---

### Requirement: Due-State Result Contract

The scheduler MUST return a result containing `resolvedTimezoneId`, `localDate`,
`targetLocalTime`, and `isDue`. When `isDue = false`, no additional payload is required.

#### Scenario: Morning Summary is due

- GIVEN current local wall-clock time is at or after `targetLocalTime` in the resolved timezone
- WHEN the scheduler is invoked
- THEN the result has `isDue = true` with `resolvedTimezoneId`, `localDate`, and `targetLocalTime` populated

#### Scenario: Morning Summary is not yet due

- GIVEN current local wall-clock time is before `targetLocalTime`
- WHEN the scheduler is invoked
- THEN the result has `isDue = false`

---

### Requirement: Persisted Daily Emission Guard

The system MUST persist Morning Summary emission per user per local date.
Re-invocations on the same local date for the same user MUST return `isDue = false`.
The guard MUST survive process restarts; in-memory state is insufficient.

#### Scenario: Guard blocks same-day duplicate

- GIVEN a Morning Summary was already emitted for user U on local date D
- WHEN the scheduler is invoked again for user U on local date D
- THEN `isDue = false` is returned; no second emission occurs

#### Scenario: Guard resets on the next local day

- GIVEN a Morning Summary was emitted for user U on local date D
- WHEN the scheduler is invoked for user U on local date D+1 at or after `targetLocalTime`
- THEN `isDue = true` is returned

#### Scenario: Guard survives process restart

- GIVEN a Morning Summary was emitted for user U on local date D and the process restarts
- WHEN the scheduler is invoked for user U on local date D after restart
- THEN `isDue = false` is returned (read from persistence, not memory)

---

### Requirement: Override-Ready Seam

The system MUST expose a programmatic mechanism to reset the daily guard for a given user
and local date, enabling forced re-emission. This seam MUST be prepared in this slice
but MUST NOT be wired to any UI or external action. Same-day `targetLocalTime`
changes are out of scope.

#### Scenario: Guard reset enables forced re-emission

- GIVEN a Morning Summary was already emitted for user U on local date D
- WHEN the override seam is invoked programmatically for user U on local date D
- THEN the guard is reset and the next scheduler invocation returns `isDue = true`
