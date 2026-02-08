# Arc Quests — Architecture Proposal

**Version:** 1.1  
**Date:** February 6, 2026  
**Status:** Approved (pending final review of v1.1 changes)

---

## Revision History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-02-06 | Initial proposal |
| 1.1 | 2026-02-06 | Applied 6 required changes from review: rowversion concurrency, /api prefix, UpdatedAt on QuestUpdate append, Ctrl+Q session guardrails, TipTap multi-instance verification, Arc Overview editing conventions |

---

## 1. Domain Model

### Entity Relationship Diagram

```
World ──1:N──> Campaign ──1:N──> Arc ──1:N──> Quest ──1:N──> QuestUpdate
                                  │                              │
                                  └──1:N──> Article (Session) <──┘ (optional FK)
```

### Quest Entity

```
Quest
├── Id              : Guid (PK)
├── ArcId           : Guid (FK → Arc, required)
├── Title           : string (max 300, required)
├── Description     : string? (HTML from TipTap, nullable)
├── Status          : QuestStatus enum (Active=0, Completed=1, Failed=2, Abandoned=3)
├── IsGmOnly        : bool (default false)
├── SortOrder       : int (default 0)
├── CreatedBy       : Guid (FK → User)
├── CreatedAt       : DateTime
├── UpdatedAt       : DateTime (updated on quest edit AND on QuestUpdate append)
├── RowVersion      : byte[] (SQL rowversion, EF concurrency token)
└── Nav: Arc, Creator, Updates (ICollection<QuestUpdate>)
```

**RowVersion** is a SQL Server `rowversion` column mapped as `[Timestamp]` in EF Core. It auto-increments on any row update and is used for optimistic concurrency detection on PUT requests. The client receives `RowVersion` as a base64 string in `QuestDto`, sends it back on PUT, and gets `409 Conflict` with the current server state if it's stale.

**UpdatedAt** is a separate application-managed column that tracks the timestamp of the most recent meaningful change — either a direct quest edit or a new QuestUpdate being appended. This enables "recent activity" sorting on the Arc Overview without querying into QuestUpdates.

### QuestUpdate Entity

```
QuestUpdate
├── Id              : Guid (PK)
├── QuestId         : Guid (FK → Quest, required)
├── SessionId       : Guid? (FK → Article where Type=Session, nullable)
├── Body            : string (HTML from TipTap, required — non-empty)
├── CreatedBy       : Guid (FK → User)
├── CreatedAt       : DateTime
└── Nav: Quest, Session (Article), Creator (User)
```

QuestUpdates are append-only. No concurrency control needed. Deletes are hard deletes (GM can delete any, Players can delete own only).

### QuestStatus Enum

```csharp
public enum QuestStatus
{
    Active = 0,
    Completed = 1,
    Failed = 2,
    Abandoned = 3
}
```

### Why Quests Are Not Articles

Articles are hierarchical, wiki-linked, drag-and-drop content nodes with slugs, breadcrumbs, and parent chains. Quests have a fundamentally different lifecycle (status machine), a flat update log rather than nested children, and a scoping model tied to Arcs rather than the freeform tree. Shoehorning quests into ArticleType would pollute the tree view, break the Article contract, and create filtering complexity everywhere articles are queried.

### Why Include IsGmOnly

DMs routinely prep hidden quests (faction objectives, secret betrayals) that shouldn't be visible to players until revealed. A single boolean is cheap, avoids a full visibility enum, and maps directly to the existing pattern of filtering by WorldRole. The alternative — tracking this outside the app — defeats the purpose of the tool.

### Indexes

- `IX_Quest_ArcId` — all queries start from Arc context
- `IX_Quest_ArcId_Status` — filtered listing (active quests)
- `IX_Quest_ArcId_UpdatedAt` — recent activity sorting on Arc Overview
- `IX_QuestUpdate_QuestId_CreatedAt` — timeline ordering
- `IX_QuestUpdate_SessionId` — "quests referenced in this session" lookups

---

## 2. Security & Authorization

