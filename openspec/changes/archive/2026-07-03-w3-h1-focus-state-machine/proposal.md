# Proposal: W3-H1 — Focus State Machine

## Intent

Modelar los estados de foco del usuario (DeepWork, WindowOfOpportunity, Away, Recovery) como un ciudadano de primera clase del dominio, con transiciones explícitas, guardas y un resolver que determine el estado activo. Sin esto, el motor de interrupciones (W3-H2) no tiene contexto para decidir si interrumpir o diferir.

## Scope

### In Scope
- FocusStateType enum en Aura.Domain.FocusState (los 4 estados)
- FocusState sealed class con transiciones guardadas (patrón WorkItem)
- IFocusStateResolver port en Aura.Application.Ports
- FocusStateResolver implementación en Aura.Application (determina estado actual según señales)
- Tests unitarios de transiciones válidas, inválidas y resolución
- Actualización del doc docs/architecture/triage/03-focus-state-machine.md

### Out of Scope
- UI del estado de foco en dashboard (W3-H3)
- Interruption engine (W3-H2)
- Configuración de usuario de transiciones (futuro)

## Capabilities

> This section is the CONTRACT between proposal and specs phases.
> The sdd-spec agent reads this to know exactly which spec files to create or update.
> Research `openspec/specs/` before filling this in.

### New Capabilities
<!-- Capabilities being introduced. Each becomes a new `openspec/specs/<name>/spec.md`.
     Use kebab-case names (e.g., user-auth, data-export, api-rate-limiting).
     Leave empty if no new capabilities. -->
- `focus-state-machine`: define los estados de foco, transiciones y el resolver de estado actual

### Modified Capabilities
<!-- Existing capabilities whose REQUIREMENTS are changing (not just implementation).
     Only list here if spec-level behavior changes. Each needs a delta spec.
     Use existing spec names from openspec/specs/. Leave empty if none. -->
None

## Approach

Rich domain state machine with guarded transitions, siguiendo el patrón exacto de WorkItem/WorkItemStatus:
- FocusState sealed class con métodos como TryEnterDeepWork(), GoToAway(), etc.
- Cada método valida la transición y lanza InvalidOperationException si es ilegal
- FocusStateResolver recibe señales (calendario, hora, preferencias) y llama al método correspondiente
- IFocusStateResolver expone Task<FocusState> ResolveAsync(UserId userId, CancellationToken ct)

### Transition Matrix

| From → To | Allowed | Trigger |
|-----------|---------|---------|
| DeepWork → WindowOfOpportunity | ✅ | break |
| WindowOfOpportunity → Away | ✅ | dnd / meeting |
| Away → Recovery | ✅ | end |
| Away → DeepWork | ✅ | direct focus return |
| Recovery → DeepWork | ✅ | refocus |
| Recovery → WindowOfOpportunity | ✅ | soft-landing |
| Any other | ❌ InvalidOperationException |

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| src/Aura.Domain/FocusState/ | New | FocusStateType.cs, FocusState.cs |
| src/Aura.Application/Ports/IFocusStateResolver.cs | New | Port interface |
| src/Aura.Application/Services/FocusStateResolver.cs | New | Resolver implementation |
| tests/Aura.UnitTests/Triage/FocusStateMachineTests.cs | New | Unit tests |
| docs/architecture/triage/03-focus-state-machine.md | Modified | From "deferred" to implemented |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Missing signal ports (calendar, preferences) require stubs | Medium | Resolver with configurable signal sources, documented placeholder dependencies |
| Transition model assumptions wrong | Low | Tests for every valid/invalid transition — easy to correct |
| Signal conflict (calendar vs preference) unresolved | Medium | Documented priority order in resolver |

## Rollback Plan

Revert commit and/or delete src/Aura.Domain/FocusState/, the port, the resolver, and the test file.

## Dependencies

None

## Success Criteria

- [ ] FocusStateType enum con 4 estados
- [ ] FocusState sealed class con transiciones guardadas — toda transición inválida lanza InvalidOperationException
- [ ] IFocusStateResolver port definido e implementado
- [ ] Resolver produce estado determinístico dadas las mismas señales
- [ ] Tests unitarios pasan
- [ ] dotnet test Aura.sln sigue verde
- [ ] docs/architecture/triage/03-focus-state-machine.md actualizado
