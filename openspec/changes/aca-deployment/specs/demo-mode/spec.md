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