| Action | GM | Player | Observer |
|---|---|---|---|
| View quests & updates | ✅ (all) | ✅ (non-GmOnly) | ✅ (non-GmOnly) |
| Create quest | ✅ | ❌ | ❌ |
| Edit quest (title/status/desc) | ✅ | ❌ | ❌ |
| Delete quest | ✅ | ❌ | ❌ |
| Add QuestUpdate | ✅ | ✅ | ❌ |
| Delete own QuestUpdate | ✅ (any) | ✅ (own only) | ❌ |

**Implementation:** All Quest endpoints resolve the Arc → Campaign → World chain, then check `WorldMembers` for the user's role. This matches the existing `GetAccessibleArticles` pattern where membership is verified via a WorldMembers join. GmOnly quests are filtered out of responses for non-GM roles at the service layer.

**Observer role:** Read-only for everything, including QuestUpdates. Observers cannot create QuestUpdates. This is consistent with the existing Observer contract across the app.

---

## 3. API Design

### Endpoints

All routes are prefixed with `/api` per existing Chronicis conventions.

| Verb | Route | Purpose | Auth |
|---|---|---|---|
| `GET` | `/api/arcs/{arcId}/quests` | List quests for arc | Member |
| `POST` | `/api/arcs/{arcId}/quests` | Create quest | GM |
| `GET` | `/api/quests/{questId}` | Get quest with recent updates | Member |
| `PUT` | `/api/quests/{questId}` | Update quest (title/status/desc) | GM |
| `DELETE` | `/api/quests/{questId}` | Delete quest + cascade updates | GM |
| `GET` | `/api/quests/{questId}/updates?skip=0&take=20` | Paginated update timeline | Member |
| `POST` | `/api/quests/{questId}/updates` | Add update | GM or Player |
| `DELETE` | `/api/quests/{questId}/updates/{updateId}` | Delete update | GM or own |

### Request/Response DTOs

```
QuestDto               { Id, ArcId, Title, Description, Status, IsGmOnly, SortOrder,
                         CreatedBy, CreatedByName, CreatedAt, UpdatedAt,
                         RowVersion (base64 string), UpdateCount }

QuestCreateDto         { Title, Description?, Status?, IsGmOnly?, SortOrder? }

QuestEditDto           { Title?, Description?, Status?, IsGmOnly?, SortOrder?,
                         RowVersion (base64 string, required) }

QuestUpdateEntryDto    { Id, QuestId, Body, SessionId, SessionTitle?,
                         CreatedBy, CreatedByName, CreatedByAvatarUrl?, CreatedAt }

QuestUpdateCreateDto   { Body, SessionId? }

PagedResult<T>         { Items: T[], TotalCount, Skip, Take }
```

### Concurrency

`Quest.RowVersion` is a SQL Server `rowversion` / EF Core `[Timestamp]` column. On PUT `/api/quests/{id}`, the client must include `RowVersion` (base64). The service attaches the original RowVersion to the tracked entity before calling `SaveChangesAsync`. If EF throws `DbUpdateConcurrencyException`, the controller returns `409 Conflict` with the current `QuestDto` so the client can merge or retry.

QuestUpdates are append-only and do not require concurrency control.

### Quest.UpdatedAt Semantics

`Quest.UpdatedAt` is set to `DateTime.UtcNow`:
- When the quest itself is edited (PUT)
- When a new QuestUpdate is appended (POST to `/api/quests/{id}/updates`)

This is done in the service layer, not via database trigger, to keep the logic explicit and testable.

### SessionId Validation on QuestUpdate Creation

When `QuestUpdateCreateDto.SessionId` is non-null, the service verifies:
1. The Article exists
2. The Article's `Type` is `ArticleType.Session`
3. The Article's `ArcId` matches the Quest's `ArcId`

If any check fails, return `400 Bad Request` with a descriptive error. This prevents invalid or cross-arc session associations.

### Error Cases

- `404` — Quest/Arc not found or user not a member of the world
- `403` — Insufficient role (Player trying to create quest)
- `409` — RowVersion mismatch on quest edit (response body contains current QuestDto)
- `400` — Empty title, empty update body, invalid SessionId, invalid status value

