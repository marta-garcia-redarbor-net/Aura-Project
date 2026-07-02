# Stable Selector Inventory

**Capability**: `test-baseline-guardrails`  
**Rule**: Selectors in this file are stable contracts. Removal or rename MUST be accompanied by
test adaptation in the same PR. See `spec.md` REQ-02.

---

## Shell Structure

These selectors are emitted by `MainLayoutAuthenticated.razor` and `Sidebar.razor`.
They are present whenever an authenticated user views any dashboard route.

| Selector | Component | Notes |
|----------|-----------|-------|
| `data-testid="dashboard-shell"` | `MainLayoutAuthenticated.razor` | Root authenticated shell container |
| `data-testid="dashboard-sidebar"` | `Sidebar.razor` | Navigation sidebar |
| `data-testid="dashboard-header"` | `Header.razor` | Top header bar |
| `data-testid="dashboard-main"` | `MainLayoutAuthenticated.razor` | Main content area |

---

## Dashboard State Panel

Emitted by `DashboardStatePanel.razor`. Exactly one state marker is present per render cycle.

| Selector | Condition |
|----------|-----------|
| `data-testid="dashboard-state-loading"` | API call in flight |
| `data-testid="dashboard-state-empty"` | API returned empty card list |
| `data-testid="dashboard-state-error"` | API threw or returned error |
| `data-testid="dashboard-state-populated"` | API returned ≥1 card |

---

## Dashboard Cards

Emitted by `DashboardCards.razor` when state is `Populated`.

| Selector | Condition |
|----------|-----------|
| `data-testid="dashboard-card-status"` | Each card row status indicator |
| `data-testid="dashboard-header-user"` | User display name in header (populated state) |

---

## System Status Panel

Emitted by `SystemStatusPanel.razor`.

| Selector | Condition |
|----------|-----------|
| `data-testid="system-status-panel"` | Always present when panel renders |
| `data-testid="system-status-list"` | Indicator list container |
| `data-testid="system-indicator-state-api"` | API health indicator |
| `data-testid="system-indicator-state-qdrant"` | Qdrant health indicator |
| `data-testid="system-indicator-state-mockauth"` | Mock auth health indicator |
| `data-testid="system-status-error"` | Panel error state (API threw) |

---

## Module Progress Panel

Emitted by `ModuleProgressPanel.razor`.

| Selector | Condition |
|----------|-----------|
| `data-testid="module-progress-panel"` | Always present when panel renders |
| `data-testid="module-progress-list"` | Module list container |
| `data-testid="module-progress-empty"` | No module entries |
| `data-testid="module-progress-error"` | API threw |

---

## Inbox Preview Panel

Emitted by `InboxPreviewPanel.razor`.

| Selector | Condition |
|----------|-----------|
| `data-testid="inbox-preview-panel"` | Always present |
| `data-testid="inbox-preview-loading"` | API call in flight |
| `data-testid="inbox-preview-empty"` | No inbox groups |
| `data-testid="inbox-preview-error"` | API threw |
| `data-testid="inbox-preview-populated"` | ≥1 inbox group |
| `data-testid="inbox-preview-item"` | Per item row |
| `data-testid="inbox-preview-item-title"` | Item title |
| `data-testid="inbox-preview-item-sender"` | Present when `Sender != null` |
| `data-testid="inbox-preview-item-snippet"` | Present when `Snippet != null` |
| `data-testid="inbox-preview-item-deeplink"` | Present when `DeepLink != null` |
| `data-testid="inbox-preview-item-sync-state"` | Present when `SyncState != null` |

---

## Morning Summary Preview Panel

Emitted by `MorningSummaryPreviewPanel.razor`.

| Selector | Condition |
|----------|-----------|
| `data-testid="morning-summary-preview-panel"` | Always present |
| `data-testid="morning-summary-preview-loading"` | API call in flight |
| `data-testid="morning-summary-preview-empty"` | No summary entries |
| `data-testid="morning-summary-preview-error"` | API threw |
| `data-testid="morning-summary-preview-populated"` | ≥1 summary entry |
| `data-testid="morning-summary-preview-rank"` | Per-entry rank label |

---

## Sync Status Panel

Emitted by `SyncStatusPanel.razor`.

| Selector | Condition |
|----------|-----------|
| `data-testid="sync-status-panel"` | Always present |
| `data-testid="sync-now-button"` | Sync trigger button |
| `data-testid="sync-source-progress-teams"` | Teams source row |
| `data-testid="sync-source-progress-outlook"` | Outlook source row |

`sync-source-*` selectors are deprecated and MUST NOT be reintroduced in new tests.

---

## Notes

- Selectors named `blurred-*` (`data-testid="blurred-sidebar"`, `data-testid="blurred-header"`) are
  in `RestrictedAccessView` (unauthenticated state). They are NOT stable test contract selectors —
  they may change as the login UI evolves.
- Browser-level tests (`DashboardRootBrowserTests`, `HealthRouteBrowserTests`) assert a subset of
  the shell selectors above using Playwright locators.
- Host-level smoke tests (`InitialDashboardSmokeTests`) assert full HTML presence via string search.
