# Tutorial Sidebar (Ctrl+T) — Implementation Plan

## Goal

Add a contextual **Tutorial** sidebar (drawer) similar to the existing **Quests drawer**.

* **Ctrl+T** toggles the tutorial drawer open/closed.
* Content is **contextual**: determined by the currently displayed page.
* Content is authored/stored as an **Article** with a new `ArticleType`: **Tutorial**.
* Tutorial content is **globally readable** (no world membership required) and **admin-editable**.
* Add an **Admin UI tab** on the user admin page to list and edit tutorial pages.

Non-goals (for initial version):

* Per-user progress tracking (“completed steps”), checklists, badges.
* A guided “tour” overlay / spotlight system.
* Localization.

---

## UX / Product Behavior

### Drawer behavior

* Drawer lives on the **right side** (same region/feel as other drawers).
* Uses the same visual language as existing drawers (header, close button, scrollable body).
* When opened:

  * If tutorial exists for the current context, render it.
  * If not, render the **default tutorial** (`Page:Default`).

### Auto-open rules

The tutorial drawer should automatically open (once per user / per context rules below) when:

1. **User is brand new**

* Definition: user has `Onboarded == false` (existing flag in the system).
* Behavior: automatically open the drawer on first eligible authenticated page load.
* After the drawer is opened once for onboarding purposes, the normal onboarding flow should eventually set `Onboarded = true` (existing behavior), which prevents future auto-opens under this rule.

2. **User is viewing content for a tutorial world**

* If current world has `IsTutorial == true`, the Tutorial drawer is **forced open** while in that world context.
* Behavior:

  * Drawer auto-opens immediately upon entering the world.
  * Drawer **cannot be closed** while the user is in the tutorial world.
  * Ctrl+T and the ? icon should **not close** the drawer in this context (they may either do nothing, or just focus/bring the drawer to front).
  * Close button is hidden/disabled.

**Note:** Because the drawer is not dismissible in tutorial worlds, do **not** persist dismissal flags for this case.

### Keyboard shortcut

* **Ctrl+T** toggles the drawer.
* Shortcut should work:

  * from anywhere in the app,
  * even while typing in TipTap,
  * but should NOT override browser/system shortcuts if a platform reserves it (handle gracefully).

Special case:

* If the current world has `IsTutorial == true`, Ctrl+T must **not** close the tutorial drawer.

### App drawer entry point

Add a **question mark icon** to the app drawer (or the same area where other global drawers are launched).

* Clicking it opens the Tutorial drawer.
* If already open, clicking toggles/keeps focus consistent with other drawer buttons.

Special case:

* If the current world has `IsTutorial == true`, the ? icon must **not** close the drawer.

### Content mapping rules

Tutorial content is selected using a deterministic key derived from the current route.

#### Mapping inputs

1. **Authenticated page route** (e.g., `/dashboard`, `/settings`, `/world/{id}`, `/article/{path}`)
2. **Article type** when viewing an article (e.g., WikiArticle, Character, Session, etc.)

#### Mapping output

A single Tutorial article.

#### Mapping precedence

* If on an **article detail page**:

  1. match `ArticleType`-specific tutorial for that page (e.g., “ArticleDetail:Session”)
  2. fallback to generic article detail tutorial (e.g., “ArticleDetail:Any”)
* If on a **non-article page**:

  * match by route key (e.g., “Settings”, “Dashboard”, “WorldDetail”, etc.)

---

## Data Model Changes

### Decisions (locked in)

* Tutorials are stored as `ArticleType.Tutorial` in `Articles`.
* Tutorial articles should be **global** and not tied to a real world.

  * **Decision:** store `WorldId = Guid.Empty` for tutorial articles (instead of `NULL`).
  * Rationale: avoids `NULL`-handling edge cases in uniqueness constraints and world-scoped query filters.
  * Rule: treat `Guid.Empty` as the **system/tutorial world** sentinel.

### 1) New enum value: `ArticleType.Tutorial`

* Add `Tutorial` to the existing `ArticleType` enum.

### 2) Mapping table: `TutorialPages`

A dedicated mapping table maps a **page context** to a **Tutorial article**.

**New table: `TutorialPages`**

* `Id` (Guid, PK)
* `PageType` (string, required, indexed)

  * Canonical key used for resolution.
  * Examples:

    * `Page:Dashboard`
    * `Page:Settings`
    * `Page:WorldDetail`
    * `ArticleType:Session`
    * `ArticleType:Character`
    * `ArticleType:Any` (fallback)
    * `Page:Default` (global fallback)