---

## 4. Client Architecture

### New Services

| Service | Registration | Purpose |
|---|---|---|
| `IQuestApiService` / `QuestApiService` | Scoped | HTTP calls to quest endpoints |
| `IQuestDrawerService` / `QuestDrawerService` | Scoped | Ctrl+Q toggle event bus (mirrors MetadataDrawerService) |

### State Management

The Ctrl+Q drawer needs to know: (a) which Arc the user is in, and (b) whether the current page is a Session/SessionNote.

**Arc context resolution:** When a user is viewing an article, `ArticleDto.ArcId` is already populated. The drawer reads the currently loaded article from `TreeStateService.SelectedArticleId`, fetches its details from the article cache. If `ArcId` is null (e.g., viewing a Wiki article), the drawer shows "No active arc — navigate to a session to use quest tracking."

**Session detection:** Check `ArticleDto.Type == Session || SessionNote`. When Type is SessionNote, walk up via `ParentId` to find the Session ancestor's `Id`. Cache this resolved SessionId for the duration of the drawer session.

### UI Components

**ArcDetail page additions (quest creation/editing home):**

- `ArcQuestList` — list of quests with status chips and sort controls, ordered by SortOrder then UpdatedAt desc
- `ArcQuestEditor` — inline editing following existing Chronicis conventions:
  - Title saves on blur / Enter (same as article title)
  - Description auto-saves with 0.5s debounce via TipTap onChange (same as article body)
  - Status changes apply immediately via PUT
  - IsGmOnly toggle applies immediately via PUT
  - SortOrder changes apply immediately via PUT
  - All immediate saves include RowVersion; on 409, show snackbar "Quest was modified by another user" and reload
- `ArcQuestTimeline` — expandable timeline per quest showing updates with author avatars, timestamps, and session links

**Ctrl+Q Drawer (quick reference and update entry):**

- `QuestDrawer` — overlay drawer (right side, same pattern as metadata drawer)
  - Quest selector dropdown (filtered to current Arc, active quests first)
  - Read-only quest summary (title, status chip, description preview)
  - TipTap editor for new QuestUpdate body
  - "Associate with this session" checkbox:
    - **Checked + enabled** when opened from a Session or SessionNote page (resolved SessionId available)
    - **Unchecked + disabled** when opened from any other page, with label "No session detected"
    - User can uncheck when on a Session page if the update isn't session-specific
    - When disabled, QuestUpdateCreateDto.SessionId is always null
  - Submit button → POST to `/api/quests/{id}/updates`
  - Recent updates list (last 5) below the editor for quick context

**Focus/Escape behavior:**
- Ctrl+Q toggles the drawer open/closed
- On open: focus moves to quest selector (or TipTap editor if quest already selected)
- Escape closes the drawer and returns focus to the main content area
- Opening quest drawer closes metadata drawer; opening metadata drawer closes quest drawer

### Keyboard Shortcut Wiring

Add `Ctrl+Q` handler to `keyboardShortcuts.js` following the exact Ctrl+M pattern:

```
// In handleKeyDown:
if (e.ctrlKey && e.key === 'q') {
    e.preventDefault();
    dotNetHelper.invokeMethodAsync('OnCtrlQ');
}
```

`AuthenticatedLayout` receives `OnCtrlQ` and calls `QuestDrawerService.Toggle()`. The `QuestDrawer` component subscribes to the toggle event.

---

## 5. TipTap Integration Plan

### Multi-Instance Verification (Required Before Implementation)

Before writing any component code, explicitly verify:

1. **Concurrent instances:** Create a test page with two `<div>` containers, call `initializeTipTapEditor` on each with different `editorId` values. Confirm both editors render and accept input independently.
2. **Wiki link autocomplete scoping:** Type `[[` in each editor and confirm the autocomplete popup appears anchored to the correct editor, not the other one. The existing autocomplete uses `position: fixed` with viewport-relative coordinates from the caret position, so this should be instance-safe, but must be verified.
3. **External link chip behavior:** Insert an external link chip in each editor and confirm click-to-preview works independently.

