# Delta for Restricted Access View

## MODIFIED Requirements

### Requirement: Restricted Access Container

The system SHALL render a full-page overlay with `data-testid="restricted-access-view"` when the user is not authenticated and accesses `/dashboard`. The overlay MUST render immediately on page load — no HTTP-level redirect, no blank-page interstitial, no loading spinner before content. This view is NOT shown at `/` (landing page is public).
(Previously: Triggered on `/` for unauthenticated users.)

#### Scenario: Unauthenticated user sees restricted view at dashboard

- GIVEN the user is not authenticated
- WHEN the user navigates to `/dashboard`
- THEN the restricted access view renders without a redirect to Entra ID
- AND `data-testid="restricted-access-view"` is present in the DOM
- AND the URL remains `/dashboard` (no external redirect)

#### Scenario: Unauthenticated user at landing sees public page

- GIVEN the user is not authenticated
- WHEN the user navigates to `/`
- THEN the public landing page renders
- AND the restricted access view is NOT shown

#### Scenario: Authenticated user bypasses restricted view

- GIVEN the user is authenticated
- WHEN the user navigates to `/dashboard`
- THEN the restricted access view is NOT rendered
- AND the normal dashboard content is displayed

### Requirement: Centered Login Card

The system SHALL display a centered card with a message indicating dashboard access requires authentication and a link to the landing page at `/`. The full login experience (Microsoft sign-in, demo mode) is now on the landing page. All interactive elements MUST carry `data-testid` attributes.
(Previously: Card showed "Sign in with Microsoft" button with popup auth flow directly.)

#### Scenario: Restricted view shows link to landing

- GIVEN the restricted access view is rendered at `/dashboard`
- WHEN the user views the card
- THEN a message indicates authentication is required
- AND a link or button to `/` is present with `data-testid="restricted-go-login-btn"`

#### Scenario: Authenticated user bypasses restricted view

- GIVEN the user is authenticated
- WHEN the user navigates to `/dashboard`
- THEN the restricted access view is NOT rendered
- AND the normal dashboard content is displayed