* `PageTypeName` (string, required)

  * Human-friendly label shown in the SysAdmin UI.
  * Examples:

    * `Dashboard`
    * `Settings`
    * `World Detail`
    * `Session Articles`
    * `Default Tutorial`
* `ArticleId` (Guid, required, FK → `Articles.Id`, indexed)
* `CreatedAt` (datetime)
* `ModifiedAt` (datetime)

**Constraints / indexes (decision)**

* Unique index on `PageType` (one mapping per PageType).
* **No uniqueness constraint** on `ArticleId` (a single tutorial article may be reused for multiple PageTypes).

### 3) Permissions (decision)

Tutorial articles are **globally readable** to authenticated users.

Editing is restricted to **SysAdmin only**.

Implementation note: we already have an **admin-check service** used across the app. Extend it (or add a parallel method) to distinguish **SysAdmin vs Admin**, and use that consistently in API + UI gating.

Read rule:

* `ArticleType == Tutorial` ⇒ allow authenticated users to read regardless of world membership.

Write rule:

* `ArticleType == Tutorial` ⇒ require SysAdmin.

Query rule:

* World-scoped article lists/trees/search should **exclude** tutorials by default.
* Additionally, treat `WorldId == Guid.Empty` as “system” and exclude it from world queries.

---

## Backend (API) Changes

### Endpoints

#### 1) Resolve tutorial for current context

`GET /api/tutorials/resolve?pageType={pageType}`

* Returns `TutorialDto` containing:

  * `ArticleId`
  * `Title`
  * `Body`
  * `ModifiedAt`
* Behavior (with default tutorial):

  1. If exact `pageType` match exists, return it.
  2. If no match and `pageType` is `ArticleType:{X}`, attempt fallback `ArticleType:Any`.
  3. If still no match, load the **default tutorial** mapping: `Page:Default`.

> Result: this endpoint always returns *some* tutorial as long as `Page:Default` exists (seed/migration ensures it does).

#### 2) SysAdmin list mappings

`GET /api/sysadmin/tutorials`

* Returns list:

  * `PageType`
  * `ArticleId`
  * `Title`
  * `ModifiedAt`

#### 3) SysAdmin mapping CRUD

* `POST /api/sysadmin/tutorials` (create mapping + create article or attach existing)
* `PUT /api/sysadmin/tutorials/{id}` (change PageType ↔ ArticleId)
* `DELETE /api/sysadmin/tutorials/{id}`

> Note: article content editing remains via the normal article editor endpoints, but must enforce SysAdmin if `ArticleType == Tutorial`.

### AuthZ

* `resolve` endpoint: authenticated users.
* sysadmin endpoints: SysAdmin-only policy/role.
* Article create/update/delete:

  * if `ArticleType == Tutorial` ⇒ SysAdmin-only.

### Access control rules

* Ensure the standard “world membership” filters do not block Tutorial articles.

  * If the code uses a helper like `GetAccessibleArticles(userId)` then extend it to include tutorials.

### Caching

* Tutorials are relatively static.
* Consider short-lived in-memory caching by `PageType` (e.g., 30–120 seconds) to avoid repeat reads when users toggle.

---

## Frontend Changes (Blazor)

### Decisions (locked in)

* Tutorial/Quest/Metadata drawers should be made **consistent**.
* **Decision:** consolidate drawers into a single, consistent drawer host/component with a coordinator.

  * This avoids brittle pairwise “when I open, close you” wiring.

### 1) Unified drawer host + coordinator

Create a `DrawerHost` component responsible for rendering and animating a single drawer region.

Create `IDrawerCoordinator`:

* `DrawerType Current` (None/Tutorial/Quests/Metadata)
* `Open(DrawerType type)`
* `Close()`
* `Toggle(DrawerType type)`
* `bool IsForcedOpen` (set when in tutorial world)
* `event Action? OnChanged`

Rules:

* Opening any drawer closes the others automatically.
* If `IsForcedOpen == true` and `Current == Tutorial`, `Close()` is a no-op.

### 2) Tutorial drawer implementation

Create `TutorialDrawer` content component used by `DrawerHost`.

* Header: “Tutorial”
* Body: loading/error/content
* Close button hidden/disabled when forced open.

### 3) Lazy loading (decision)

Tutorial content should **not** be fetched until needed.