**If any of these fail:** Fall back to lazy initialization — create the Ctrl+Q TipTap instance only when the drawer opens, dispose it on close. This avoids two simultaneous instances at the cost of a brief initialization delay (~100ms) on drawer open.

### Reuse Strategy

Both the Ctrl+Q editor and the ArcQuestEditor use the same `initializeTipTapEditor` JS function with unique `editorId` values. The existing extensions array (StarterKit, wiki links, external link chips) is built identically — no "lite" variant. Rationale: quest updates frequently reference NPCs, locations, and SRD content. A stripped-down editor would force users to switch contexts for linking, which breaks flow during a session.

### Implementation Pattern

Each TipTap instance gets a unique container ID (e.g., `quest-drawer-editor`, `quest-desc-editor-{questId}`). The Blazor component follows the existing pattern: render `<div id="{editorId}">`, call `initializeTipTapEditor` in `OnAfterRenderAsync`, dispose via `destroyTipTapEditor` in `Dispose`. The `dotNetHelper` callback for content changes routes to the component's save handler rather than the article auto-save pipeline.

### Storage Format

HTML, matching the existing `Article.Body` pattern. Wiki link tokens (`[[guid|text]]`) and external link tokens (`[[source|id|title]]`) are stored inline in the HTML, resolved on render — identical to articles.

---

## 6. Migration & Rollout

### EF Migration

Single migration: `AddQuestEntities`

- Creates `Quests` table with:
  - FK to `Arcs` (required, cascade delete)
  - FK to `Users` (CreatedBy, restrict delete)
  - `RowVersion` column (`rowversion` type, mapped as `[Timestamp]`)
  - Indexes per §1
- Creates `QuestUpdates` table with:
  - FK to `Quests` (required, cascade delete)
  - FK to `Articles` (SessionId, nullable, set null on delete)
  - FK to `Users` (CreatedBy, restrict delete)
  - Indexes per §1
- No backfill needed — net-new feature

### Delivery Order (4 slices)

| Slice | Scope | Dependencies |
|---|---|---|
| 1 | **Backend** — Entities, DbContext config, migration, QuestService, QuestsController, QuestUpdatesController, DTOs, concurrency handling | None |
| 2 | **Arc Overview UI** — QuestApiService, ArcQuestList, ArcQuestEditor (inline edit with debounce), ArcQuestTimeline | Slice 1 |
| 3 | **Ctrl+Q Drawer** — TipTap multi-instance verification, QuestDrawerService, QuestDrawer component, keyboard shortcut wiring, session resolution logic | Slices 1+2 |
| 4 | **Polish** — Loading states, empty states, error handling, 409 conflict UX, quest count badges in tree nav, optimistic UI | Slices 1-3 |

---

## 7. Open Questions / Risks

### Codebase verification needed (before Slice 3)

- **TipTap multi-instance safety** — detailed verification plan in §5. This is the highest-risk item for the Ctrl+Q drawer.
- **ArticleMetadataDrawer implementation pattern** — confirm whether it uses MudDrawer or custom positioned div so the quest drawer matches exactly.

### Performance considerations

- Quest list per Arc is small (typically 5-20). No pagination needed for the list itself, only for updates.
- Ctrl+Q drawer loads quests on open, not on every page navigation. No background polling.
- Session page load does NOT auto-fetch quests. Quests load only when the drawer opens or when visiting Arc detail.
- `UpdatedAt` index enables efficient "recently active" sorting without joining to QuestUpdates.

### Deferred to future slices

- **Backlinks from QuestUpdates:** If a QuestUpdate body contains `[[Waterdeep]]`, Waterdeep's backlinks panel does not show it. This can be added later by extending the ArticleLink pipeline to scan QuestUpdate bodies.
- **Quest references in search results:** Global search does not include Quest titles or QuestUpdate bodies. Can be added as a search provider if there's demand.
- **Public world quest visibility:** Quests are not exposed via the public world viewer. Can be added if public campaigns become a use case.

