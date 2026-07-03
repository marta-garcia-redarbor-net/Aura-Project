# Triage — Focus State Machine

## Status

**Implemented** as part of `w3-h1-focus-state-machine`.

## States

Aura defines four focus states:

| State | Meaning |
|-------|---------|
| DeepWork | User is in deep concentration. No interruptions except critical urgency. |
| WindowOfOpportunity | User is receptive to interruptions. Default state. |
| Away | User is unavailable (meeting, DND, away from keyboard). No interruptions. |
| Recovery | Post-interruption or post-meeting. Reacclimation period before DeepWork. |

## Transition Matrix

| From → To | Allowed | Trigger |
|-----------|---------|---------|
| DeepWork → WindowOfOpportunity | ✅ | break |
| WindowOfOpportunity → Away | ✅ | dnd / meeting |
| Away → Recovery | ✅ | end |
| Away → DeepWork | ✅ | direct focus return |
| Recovery → DeepWork | ✅ | refocus |
| Recovery → WindowOfOpportunity | ✅ | soft-landing |
| Any other | ❌ InvalidOperationException |

## Integration Point

`IFocusStateResolver` (in `Aura.Application.Ports`) is the contract for determining the current focus state:

```csharp
Task<FocusState> ResolveAsync(string userId, CancellationToken cancellationToken = default);
```

The default implementation (`FocusStateResolver` in `Aura.Application.Services`) returns `WindowOfOpportunity` as a placeholder. Real signal sources (calendar, activity, preferences) will be wired in W3-H2 when the interruption engine is built.

## Domain Types

| Type | Location | Purpose |
|------|----------|---------|
| `FocusStateType` | `Aura.Domain.FocusState` | Enum: DeepWork, WindowOfOpportunity, Away, Recovery |
| `FocusState` | `Aura.Domain.FocusState` | Sealed class with 6 guarded transitions |
| `IFocusStateResolver` | `Aura.Application.Ports` | Port for resolving current focus state |
| `FocusStateResolver` | `Aura.Application.Services` | Stub returning WindowOfOpportunity by default |