Fetch tutorial content when:

* Tutorial drawer is opened (by Ctrl+T, ? icon, auto-open), OR
* Route changes while Tutorial drawer is open, OR
* Entering tutorial world (forced open implies it is open).

### 4) Tutorial resolution service

Add `ITutorialApiService`:

* `Task<TutorialDto?> ResolveAsync(string pageType)`

### 5) PageType resolver

Implement `TutorialPageTypeResolver`:

* Inputs:

  * current URI
  * (optional) current ArticleType
  * (optional) current WorldId / IsTutorial flag
* Output: canonical `PageType` string

### 6) Page list source for admin dropdown (decision)

We will NOT do runtime reflection.

Instead:

* Codex will use the repository’s `Pages/*.razor` file list during implementation to generate/curate a list of eligible pages.
* Store the list in code (e.g., `TutorialPageTypes.cs`) as:

  * `IReadOnlyList<(string PageType, string DefaultName)>` for `Page:{X}`
  * plus `ArticleType:{X}` entries derived from the enum.

This keeps runtime behavior simple and keeps the dropdown clean.

### 7) Keyboard shortcut

* **Ctrl+T** toggles Tutorial open/closed via `IDrawerCoordinator.Toggle(Tutorial)`.
* Special case:

  * If `IsTutorial == true` for the current world, Ctrl+T must **not** close the Tutorial drawer.

### 8) App drawer entry point

Add a **question mark icon** to the app drawer.

* Click opens Tutorial via `IDrawerCoordinator.Toggle(Tutorial)`.
* Special case: cannot close tutorial while in tutorial world.

### 9) Auto-open rules

1. **New user**

* Definition: user has `Onboarded == false`.
* Behavior: automatically open Tutorial on first eligible authenticated page load.

2. **Tutorial world**

* If current world has `IsTutorial == true`, the Tutorial drawer is **forced open** while in that world context.
* Behavior:

  * Auto-open immediately upon entering the world.
  * Cannot be closed.

---

## Admin UI (User Admin Page) (User Admin Page)

Add a new tab: **Tutorials**.

**Visibility / access:** SysAdmin-only.

### Tab contents

* Table of tutorial mappings:

  * Page Type (key)
  * Page Type Name (label)
  * Title
  * Last Modified
  * Actions: Open/Edit, (optional) Remove Mapping

### Page type options source (chosen)

The **Create Tutorial** dropdown should be populated automatically from `Pages/*.razor`.

Implementation approach (Blazor-friendly):

* At runtime, enumerate routable components in the app assembly using `RouteAttribute` (this is effectively the compiled representation of `@page` directives).
* Filter to components that originate from the `Pages` folder/namespace convention (e.g., namespace contains `.Pages.`) so we don’t include random routable components.
* Convert discovered routes/components into canonical `PageType` values (`Page:{Name}`), where `{Name}` is derived from the component type name (or a friendly label map).

