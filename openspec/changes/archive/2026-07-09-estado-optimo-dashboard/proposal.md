# Proposal: Dashboard Estado Óptimo (Cero Pendientes)

## Intent

Replace generic "No items" empty state (lines 55-61 of `PrioritySummaryCards.razor`) with per-card positive/encouraging empty states that reduce cognitive load. Each card gets a bespoke icon, title, subtitle, and footer link when its item count is zero.

## Scope

### In Scope
- Per-card empty state template rendering in `PrioritySummaryCards.razor`
- `PrioritySummaryCard` model extension with empty-state properties
- Card builder updates in `PrioritySummaryService.BuildCards()`
- CSS styles for the new empty state layout
- Unit tests for each card's positive empty state

### Out of Scope
- Detail page empty states (Teams, Outlook, Calendar, PR pages)
- Other dashboard components (status cards, ranked summary, feed panels)
- Animation/transition effects on entering empty state

## Capabilities

### New Capabilities
None

### Modified Capabilities
- `dashboard-inbox-preview`: Empty state behavior for Teams Mentions, Outlook, and Schedule Today cards
- `pr-connector-ui`: Empty state for Pull Requests card; `PrioritySummaryCard` model extensions

## Approach

1. Add properties to `PrioritySummaryCard` record: `EmptyIcon`, `EmptyTitle`, `EmptySubtitle`, `EmptyFooterLabel`, `EmptyFooterUrl`
2. Set per-card values in `PrioritySummaryService.BuildCards()`:

   | Card | Icon | Title | Subtitle |
   |------|------|-------|----------|
   | Teams Mentions | check_circle | Inbox Zero | Everything is optimal. Your cognitive load is clear. |
   | Outlook | mark_email_read | All Caught Up | Take a deep breath. |
   | Schedule Today | event_available | Schedule Clear | No meetings for today. Enjoy your focused time. |
   | Pull Requests | verified | Queue Empty | No pending reviews. Your workspace is clear. |

3. Update `PrioritySummaryCards.razor` empty block (lines 55-61) to render icon + title + subtitle + footer link
4. Add CSS classes `.priority-card__empty-state` in `stitch-dashboard.css`
5. Update tests in `PrioritySummaryCardsRenderingTests.cs`

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.UI/Services/IPrioritySummaryService.cs` | Modified | Add empty-state props to PrioritySummaryCard |
| `src/Aura.UI/Services/PrioritySummaryService.cs` | Modified | Set per-card empty state values in BuildCards() |
| `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor` | Modified | Replace "No items" with new empty template |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Modified | Add `.priority-card__empty-state` styles |
| `tests/Aura.UnitTests/Dashboard/PrioritySummaryCardsRenderingTests.cs` | Modified | Add empty state rendering tests |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Overflow/truncation of longer subtitles | Low | Use CSS text-overflow + test with max-length strings |

## Rollback Plan

`git checkout` the 5 affected files to restore current state.

## Dependencies

None.

## Success Criteria

- [ ] Teams card empty shows `check_circle` + "Inbox Zero" + "Everything is optimal..."
- [ ] Outlook card empty shows `mark_email_read` + "All Caught Up" + "Take a deep breath."
- [ ] Schedule card empty shows `event_available` + "Schedule Clear" + "No meetings..."
- [ ] PR card empty shows `verified` + "Queue Empty" + "No pending reviews..."
- [ ] Non-empty cards continue rendering normally (no regression)
- [ ] All existing and new unit tests pass: `dotnet test Aura.sln`
