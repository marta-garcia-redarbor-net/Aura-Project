# Landing Page Specification

## Purpose

Public-facing landing page at `/` that introduces Aura to first-time visitors. Matches the Stitch design "Aura | Refined Enterprise Landing Page". No authentication required. Authenticated users are auto-redirected to `/dashboard`.

## Requirements

### Requirement: Public Route at Root

The system MUST serve a Blazor component (`LandingPage.razor`) at `/` using `AnonymousLayout`. The page MUST be accessible without authentication. All content MUST be static (no API data dependencies).

#### Scenario: Unauthenticated visitor sees landing page

- GIVEN the application is running
- WHEN an unauthenticated user navigates to `/`
- THEN the landing page renders with Stitch dark-mode design
- AND no authentication challenge is triggered

#### Scenario: Authenticated user auto-redirects to dashboard

- GIVEN the user has a valid authentication cookie
- WHEN the user navigates to `/`
- THEN the system redirects to `/dashboard`
- AND the landing page is NOT rendered

### Requirement: Fixed Header

The system MUST render a fixed header with the Aura brand and a "Login / Access Aura" button. The button MUST invoke the `AuthPopupService` popup flow. The header MUST remain visible during scroll.

#### Scenario: Header renders on landing

- GIVEN the landing page is rendered
- WHEN the user views the page
- THEN a fixed header is visible with the Aura brand
- AND a "Login / Access Aura" button is present with `data-testid="landing-login-btn"`

#### Scenario: Login button triggers popup auth

- GIVEN the landing page is rendered
- WHEN the user clicks "Login / Access Aura"
- THEN a popup opens to `/login/challenge`
- AND the current page remains at `/`

### Requirement: Hero Section with Dual CTAs

The system MUST render a hero section with two call-to-action buttons: "Login / Access Aura" (primary) and "Explore Demo Mode" (secondary). The primary CTA MUST invoke popup auth. The secondary CTA MUST navigate to `/login/demo`.

#### Scenario: Hero renders both CTAs

- GIVEN the landing page is rendered
- WHEN the user views the hero section
- THEN "Login / Access Aura" button is present with `data-testid="hero-login-btn"`
- AND "Explore Demo Mode" button is present with `data-testid="hero-demo-btn"`

#### Scenario: Demo CTA navigates to demo auth

- GIVEN the landing page is rendered
- WHEN the user clicks "Explore Demo Mode"
- THEN the browser navigates to `/login/demo`

### Requirement: Content Sections

The system MUST render the following sections in order: problem/solution grid, features bento grid, bottom CTA, and footer. All sections MUST match the Stitch design dark theme using existing `stitch-dashboard.css` tokens.

#### Scenario: All sections render in order

- GIVEN the landing page is rendered
- WHEN the user scrolls through the page
- THEN problem/solution grid is visible
- AND features bento grid is visible
- AND bottom CTA section is visible
- AND footer is visible

#### Scenario: Dark theme consistency

- GIVEN the landing page is rendered
- WHEN visual inspection occurs
- THEN colors match existing Aura design system tokens
- AND no light-mode defaults are applied

### Requirement: Post-Logout Redirect

The system MUST redirect users to `/` (landing page) after logout completes.

#### Scenario: Logout returns to landing

- GIVEN the user is authenticated and on `/dashboard`
- WHEN the user triggers logout
- AND logout completes
- THEN the browser is at `/`
- AND the landing page renders
