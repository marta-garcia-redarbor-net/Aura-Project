## Exploration: w1-h6-dashboard-inicial

### Current State
Aura has its base `Aura.Api` project (using .NET SDK Web), `Aura.Infrastructure`, `Aura.Application`, `Aura.Domain`, and `Aura.Workers`. H4 (Kernel Skeleton) and H5 (Mock Auth) are already implemented. There is currently no UI project or Blazor Server configured in the solution. The user has a base dashboard design generated in Stitch (static HTML/CSS/Tailwind) that needs to be integrated to satisfy H6 (Dashboard Inicial).

### Affected Areas
- `src/Aura.UI/` (New Project) — Needs to be created.
- `Aura.sln` — Needs to include the new project.
- Playwright Tests — Needs a new project or folder for UI smoke tests.

### Approaches

1. **Host Blazor Server inside `Aura.Api`**
   - **Pros:** Fewer projects, simpler deployment (everything in one process).
   - **Cons:** Violates the architectural intent defined in `docs/ai/04-ui-incremental-strategy.md` that explicitly states "La UI consume exclusivamente los endpoints de Aura.Api". If Blazor is inside the API, there's a strong temptation to bypass HTTP endpoints and inject Application/Infrastructure services directly into the UI components.
   - **Effort:** Low.

2. **Create a separate `Aura.UI` Blazor Server project**
   - **Pros:** Strictly enforces the architectural rule (UI consumes DTOs and endpoints of `Aura.Api`). Clear separation of concerns. `Aura.Api` remains pure REST/Webhooks.
   - **Cons:** One more project in the solution, requires configuring `HttpClient` to communicate with the API.
   - **Effort:** Medium.

### Recommendation
**Approach 2 (Separate `Aura.UI` project)** is the recommended path to strictly adhere to the Clean Architecture guard and the UI incremental strategy. The Blazor Server UI should only communicate with `Aura.Api` via standard HTTP calls.

**Integration of Stitch Design:**
1. **Reuse:** Copy the global CSS (Tailwind) and static assets (fonts/icons) from the Stitch export into `Aura.UI/wwwroot`.
2. **Adapt:** Break down the Stitch HTML layout into atomic Blazor components (e.g., `MainLayout.razor`, `Sidebar.razor`, `Header.razor`).
3. **Rewrite:** Remove static dummy data from Stitch and replace it with Blazor bindings (`@Model.Property`) backed by DTOs fetched from `Aura.Api`. Rewrite any vanilla JS interactions (e.g., dropdown toggles) as Blazor C# event handlers (`@onclick`).

**First Thin Vertical Slice (W1-H6-T1):**
Create the `Aura.UI` Blazor Server project. Implement `MainLayout.razor` mapping the Stitch base layout structure (sidebar/header). Create an empty `Index.razor` (the dashboard container). Add a basic Playwright smoke test verifying the app loads and displays the layout.

### Risks
- Injecting `Application` or `Infrastructure` services directly into `Aura.UI` instead of consuming `Aura.Api` endpoints.
- Leaving unnecessary JavaScript from Stitch that conflicts with Blazor Server's DOM diffing.

### Ready for Proposal
Yes. The orchestrator can proceed to the `sdd-propose` phase focusing on setting up `Aura.UI` and the base `MainLayout` using the Stitch assets.