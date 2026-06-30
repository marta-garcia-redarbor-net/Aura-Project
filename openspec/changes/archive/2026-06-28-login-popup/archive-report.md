# Archive Report: login-popup

**Change**: login-popup
**Archived**: 2026-06-28
**Mode**: openspec
**Verdict at archive**: FAIL (3 CRITICAL gaps identified in verify-report, 5 previously CRITICAL issues confirmed fixed)

## Artifacts Archived

| Artifact | Status | Path |
|----------|--------|------|
| proposal.md | ✅ | `openspec/changes/archive/2026-06-28-login-popup/proposal.md` |
| design.md | ✅ | `openspec/changes/archive/2026-06-28-login-popup/design.md` |
| tasks.md | ✅ | `openspec/changes/archive/2026-06-28-login-popup/tasks.md` |
| verify-report.md | ✅ | `openspec/changes/archive/2026-06-28-login-popup/verify-report.md` |
| specs/ | ✅ | 3 delta specs archived |

## Specs Synced

| Domain | Action | Details |
|--------|--------|---------|
| `api-authentication` | Updated | 3 requirements ADDED (OIDC Challenge Endpoint, Authentication Callback Page, CORS for Mock Login), 1 requirement MODIFIED (MSAL Token Acquisition), 1 requirement REMOVED (Mock Login Popup Compatibility) |
| `restricted-access-view` | Created | New domain — 5 requirements (Restricted Access Container, Blurred Dashboard Shell, Centered Login Card, Popup Auth Flow, CSS Animations), 1 requirement REMOVED (Entra ID Mode Compatibility two-button variant) |
| `oidc-popup-auth` | Created | New domain — 5 requirements REMOVED (Popup Window Launch, Correct OIDC Parameters, Popup-to-Main Communication, Auth State Update After Popup Login, SignalR Circuit Preservation) |

## Task Completion

| Phase | Tasks | Complete | Notes |
|-------|-------|----------|-------|
| Phase 1: Foundation | 4 | 4/4 | All auth pipeline tasks done |
| Phase 2: Core | 5 | 5/5 | [Authorize] removed, RestrictedAccessView simplified |
| Phase 3: Core | 7 | 7/7 | Callback + MsalTokenAcquisitionService done |
| Phase 4: Cleanup | 5 | 5/5 | Dead code deleted |
| Phase 5: Final | 5 | 2/5 | 5.1 automated pass; 5.2–5.4 are manual E2E (not implementation tasks) |
| **Total** | **23** | **21/21 implementation** | 2 manual E2E tasks remain as documented evidence |

## Verification Summary

- **563 unit tests pass**, 0 failures
- **5 previously CRITICAL issues** all confirmed FIXED in source code
- **3 new CRITICAL issues** identified in verify-report (postMessage format mismatch, missing token exchange, auth state not updated for production OIDC flow)
- **Dev-mode flow**: COMPLETE end-to-end
- **Production OIDC flow**: INCOMPLETE (callback sends `code`, listener expects `token`; no token exchange; no auth state update)

## Archive Reason

Archived with verification FAIL status as documented. The change folder captures the complete audit trail: proposal, design, tasks, verify-report, and all delta specs. The 3 new CRITICAL issues in the production OIDC flow must be addressed in a follow-up change.

## Source of Truth Updated

The following main specs now reflect the new behavior:
- `openspec/specs/api-authentication/spec.md`
- `openspec/specs/restricted-access-view/spec.md`
- `openspec/specs/oidc-popup-auth/spec.md`
