# Demo Mode

Data-loading buttons, Qdrant-optional fallback.

| Req | Str |
|---|---|
| `DemoMode__Enabled` toggle controls button visibility | MUST |
| Buttons use standard ports | MUST |
| Qdrant down degrades gracefully | MUST |

#### Scenario: Toggle shows buttons

- GIVEN `DemoMode__Enabled=true`
- WHEN UI loads
- THEN "Load Sample Data" rendered

#### Scenario: Qdrant fallback

- GIVEN Qdrant unreachable
- WHEN button clicked
- THEN items persist without embeddings

---

### Requirement: Demo Wizard Tooltip Repositioning at ≤768px

The demo wizard tooltip (`.demo-wizard`) MUST use `position: fixed; bottom: 1rem; left: 50%; transform: translateX(-50%)` at ≤768px instead of `position: absolute` relative to the demo button. The tooltip MUST remain tappable and fully visible on 390px viewports.

(Previously: Tooltip was `position: absolute` anchored below the demo button with `left: 50%; transform: translateX(-50%)`.)

#### Scenario: Demo wizard visible on 390px viewport

- GIVEN viewport is 390px wide and demo user is active
- WHEN the wizard tooltip is shown
- THEN it is centered at the bottom of the viewport
- AND the text is fully readable
- AND the tooltip is tappable

#### Scenario: Demo wizard does not overlap sidebar drawer

- GIVEN viewport ≤768px and sidebar drawer is open
- WHEN the demo wizard is visible
- THEN the wizard z-index is below the sidebar drawer (z-index < 40) OR the wizard is hidden while drawer is open
