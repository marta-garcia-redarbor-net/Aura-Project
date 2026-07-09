# Proposal: Landing Page Rework

## Intent

Current landing page `/` is the authenticated dashboard — users must log in before seeing any value. A public landing page with the Stitch design ("Aura | Refined Enterprise Landing Page") converts first-time visitors and reduces auth barrier friction.

## Scope

### In Scope
- Public landing page at `/` from Stitch design (hero, features, CTAs, footer, fixed header)
- PriorityDashboard route changes from `/` to `/dashboard`
- Auto-redirect authenticated users at `/` → `/dashboard`
- "Explore Demo Mode" triggers fake auth (no Microsoft login) with demo claims
- Post-logout redirects to `/`

### Out of Scope
- A/B testing, analytics, SEO metadata, i18n
- Landing page variant generation
- Changes to dashboard functionality (data, layout) beyond route

## Capabilities

### New Capabilities
- `landing-page`: Public Blazor component matching Stitch design. Sections: fixed header ("Login / Access Aura" button), hero (two CTAs: "Login / Access Aura" + "Explore Demo Mode"), problem/solution grid, features bento grid, bottom CTA, footer. Dark theme matching existing Aura design system. Publicly accessible (no auth required).
- `demo-auth`: Minimal API endpoint `/login/demo` creating fake authentication cookie with demo claims (name, email, "Demo" role), then redirects to `/dashboard`.

### Modified Capabilities
- `dashboard-routing`: PriorityDashboard.razor `@page "/"` → `@page "/dashboard"`. Auto-redirect for authenticated visitors at `/` → `/dashboard`.
- `api-authentication`: `/authentication/callback` (no opener) and `/login/dev` redirect targets change from `/` to `/dashboard`.
- `restricted-access-view`: Login card moves to landing page CTAs. RestrictedAccessView only shown on direct unauthenticated `/dashboard` access.
- `demo-mode`: Demo identity determined by **session claims**, not `UseEntraId` config. The `/login/demo` endpoint sets an `aura_demo_mode` claim on the auth cookie. UI reads this claim — not config files — to show/hide demo controls (enter demo data, reset demo). Config `UseEntraId` continues to control real vs dev Entra ID auth but no longer drives demo-mode UI visibility.

## Approach

1. Create `LandingPage.razor` — public component consuming no API data, rendering all Stitch sections
2. Create `LoginButton.razor` — reusable CTA invoking `AuthPopupService` for popup auth flow
3. Update `PriorityDashboard.razor` route to `/dashboard`
4. Add `/login/demo` endpoint — creates demo claims cookie with `aura_demo_mode` claim + redirects to `/dashboard`
5. Update `AuthenticationCallback.razor.cs` redirect target `/` → `/dashboard`
6. Remove config-driven demo mode checks from UI components; replace with `aura_demo_mode` claim check
7. "Reset demo" button stays as-is: clears demo data in current session, no redirect
8. Use existing `AnonymousLayout` for landing page (public route)

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `UI/Pages/LandingPage.razor` | New | Public landing component |
| `UI/Components/Auth/LoginButton.razor` | New | Reusable login CTA |
| `UI/Program.cs` | Modified | Add `/login/demo` endpoint, update redirects |
| `UI/Components/Dashboard/PriorityDashboard.razor` | Modified | Route `/` → `/dashboard` |
| `UI/Components/Auth/RestrictedAccessView.razor` | Modified | Login CTAs moved to landing |
| `UI/Components/Auth/AuthenticationCallback.razor.cs` | Modified | Redirect `/` → `/dashboard` |
| `UI/Components/Routes.razor` | Modified | Landing route is anonymous |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Existing tests assert `/` route on PriorityDashboard | High | Update route assertions alongside component route |
| Popup auth redirect to `/dashboard` breaks existing auth flow | Low | Post-login redirect is a URL change only — auth cookie unchanged |
| Demo mode users may lack proper auth for API calls | Low | Demo claims include token claim; DevAccessTokenHandler covers this path |

## Rollback Plan

- Revert PriorityDashboard `@page "/dashboard"` to `@page "/"`
- Remove `LandingPage.razor` and `LoginButton.razor`
- Restore all callback redirects from `/dashboard` to `/`
- Remove `/login/demo` endpoint
- Restore config-driven demo mode checks in UI components
- Remove `aura_demo_mode` claim checks from UI
- Remove auto-redirect logic from landing page route

## Dependencies

- Stitch design screen (ID: 5729c2ce011b482d80a6a76b66e66219) — already available
- Existing CSS/design tokens in `stitch-dashboard.css` cover dark theme needs

## Success Criteria

- [ ] Unauthenticated visitor at `/` sees Stitch landing page, not login card
- [ ] Authenticated user at `/` redirects to `/dashboard`
- [ ] "Login / Access Aura" → popup auth → `/dashboard`
- [ ] "Explore Demo Mode" → fake auth → `/dashboard` with demo claims
- [ ] Logout redirects to landing page `/`
- [ ] Existing `dotnet test Aura.sln` passes (after route updates)