---

## Decision Checklist (v1.1 — Updated)

All items approved:

1. ✅ Quests belong to Arcs (not Campaigns, not Worlds)
2. ✅ IsGmOnly boolean for hidden quests
3. ✅ GM-only quest creation/editing; Players can add QuestUpdates; Observers read-only
4. ✅ SQL rowversion / EF `[Timestamp]` for optimistic concurrency on Quest edits
5. ✅ HTML storage for QuestUpdate.Body and Quest.Description
6. ✅ Full TipTap (same extensions as Article editor) in all quest editors
7. ✅ Mutually exclusive drawers (quest drawer closes metadata drawer and vice versa)
8. ✅ No backlinks from QuestUpdates in Slice 1
9. ✅ 4-slice delivery order (backend → Arc UI → Ctrl+Q → polish)
10. ✅ SessionId on QuestUpdate references Article.Id (validated as Session type, same Arc)
11. ✅ Quest.UpdatedAt updated on both quest edits and QuestUpdate appends
12. ✅ Ctrl+Q session checkbox disabled with "No session detected" when outside Session/SessionNote context
13. ✅ TipTap multi-instance verification required before Slice 3 implementation
14. ✅ Arc Overview editing follows existing conventions (blur/Enter for title, debounce for description, immediate for status/toggles)
15. ✅ All API routes prefixed with /api

---

## Safety and Invariants

1. No opportunistic refactors
  - Do not rename, move, or “clean up” unrelated files.
  - Do not change existing editor/linking behavior outside Quest components.
  - Do not modify unrelated API routes, DTOs, or auth logic.
2. No new infrastructure or libraries
  - Do not add new packages, databases, caches, message queues, or background jobs.
  - Use existing Chronicis stack and patterns only (ASP.NET Core Web API, EF Core, Blazor WASM, MudBlazor, TipTap interop).
3. Stick to the bounded scope
  - Quests are not Articles and must not appear in the tree.
  - Ctrl+Q drawer cannot create quests or edit quest titles.
  - Arc Overview is the only place where quests are created/edited.
4. Preserve existing permission model patterns
  - Authorization must use World membership and roles (GM/Player/Observer) consistently with existing services.
  - Filtering for GM-only quests happens at the service layer, not in the UI.
5. TipTap safety rules
  - TipTap instances must be isolated per editor instance.
  - If multi-instance isolation is uncertain, initialize TipTap only on drawer open and dispose on close.
  - Quest content storage is HTML, consistent with Article.Body.
6. Data safety
  - All new tables must have appropriate foreign keys and indexes as specified.
  - Use concurrency tokens for quest edits (RowVersion).
  - QuestUpdates are append-only; deletion rules must be explicit and enforced (GM any, player own).
7. Definition of Done per slice
  - Each slice must compile, pass tests, and keep existing tests passing.
  - UI slices must not require backend changes outside documented endpoints.
  - No partial wiring that breaks keyboard shortcuts or editor initialization.
8. Logging and performance
  - No per-keystroke server calls beyond existing debounce patterns.
  - Drawer loads quests on open; no background polling.
9. If something is ambiguous, prefer the smallest change that matches existing Chronicis behavior. 
  - Ask for clarification only when ambiguity would cause irreversible schema/API decisions.
10. When starting a slice, build a plan that can be executed in phases of 5-8 files and pause between each phase until I instruct you to continue.
11. When you finish a slice, provide a summary of all of your implementation activity and decisions. 

---

MOST IMPORTANT: 

1. If you believe a change is needed outside the definition of a slice, stop and ask.
2. You have access to Desktop Commander. You may perform all filesystem CRUD operations, build dotnet, generate entity framework migrations, and pull data from Azure, but you may NOT apply any migrations or apply changes to Azure yourself - always provide powershell scripts for me to apply those changes. 
3. All build errors and warnings must be fixed before a phase is considered to be complete.

Before proceeding, please indicate that you understand these strictures.

---