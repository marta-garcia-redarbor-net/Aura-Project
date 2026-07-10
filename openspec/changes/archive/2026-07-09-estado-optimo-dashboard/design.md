# Design: Dashboard Estado Óptimo (Positive Empty States)

## Technical Approach

Extend the `PrioritySummaryCard` record with four optional init-only properties for empty-state content. Update the Razor component to render a styled empty state (icon + title + subtitle) when a card has zero items, and show the footer with a card-specific link label. Populate the properties per-card in `PrioritySummaryService.BuildCards()`. Add CSS for the new empty-state layout and remove the dimming opacity.

No infrastructure, API, or domain-layer changes. All modifications are confined to `Aura.UI` (presentation layer).

## Architecture Decisions

### Decision: Init-only properties vs positional parameters

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Nullable positional params with defaults | Consistent with existing pattern but breaks if order changes | **Rejected** — 4 new params would bloat the constructor |
| Init-only properties with defaults | No breaking change to existing constructors or tests | **Chosen** — follows `IsPrCard`/`PrItems` precedent |

**Rationale**: The record already uses init-only properties (`IsPrCard`, `PrItems`) for optional data. Adding `EmptyIcon`, `EmptyTitle`, `EmptySubtitle`, `EmptyLinkLabel` the same way keeps all 13 existing constructor call sites (tests + service) working without modification.

### Decision: Reuse `ViewAllUrl` for empty footer link

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Add 5th `EmptyFooterUrl` property (per spec) | More flexible but redundant — all 4 cards link to their own `ViewAllUrl` | **Rejected** — unnecessary duplication |
| Reuse existing `ViewAllUrl` | Zero new URL plumbing, footer link just changes its label | **Chosen** — spec says "pointing to the card's ViewAllUrl" |

**Rationale**: The spec scenarios explicitly state "pointing to the card's `ViewAllUrl`". A separate URL property adds complexity with no current use case.

### Decision: Remove `.priority-card--empty` opacity entirely

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Keep opacity 0.5 on card, override on empty-state children | Complex selector nesting, fragile | **Rejected** |
| Remove opacity, rely on empty-state visual design for differentiation | Simple, positive tone not undermined by dimming | **Chosen** |

**Rationale**: The purpose of empty states is to communicate "everything is optimal." Dimming the card at 50% opacity contradicts that message. The empty-state icon and typography provide sufficient visual differentiation.

## Data Flow

```
PrioritySummaryService.BuildCards()
    │
    ├── Sets EmptyIcon/Title/Subtitle/LinkLabel per card
    │
    ▼
PrioritySummaryCards.razor (foreach card)
    │
    ├── isEmpty? ──YES──→ Render .priority-card__empty-state
    │                     (icon + title + subtitle)
    │                     + footer with EmptyLinkLabel
    │
    └── isEmpty? ──NO───→ Render normal items list
                          + footer with "View all N items"
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.UI/Services/IPrioritySummaryService.cs` | Modify | Add 4 init-only properties to `PrioritySummaryCard` record |
| `src/Aura.UI/Services/PrioritySummaryService.cs` | Modify | Populate empty-state properties per card in `BuildCards()` |
| `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor` | Modify | Replace "No items" block (L55-61) with empty-state template; show footer when empty (L190) |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Modify | Add `.priority-card__empty-state` styles; remove `.priority-card--empty` opacity |
| `tests/Aura.UnitTests/Dashboard/PrioritySummaryCardsRenderingTests.cs` | Modify | Add 4 empty-state rendering tests (one per card) |

## Interfaces / Contracts

### PrioritySummaryCard — new properties

```csharp
public sealed record PrioritySummaryCard(
    // ... existing positional params unchanged ...
    List<UpcomingMeetingResponse>? CalendarItems)
{
    // Existing
    public bool IsCalendarCard => CalendarItems is not null;
    public bool IsPrCard { get; init; }
    public List<PrPreviewItemResponse>? PrItems { get; init; }
    public int TotalCount => PrItems?.Count ?? PreviewItems?.Count ?? CalendarItems?.Count ?? 0;

    // NEW — empty state
    public string? EmptyIcon { get; init; }
    public string? EmptyTitle { get; init; }
    public string? EmptySubtitle { get; init; }
    public string? EmptyLinkLabel { get; init; }
}
```

### Per-card values (BuildCards)

| Card | EmptyIcon | EmptyTitle | EmptySubtitle | EmptyLinkLabel |
|------|-----------|------------|---------------|----------------|
| Teams | `check_circle` | Inbox Zero | Everything is optimal. Your cognitive load is clear. | View All Mentions |
| Outlook | `mark_email_read` | All Caught Up | Take a deep breath. | See All Emails |
| Schedule | `event_available` | Schedule Clear | No meetings for today. Enjoy your focused time. | View Full Schedule |
| PRs | `verified` | Queue Empty | No pending reviews. Your workspace is clear. | View All Repositories |

### Razor empty-state template (replaces L55-61)

```razor
@if (isEmpty)
{
    <div class="priority-card__empty-state" data-testid="priority-card-empty-state">
        <span class="material-symbols-outlined priority-card__empty-icon">@card.EmptyIcon</span>
        <p class="priority-card__empty-title">@card.EmptyTitle</p>
        <p class="priority-card__empty-subtitle">@card.EmptySubtitle</p>
    </div>
}
```

### Footer change (L190)

Remove `@if (!isEmpty)` guard. Inside the footer, conditionally render the link label:
- When empty: `@card.EmptyLinkLabel` pointing to `@card.ViewAllUrl`
- When not empty: existing `"View all {count} {itemsLabel}"` text

### CSS additions

```css
.priority-card__empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    flex: 1;
    padding: 1.5rem 0;
}

.priority-card__empty-icon {
    font-size: 48px;
    color: var(--aura-status-healthy, #4caf50);
}

.priority-card__empty-title {
    font-size: 15px;
    font-weight: 600;
    color: var(--aura-on-surface);
    margin: 0;
}

.priority-card__empty-subtitle {
    font-size: 12px;
    color: var(--aura-on-surface-variant);
    text-align: center;
    margin: 0;
    max-width: 20ch;
}

/* REMOVE: .priority-card--empty { opacity: 0.5; } */
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit (bUnit) | Each card renders correct empty icon, title, subtitle when `TotalCount == 0` | 4 tests — one per card type, assert text content and icon presence |
| Unit (bUnit) | Footer shows `EmptyLinkLabel` when empty, "View all N items" when not | 2 tests — empty footer + regression for non-empty footer |
| Unit (bUnit) | Non-empty cards do NOT render `.priority-card__empty-state` | 1 test — assert `QuerySelector("[data-testid='priority-card-empty-state']")` is null |
| Unit (service) | `BuildCards()` populates empty-state properties for all 4 cards | 1 test — assert non-null values match expected per-card table |

**Test command**: `dotnet test Aura.sln`

## Migration / Rollout

No migration required. All new properties are optional with null defaults. Existing card construction (13 call sites in tests + 4 in service) compiles without changes. The Razor template falls back gracefully if properties are null (renders empty spans).

## Open Questions

None.
