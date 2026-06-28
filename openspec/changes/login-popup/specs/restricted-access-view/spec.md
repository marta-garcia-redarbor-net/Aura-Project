# Delta for Restricted Access View

## MODIFIED Requirements

### Requirement: Restricted Access Container

The system SHALL render a full-page overlay with `data-testid="restricted-access-view"` when the
user is not authenticated and accesses any dashboard route. The overlay MUST render immediately on
page load — no HTTP-level redirect, no blank-page interstitial, no loading spinner before content.
(Previously: No timing constraint; did not prohibit HTTP-level redirect before render.)

#### Scenario: Unauthenticated user sees restricted view immediately

- GIVEN the user is not authenticated
- WHEN the user navigates to `/`
- THEN the restricted access view renders without a redirect to Entra ID
- AND `data-testid="restricted-access-view"` is present in the DOM
- AND the URL remains `/` (no external redirect)

#### Scenario: Authenticated user bypasses restricted view

- GIVEN the user is authenticated
- WHEN the user navigates to a protected dashboard route
- THEN the restricted access view is NOT rendered
- AND the normal dashboard content is displayed

### Requirement: Blurred Dashboard Shell

The system SHALL render a dashboard shell skeleton (sidebar + header) behind the login card, with
CSS `filter: blur(8px)` and `pointer-events: none` applied to prevent interaction.
(Previously: Unchanged — no functional change, retained for archive completeness.)

#### Scenario: Blurred shell is visible behind login card

- GIVEN the restricted access view is rendered
- WHEN the user views the page
- THEN a dashboard shell skeleton is visible behind the login card
- AND the shell has a blur effect applied
- AND the shell is not interactive (pointer-events disabled)

### Requirement: Centered Login Card

The system SHALL display a centered login card with the application name and a single
"Sign in with Microsoft" primary button. The card MUST NOT show a second authentication
button in `UseEntraId=true` mode. All interactive elements MUST carry `data-testid` attributes.
(Previously: Card showed two buttons in Entra mode; no data-testid requirement stated here.)

#### Scenario: Login card shows only Microsoft button in Entra mode

- GIVEN `UseEntraId=true` and the restricted access view is rendered
- WHEN the user views the login card
- THEN the card displays "Aura" as the application title
- AND exactly one button is visible: "Sign in with Microsoft" with `data-testid="login-microsoft-btn"`
- AND no secondary button is rendered

#### Scenario: Login card displays mock button in dev mode

- GIVEN `UseEntraId=false` and the restricted access view is rendered
- WHEN the user views the login card
- THEN a dev fallback button is present with `data-testid="login-dev-btn"`

### Requirement: Popup Auth Flow

The system SHALL open a browser popup to `/login/challenge` — NOT a manually constructed
Entra URL — when the user clicks "Sign in with Microsoft". OIDC state and nonce MUST be
owned by the server-side OIDC middleware, not constructed client-side.
(Previously: `BuildAuthUrl()` constructed a full Entra authorization URL with manual state/nonce.)

#### Scenario: Button click opens popup to /login/challenge

- GIVEN the restricted access view is rendered and `UseEntraId=true`
- WHEN the user clicks "Sign in with Microsoft"
- THEN a popup window opens to `/login/challenge`
- AND the popup URL is `/login/challenge` (not an Entra ID URL)

#### Scenario: Popup completion triggers Navigation.Refresh

- GIVEN the popup has completed OIDC authentication
- WHEN the popup posts `{ type: 'auth-success' }` to the opener
- THEN the opener calls `Navigation.Refresh()` (force-load)
- AND the Blazor re-evaluates auth state and renders the dashboard

#### Scenario: Popup blocked shows fallback

- GIVEN the browser blocks popups
- WHEN the user clicks "Sign in with Microsoft"
- THEN a fallback message is displayed with a direct link to complete login
- AND no unhandled JavaScript exception occurs

### Requirement: CSS Animations

The system SHALL apply fade-in and zoom-in animations to the login card on initial render, and a
shimmer skeleton animation to the blurred background content.
(Previously: Unchanged — retained for archive completeness.)

#### Scenario: Login card animates on mount

- GIVEN the restricted access view is rendered
- WHEN the page loads
- THEN the login card fades in and zooms from a smaller scale to full size
- AND the blurred background displays a shimmer animation

## REMOVED Requirements

### Requirement: Entra ID Mode Compatibility (two-button variant)

(Reason: The redesign mandates a single "Sign in with Microsoft" button in Entra mode. The
scenario "Dev mode shows both buttons" is now covered under Centered Login Card. The standalone
requirement duplicated button-visibility rules that are now consolidated.)
(Migration: Tests asserting two buttons in Entra mode must be updated to assert exactly one button.)
