# Morning Summary Ranking Specification

## Purpose

Defines the deterministic ranking policy applied by the Morning Summary to produce an ordered,
explainable list of work items. Covers decision order, preliminary score usage, tiebreak chain,
fallback behavior, and output contract.

Connector pre-scoring algorithm, AI-assisted prioritization, caching, scheduling, persistence,
and UI behavior are out of scope.

## Requirements

| Requirement | Constraint | Strength |
|---|---|---|
| Primary Ranking Order | Deadline → Impact → Risk for explicit signal resolution | MUST |
| Preliminary Score as Decision Input | Single input used post-explicit and as all-signals-absent fallback | MUST |
| Deterministic Tiebreak Chain | Nearest due date → oldest item → stable Id | MUST |
| Insufficient Signals Handling | No signals and no score → `insufficient-signals`, placed last | MUST |
| Ranked Output Contract | Ordered list + structured per-item explanation aligned with rank signals | MUST |
| Application Layer Ownership | Final ranking policy resides in Application layer only | MUST |
| AI-Assisted Prioritization Boundary | AI-assisted prioritization is out of scope | MUST NOT |

---

### Requirement: Primary Ranking Order

The Morning Summary ranking policy MUST evaluate explicit signals in this order: Deadline first,
Impact second, Risk third. The first signal that distinguishes two items determines their relative
order; lower-priority signals are not consulted for that pair.

#### Scenario: Deadline resolves order

- GIVEN two items where one has an earlier deadline and the other does not
- WHEN the ranking policy is applied
- THEN the item with the earlier deadline ranks higher, regardless of Impact or Risk values

#### Scenario: Impact resolves when Deadline does not

- GIVEN two items with equal or absent Deadline signals and different Impact signals
- WHEN the ranking policy is applied
- THEN the item with the higher Impact signal ranks higher

#### Scenario: Risk resolves when Deadline and Impact do not

- GIVEN two items where Deadline and Impact signals are equal or absent, and Risk signals differ
- WHEN the ranking policy is applied
- THEN the item with the higher Risk signal ranks higher

---

### Requirement: Preliminary Score as Decision Input

When explicit signals (Deadline, Impact, Risk) do not fully determine the order of two items,
the connector preliminary score MUST be used as the next decision input. When all explicit signals
are absent for an item, the preliminary score MUST serve as its sole decision input.

The preliminary score is ONE decision input with two applicable contexts. It MUST NOT be
documented or implemented as two independent rules.

#### Scenario: Preliminary score breaks post-explicit tie

- GIVEN two items whose explicit signals leave their relative order unresolved
- WHEN the ranking policy consults the preliminary score
- THEN the item with the higher preliminary score ranks higher

#### Scenario: Preliminary score positions an item with no explicit signals

- GIVEN a work item carrying no Deadline, Impact, or Risk signal and a connector preliminary score
- WHEN the ranking policy is applied
- THEN the preliminary score is the sole input determining that item's position

#### Scenario: Preliminary score appears as one input in documentation and implementation

- GIVEN the ranking policy documentation and its implementation
- WHEN a reviewer inspects the policy structure
- THEN the preliminary score appears as a single decision input used in two contexts
- AND it is not split into separate independent rule entries

---

### Requirement: Deterministic Tiebreak Chain

When the preliminary score does not resolve the order of two items, the system MUST apply the
following chain in sequence: nearest due date, then oldest item (earliest creation timestamp),
then lexically lower stable Id.

#### Scenario: Nearest due date resolves remaining tie

- GIVEN two items with equal explicit signals and preliminary scores but different due dates
- WHEN the tiebreak chain is applied
- THEN the item with the nearest due date ranks higher

#### Scenario: Oldest item resolves when due dates are equal or absent

- GIVEN two items where signals, scores, and due dates are equal or absent
- WHEN the tiebreak chain advances to the next step
- THEN the item with the earliest creation timestamp ranks higher

#### Scenario: Stable Id is the final deterministic resolver

- GIVEN two items where all prior signals, scores, due dates, and timestamps are equal or absent
- WHEN the tiebreak chain reaches its final step
- THEN the item with the lexically lower stable Id ranks higher

---

### Requirement: Insufficient Signals Handling

A work item with no explicit signals AND no connector preliminary score MUST be classified as
`insufficient-signals` and placed after all items that carry any usable decision signal or score.

#### Scenario: Insufficient-signals item placed last

- GIVEN a work item with no Deadline, Impact, or Risk signals and no preliminary score
- WHEN the ranking policy is applied
- THEN the item receives the `insufficient-signals` classification
- AND it appears after every item that carries at least one usable signal or score

---

### Requirement: Ranked Output Contract

The Morning Summary output MUST be an ordered list of ranked work items. Each entry MUST include
a structured per-item explanation that identifies which signal or chain step determined its
position, consistent with the `RankingExplanation` shape defined in the Morning Summary contracts
spec.

#### Scenario: Output is an ordered list with per-item explanation

- GIVEN a set of work items after the ranking policy has been applied
- WHEN the Morning Summary output is produced
- THEN the output is an ordered list
- AND each entry carries a structured explanation aligned with the signals that determined its rank

---

### Requirement: Application Layer Ownership

The final Morning Summary ranking policy MUST reside in `Aura.Application`. No connector or
Infrastructure component MAY own or override the final ranking decision.

#### Scenario: Ranking policy is not implemented in a connector

- GIVEN the solution's layer structure
- WHEN an architecture test inspects where final ranking logic resides
- THEN it is located within `Aura.Application` and not in any connector or Infrastructure class

---

### Requirement: AI-Assisted Prioritization Boundary

The Morning Summary ranking policy MUST NOT incorporate AI-assisted prioritization.
AI-assisted prioritization is reserved for future work and is explicitly out of scope for
all current ranking implementations.

#### Scenario: No AI ranking path exists

- GIVEN the Morning Summary ranking policy implementation and its documentation
- WHEN a reviewer inspects the ranking decision path
- THEN no AI-assisted prioritization mechanism is present or referenced as current behavior
