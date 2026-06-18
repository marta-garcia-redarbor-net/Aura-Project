# Dashboard Module Progress Specification

## Purpose

Expose module progress states (pending / in-progress / completed) from manually
seeded data via `Aura.Api` DTO contracts. The dashboard panel renders progress
read-only. The seeded source is decoupled from the Application layer via a port
contract so it is replaceable without changing business logic.

## Requirements

### Requirement: Module Progress Port Contract

The system MUST define a port interface in the Application layer for retrieving
module progress entries. Each entry MUST carry a module identifier and one of three
states: `pending`, `in-progress`, or `completed`. The Infrastructure layer MUST
provide a seeded adapter implementing this port.

#### Scenario: Progress entries returned via port

- GIVEN the seeded module progress adapter is registered against the Application port
- WHEN the use case requests module progress
- THEN a list of entries is returned, each with a module identifier and a valid state

#### Scenario: Port is adapter-agnostic

- GIVEN the Application layer depends on the port interface, not the concrete adapter
- WHEN a different Infrastructure adapter is registered
- THEN the use case behavior is unchanged

---

### Requirement: Seeded Data Labeling

The DTO returned from the module-progress endpoint MUST include a field that
identifies the data as seeded (e.g., `isSeeded: true`). This field MUST be set to
`true` whenever the response originates from the seeded adapter, so operators are
not misled into treating the data as live.

#### Scenario: DTO flags data as seeded

- GIVEN the seeded adapter is the active Infrastructure implementation
- WHEN a client calls the module-progress endpoint
- THEN the response DTO includes a field confirming the data source is seeded
  (e.g., `isSeeded: true`)

---

### Requirement: Module Progress API Endpoint

The API MUST expose a GET-only endpoint returning a DTO list of module progress
entries. Write verbs (POST, PUT, PATCH, DELETE) MUST NOT be accepted on this
endpoint.

#### Scenario: GET returns module progress entries

- GIVEN the seeded adapter is registered and active
- WHEN a client sends GET to the module-progress endpoint
- THEN the response is HTTP 200 with a DTO list of module entries, each containing
  module identifier, state, and seeded flag

#### Scenario: Write verbs rejected

- GIVEN the module-progress endpoint is available
- WHEN a client sends POST, PUT, PATCH, or DELETE
- THEN the response is HTTP 405 Method Not Allowed

---

### Requirement: Module Progress Panel

The UI MUST render a read-only panel displaying each module's progress state. Each
of the three states (`pending`, `in-progress`, `completed`) MUST have a distinct
visual representation. Loading, empty, and error view states MUST be present. The
panel MUST NOT expose any edit or submit affordance.

#### Scenario: Three states render distinctly

- GIVEN the API returns entries with `pending`, `in-progress`, and `completed` states
- WHEN the module-progress panel renders
- THEN each state is visually distinct from the others
- AND no edit controls are present

#### Scenario: Empty list shows explicit empty state

- GIVEN the API returns an empty list of module entries
- WHEN the panel renders
- THEN an explicit empty state is shown
- AND the dashboard shell and navigation remain functional

#### Scenario: API failure shows error state

- GIVEN the module-progress API call fails or returns a non-200 response
- WHEN the panel handles the failure
- THEN the panel shows an explicit error state without crashing the dashboard

---

### Requirement: Architecture Isolation

Module progress derivation and seeded data access MUST reside in Application and
Infrastructure respectively. No Infrastructure type or seeded adapter SHALL appear
in the Application or UI layers. Architecture tests MUST enforce this boundary.

#### Scenario: Architecture tests confirm layer isolation

- GIVEN the dashboard-module-progress capability is fully implemented
- WHEN the architecture test suite runs
- THEN no Infrastructure types are found in the Application or UI project namespaces