This gives the same result as “scan Pages/*.razor”, without relying on filesystem access at runtime.

### Edit flow (chosen)

* Clicking **Edit** navigates to the tutorial article in the **normal article editor**.
* The article editor must enforce: **only SysAdmin can save Tutorial articles**.

### Create new tutorial

* “Create Tutorial” button (SysAdmin only):

  * choose `PageType` from a dropdown (discovered pages + ArticleTypes)
  * enter/confirm `PageTypeName` (friendly label)
  * creates a new `ArticleType.Tutorial` article
  * creates a `TutorialPages` row mapping `PageType → ArticleId`

Implementation detail: during initial scaffold/seed creation, Codex can prompt you once per discovered page for the preferred `PageTypeName`, then persist it.

Also include a special mapping:

* `Page:Default` → default tutorial article (seeded).

---

## Rendering / Content Format / Content Format

Tutorial content is stored as an **Article body**.

Rendering should match existing article content rendering behavior:

* Same HTML/markdown pipeline used by the normal article viewer.
* If tutorials are stored as TipTap HTML, we should render that safely in the drawer.

---

## Data Migration

* Add enum value + schema changes (TutorialKey or TutorialPages table).
* Seed a small set of tutorial pages for:

  * Dashboard
  * Settings
  * ArticleDetail:Any

---

## Telemetry / Observability

(Optional for v1)

* tutorial drawer opened/closed
* tutorial resolved: PageType + which fallback matched
* sysadmin updates (create/update mapping)

---

## Caching

**Decision:** no caching work for v1.

* This system is not high-usage.
* We rely on **lazy loading** (only fetch when the drawer is opened / forced open).

---

## Test Plan

### Unit tests

* TutorialKeyResolver:

  * routes map to expected keys
  * article type mapping + fallback

### API integration tests

* resolve endpoint returns tutorial when present
* resolve endpoint returns 404/empty when absent
* tutorial access bypasses world membership filters
* admin endpoints require admin

### UI smoke tests

* Ctrl+T toggles drawer
* drawer updates content when navigating while open
* empty state renders when no tutorial found

---

## Rollout Plan

1. Backend: enum + `TutorialPages` table + resolve endpoint + sysadmin mapping endpoints
2. Frontend: unified drawer host/coordinator + tutorial drawer + Ctrl+T + ? icon
3. Auto-open rules: `Onboarded == false` + forced-open tutorial world
4. Admin tab: list + create mapping + edit navigation
5. Seed default tutorial mapping + a couple starter tutorials

---

## Phased Codex Execution Plan (copy/paste prompts)

> You said you’ll run **a new chat per prompt** with Codex. Each prompt below is self-contained and references prior work by name so Codex can stay oriented.

### Global instructions to include in EVERY Codex prompt

Paste this block at the top of each Codex chat:

* You are one of multiple models working on the codebase.
* DO check existing code styles and architectural patterns and mirror those patterns.
* DO NOT invent new abstraction or code organization patterns without consulting with me first.
* Examine `.editorconfig` and adhere to the coding styles defined there.
* After completing the phase, run `dotnet format`.
* All build warnings for `Chronicis.CI.sln` must be cleaned up before the phase is considered complete.

---

### Phase 0 — Recon & inventory

**Goal:** find the existing drawers, keyboard shortcut plumbing, admin-check service, and article authz/query hotspots.

**Codex prompt:**

* Search the repo for:

  * Quest drawer component + service
  * Metadata drawer component + service
  * drawer placement/layout code
  * global keyboard shortcut wiring (Ctrl+S etc.)
  * the admin-check service (how it detects admin/sysadmin)
  * article query helpers that scope by WorldId / build trees / enforce membership
  * how `World.IsTutorial` is represented and how world context is determined in UI
* Produce:

  * file paths + short notes on how each works
  * recommended insertion points for the new unified `DrawerHost` + `IDrawerCoordinator`
  * list of endpoints/services that must treat `ArticleType.Tutorial` and/or `WorldId == Guid.Empty` specially
  * any existing patterns for “system content” and sentinel IDs

**Definition of Done:**

* A short writeup with links/paths for each item above.
* Clear recommendation for where to implement:

  * coordinator
  * tutorial resolve API
  * sysadmin gate enforcement

---

### Phase 1 — Data model + migration

**Goal:** add `ArticleType.Tutorial`, add `TutorialPages` table, and ensure tutorial articles use `WorldId = Guid.Empty`.

**Codex prompt:**

* Implement per the doc “Tutorial Sidebar (Ctrl+T) — Implementation Plan”:

  * Add `Tutorial` to `ArticleType` enum.
  * Add EF model + DbSet + migration for `TutorialPages` with:

    * `PageType` unique
    * `ArticleId` non-unique indexed
    * `PageTypeName`
  * Ensure creation of tutorial articles sets `WorldId = Guid.Empty`.
  * Seed a `Page:Default` mapping + placeholder tutorial article.

**Definition of Done:**

* `dotnet build Chronicis.CI.sln` has **zero warnings**.
* Migration applies cleanly.
* Seeded default tutorial exists in DB after migration.
* `dotnet format` run and no formatting changes pending.

---

### Phase 2 — Backend endpoints + authz

**Goal:** resolve endpoint with fallback + sysadmin CRUD + enforce sysadmin-only edits of tutorial articles.

**Codex prompt:**

* Add API endpoints:

  * `GET /api/tutorials/resolve?pageType=...` with fallback:

    1. exact
    2. `ArticleType:Any` if applicable
    3. `Page:Default`
  * SysAdmin-only CRUD under `/api/sysadmin/tutorials`
* Update article create/update/delete authorization:

  * if `ArticleType == Tutorial` ⇒ require SysAdmin.
  * read access for tutorials allowed to any authenticated user.
* Update world-scoped article queries/trees/search to exclude tutorials and/or exclude `WorldId == Guid.Empty`.
* Extend the existing admin-check service to support SysAdmin in a way consistent with current patterns.

**Definition of Done:**

* `dotnet build Chronicis.CI.sln` has **zero warnings**.
* Manual smoke:

  * authenticated non-sysadmin can resolve and read tutorial content
  * non-sysadmin cannot create/update/delete tutorial articles
  * sysadmin can CRUD mappings
* Unit/integration tests added/updated where the repo already has patterns.
* `dotnet format` run.

---

### Phase 3 — Unified drawer host + coordinator

**Goal:** make Tutorial/Quest/Metadata drawers consistent and mutually exclusive.

**Codex prompt:**

* Introduce `DrawerHost` + `IDrawerCoordinator` as specified.
* Refactor existing Quest + Metadata drawers to render through `DrawerHost` with consistent behavior/placement.
* Add `TutorialDrawer` as another drawer type.
* Ensure “forced open in tutorial world” is enforced at coordinator level.
* Keep visual/UX parity (same animation, width, header behavior).

**Definition of Done:**

* `dotnet build Chronicis.CI.sln` has **zero warnings**.
* Manual smoke:

  * Quests opens/closes correctly
  * Metadata opens/closes correctly
  * opening one closes the others
* Forced-open mode supported in coordinator (even if tutorial world UI is not wired yet).
* `dotnet format` run.

---

### Phase 4 — Tutorial drawer UI + lazy loading

**Goal:** implement the tutorial drawer and only fetch content when opened.

**Codex prompt:**

* Implement `TutorialDrawer` UI:

  * loading, error, content
  * hide/disable close when forced open
* Implement `TutorialPageTypeResolver` + route/article-type mapping.
* Add lazy loading:

  * fetch on open
  * refresh on route change while open
  * forced open implies immediate fetch
* Add Ctrl+T wiring to toggle Tutorial via coordinator.
* Add ? icon in the app drawer to open Tutorial.

**Definition of Done:**

* `dotnet build Chronicis.CI.sln` has **zero warnings**.
* Manual smoke:

  * opening tutorial via ? icon fetches and renders
  * Ctrl+T toggles tutorial
  * navigating while tutorial open refreshes content
  * navigating while closed does not fetch
* `dotnet format` run.

---

### Phase 5 — Auto-open rules

**Goal:** open automatically for new users and force open in tutorial worlds.

**Codex prompt:**

* Implement auto-open:

  * if `Onboarded == false`, open tutorial drawer on first eligible authenticated page load.
  * if `World.IsTutorial == true`, force open and prevent closing while in that world.
* Ensure Ctrl+T and ? icon cannot close it in tutorial world.
* Hide/disable close button in forced mode.

**Definition of Done:**

* `dotnet build Chronicis.CI.sln` has **zero warnings**.
* Manual smoke:

  * set user to `Onboarded=false` → tutorial auto-opens on first eligible page
  * enter `IsTutorial=true` world → tutorial opens and cannot be closed
* `dotnet format` run.

---

### Phase 6 — SysAdmin admin-tab UI

**Goal:** list mappings + create mapping + edit via normal article editor.

**Codex prompt:**

* Add a SysAdmin-only “Tutorials” tab on the user admin page.
* Table shows: PageType, PageTypeName, Title, Last Modified.
* Create flow:

  * dropdown of page types (generated/curated list from `Pages/*.razor` done at implementation time)
  * prompt/field for `PageTypeName`
  * creates tutorial article + mapping
* Edit navigates to normal article editor.
* Ensure editor save is blocked unless SysAdmin for Tutorial articles.

**Definition of Done:**

* `dotnet build Chronicis.CI.sln` has **zero warnings**.
* Manual smoke:

  * sysadmin sees Tutorials tab
  * can create mapping and is navigated to editor
  * non-sysadmin does not see Tutorials tab and cannot edit tutorial article
* `dotnet format` run.

---

### Phase 7 — Seed starter content

**Goal:** add a few placeholder tutorials.

**Codex prompt:**

* Add seed mappings (or a seed script) for:

  * `Page:Dashboard`
  * `Page:Settings`
  * `ArticleType:Any`
* Ensure `Page:Default` exists.
* Ensure seeded tutorial articles have `WorldId = Guid.Empty`.

**Definition of Done:**

* `dotnet build Chronicis.CI.sln` has **zero warnings**.
* Fresh DB has the default + starter mappings after migration/seed.
* `dotnet format` run.
