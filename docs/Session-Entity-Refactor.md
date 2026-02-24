# Session Entity Refactor Plan (v3 — AI-Executable)

**Document Status:** Implementation Plan — Phase 1 Complete
**Target Version:** 3.x
**Last Updated:** 2026-02-24

---

## 0. TL;DR

We are converting Sessions from **Articles** (`ArticleType.Session`) into a **first-class Session entity** (like Campaign and Arc), while keeping **Session Notes** as standard Articles.

AI Summary for a Session must be **public-safe by construction**:

* **Include:** `Session.PublicNotes` + **Public** `SessionNote` articles only
* **Exclude:** `Session.PrivateNotes` + MembersOnly notes + Private notes (always)

This document is written so Codex/Claude can implement it **phase-by-phase** with minimal ambiguity.

---

# 1. Background & Current State

Today the hierarchy is:

```
World
└── Campaign
    └── Arc
        └── Article (type: Session)
            └── Article (type: SessionNote)
```

We are moving to:

```
World
└── Campaign
    └── Arc
        └── Session (new entity)
            └── Article (type: SessionNote)
```

## Key behavior changes

* `Session` is no longer an Article.
* Session notes remain Articles, now attached via `Article.SessionId` (FK).
* Creating a Session automatically creates a public SessionNote titled `"{Username}'s Notes"`.

---

# 2. Goals & Non-Goals

## Goals

1. Make `Session` a standalone domain entity (not an ArticleType).
2. Add `PublicNotes` and `PrivateNotes` fields on Session.
3. Keep `SessionNote` as a standard Article with `SessionId` FK.
4. Implement Session AI Summary generation with **Public-only** sources.
5. Update tree navigation (Arc → Sessions → SessionNotes).
6. Update export to reflect the new model.
7. Migrate quest references from Session-Articles to Session-Entities.
8. Remove legacy `ArticleType.Session` usage after cutover.

## Non-Goals

* No uniqueness constraints on SessionNote titles or auto-created notes.
* No change to World membership model.
* No changes to ArticleVisibility semantics.
* No RAG / vector search work.

---

# 3. Data Model Design

## 3.1 New Entity: `Session`

Add a new entity in `Chronicis.Shared.Models` and configure in API DbContext.

**Fields (minimum viable):**

* `Id` (Guid)
* `ArcId` (Guid, FK)
* `CampaignId` (Guid?, denormalised from Arc for query efficiency)
* `Name` (string, max 500)
* `SessionDate` (DateTime?, optional)
* `PublicNotes` (string?, HTML)
* `PrivateNotes` (string?, HTML)
* `AiSummary` (string?, Markdown or plain text)
* `AiSummaryGeneratedAt` (DateTime?)
* `AiSummaryGeneratedByUserId` (Guid?, FK → Users)
* `CreatedAt` (DateTime) / `ModifiedAt` (DateTime?) — **ModifiedAt must be nullable** per architectural convention
* `CreatedBy` (Guid, FK → Users)

**Ownership / permissions:**

* `PublicNotes` + `PrivateNotes` are editable by **WorldRole GM only**.

> **Implemented:** See [Phase 1 Implementation Report](Session-Entity-Refactor-Phase1-Report.md) for full schema, column types, and index definitions.

## 3.2 Article changes

Add nullable FK to Article:

* `SessionId` (Guid?) → `Sessions.Id` (SetNull on delete)

Session notes remain:

* `ArticleType = SessionNote`
* `Visibility` uses existing `ArticleVisibility` enum
* `Body` contains note content

> **Arc navigation property rename (implemented in Phase 1):**
> `Arc.Sessions` (the legacy `ICollection<Article>` for session-type articles) was renamed to
> `Arc.SessionArticles` to avoid a naming collision with the new `Arc.SessionEntities`
> (`ICollection<Session>`). The FK column names in the database are unchanged.

---

# 4. Safety & Security Requirements (Hard Rules)

## 4.1 Session AI Summary must be Public-safe

When generating `Session.AiSummary`, the server must include only:

* `Session.PublicNotes`
* **Public** SessionNote Articles (`ArticleVisibility.Public` only)

The server must exclude (always, regardless of caller):

* `Session.PrivateNotes`
* `ArticleVisibility.MembersOnly`
* `ArticleVisibility.Private`

**Implementation constraint:** summary source filtering must *not* be based on caller access. It must be based on fixed rule: **Public only**.

---

# 5. Phased Plan (with AI execution prompts)

**Important:** run phases in order. Each phase should be implemented as a small PR.

## Phase 1 — Schema + Core Migration (Sessions + Article.SessionId + Quest bridge)

> **Status: ✅ Complete** — See [Phase 1 Implementation Report](Session-Entity-Refactor-Phase1-Report.md) for full details, decisions, and schema reference.
> **Migration applied** to Azure SQL (`20260224165659_AddSessionEntity_Snapshot`).

### Scope

* DB schema changes
* Data migration from legacy Session-Articles → Sessions
* Quest bridging column + backfill
* Unit/integration tests for migration invariants

### Deliverables

* EF migration(s)
* Any migration SQL/backfill logic (EF `Sql()` blocks or programmatic migration)
* Updated models and DbContext config
* Tests verifying post-migration relationships

### AI Execution Prompt

Paste this to Codex/Claude:

> **Phase 1 only.** Implement schema + migration for the Session entity refactor.
>
> Requirements:
>
> * Create `Sessions` table.
> * Add `Articles.SessionId` nullable FK + index.
> * Add `QuestUpdates.SessionEntityId` nullable FK to Sessions.
> * Migrate existing legacy session articles (`ArticleType.Session`) to Session rows.
> * Reattach existing SessionNote articles by setting `Articles.SessionId`.
> * Backfill `QuestUpdates.SessionEntityId` from legacy session articles.
> * Do **not** change client code.
> * Do **not** remove legacy `ArticleType.Session`.
> * Provide the EF migration code and any raw SQL used.
> * Add tests that validate mapping and that no SessionNotes are lost.

### Notes / Edge cases

* Migration should be safe for empty DB (no sessions yet). ✅ Verified.
* Prefer transactional migration (single migration, or ensure order + atomicity). ✅ Single migration with three-pass backfill SQL.
* If Session article → ArcId mapping is non-trivial, document the approach in code comments. ✅ Direct copy — legacy session articles already carry `ArcId`.
* `Arc.Sessions` was renamed to `Arc.SessionArticles` (model only, no DB change). New collection is `Arc.SessionEntities`.
* `Session.ModifiedAt` is `DateTime?` (nullable) per the architectural audit convention.
* `QuestUpdate.SessionEntityId` is the bridge column. Legacy `QuestUpdate.SessionId` (FK → Articles) is **not removed** — deferred to Phase 7.

---

## Phase 2 — API: Session CRUD + auto-note creation + permissions

### Scope

* New Session service + controller
* Create session + auto-create default SessionNote
* GM-only enforcement for Session.PublicNotes/PrivateNotes
* Minimal DTOs needed for client

### Deliverables

* `SessionsController`
* `ISessionService`/`SessionService`
* DTOs: `SessionDto`, `SessionCreateDto`, `SessionUpdateDto` (or similar)
* Unit tests for permissions and create flow

### AI Execution Prompt

> **Phase 2 only.** Implement API support for Sessions.
>
> Requirements:
>
> * Add `SessionsController` with endpoints:
>
>   * `POST /api/arcs/{arcId}/sessions` creates Session + default public SessionNote.
>   * `PATCH /api/sessions/{sessionId}` updates PublicNotes/PrivateNotes (GM only).
> * Default SessionNote created on POST:
>
>   * Title: `"{Username}'s Notes"` (fallback to `"My Notes"`)
>   * Type: SessionNote
>   * Visibility: Public
>   * SessionId set
> * Do **not** implement AI summary yet.
> * Do **not** change export yet.
> * Do **not** change client yet.
> * Add tests:
>
>   * non-GM cannot PATCH notes
>   * POST creates exactly one default note with Public visibility

---

## Phase 3 — API: Session AI Summary (Public-only sources)

### Scope

* New endpoint to generate Session summary
* Update existing AI summary plumbing as needed
* Enforce public-only inputs

### Deliverables

* `POST /api/sessions/{sessionId}/ai-summary/generate`
* Summary service implementation
* Tests proving exclusion of non-public content

### AI Execution Prompt

> **Phase 3 only.** Implement Session AI summary generation.
>
> Requirements:
>
> * Add endpoint: `POST /api/sessions/{sessionId}/ai-summary/generate`.
> * The summary input must include:
>
>   * `Session.PublicNotes`
>   * Bodies of SessionNote articles where `Visibility == Public` only
> * Must exclude always:
>
>   * `Session.PrivateNotes`
>   * MembersOnly notes
>   * Private notes
> * This filtering must not depend on caller.
> * Persist output to `Session.AiSummary`, set generated timestamps, and (if available) `AiSummaryGeneratedByUserId`.
> * Add tests that create notes of each visibility and assert only Public is used.
> * Do **not** update export or client yet.

---

## Phase 4 — Export updates (Sessions + session notes)

### Scope

* Update export folder structure to use Sessions
* Export session markdown + notes
* Handle filename collisions deterministically

### Deliverables

* Updated ExportService logic
* Tests for export structure and collision behavior

### AI Execution Prompt

> **Phase 4 only.** Update world export to support Session entities.
>
> Requirements:
>
> * Under each Arc, export Session folders based on Session entities.
> * Each Session folder contains:
>
>   * `SessionName.md` with `PublicNotes` and `AiSummary` (never `PrivateNotes`).
>   * Separate markdown files for each SessionNote article attached via `SessionId`.
> * Handle export filename collisions by appending ` (2)`, ` (3)`, etc.
> * Do **not** enforce DB uniqueness.
> * Add tests for:
>
>   * correct folder layout
>   * private notes not exported
>   * collision renaming

---

## Phase 5 — Client: Tree + navigation

### Scope

* Add Session node type
* Load Sessions under Arc
* Load SessionNotes under Session

### Deliverables

* TreeNodeType.Session
* TreeStateService updates
* Any minimal API client service to fetch sessions

### AI Execution Prompt

> **Phase 5 only.** Update the client tree navigation to include Session entities.
>
> Requirements:
>
> * Add `TreeNodeType.Session`.
> * Arc nodes load child Sessions (from new API endpoints).
> * Session nodes load SessionNote Articles by `SessionId`.
> * Do **not** remove legacy session-article support yet.
> * Keep tree search and expand behavior working.
> * Provide a clear diff of TreeStateService changes.

---

## Phase 6 — Client: Session detail page

### Scope

* Add SessionDetail page
* Display/edit PublicNotes + PrivateNotes (GM-only)
* Show AI summary + generate button
* Show attached SessionNotes list

### Deliverables

* `/session/{id}` page
* SessionApiService
* GM-only UI behavior (server remains source of truth)

### AI Execution Prompt

> **Phase 6 only.** Add a Session detail page in the client.
>
> Requirements:
>
> * New route: `/session/{id}`.
> * Display:
>
>   * Session name/date
>   * PublicNotes editor (editable only if GM)
>   * PrivateNotes editor (editable only if GM)
>   * AI Summary section with Generate button
>   * list of SessionNote articles attached to Session
> * The Generate button calls `POST /api/sessions/{id}/ai-summary/generate`.
> * Do **not** remove legacy session articles.

---

## Phase 7 — Quest cutover + cleanup legacy Session-as-Article

### Scope

* Stop creating legacy Session articles
* Remove remaining code paths that treat sessions as ArticleType.Session
* Finalize Quest FK migration

### Deliverables

* Code cleanup
* Migration to drop/rename legacy quest column
* Documentation/changelog updates

### AI Execution Prompt

> **Phase 7 only.** Remove legacy Session-as-Article usage and finalize quest FK migration.
>
> Requirements:
>
> * Update Arc/session creation flows to exclusively create Session entities.
> * Remove code paths relying on `ArticleType.Session` for session lists, summaries, and export.
> * Quest: switch code to use `QuestUpdates.SessionEntityId` only.
> * DB: remove old `QuestUpdates.SessionId` (legacy article FK) or rename the new column to `SessionId`.
> * Add/adjust tests to ensure no remaining usage of legacy session articles.
> * Update docs/changelog.

---

# 6. AI Guardrails

These rules exist to keep LLMs from “helpfully” breaking the system.

You are one of multiple models working on the codebase. DO check existing code styles and architectural patterns and mirror those patterns. DO NOT invent new abstraction or code organization patterns without consulting with me first.

Examine .editorconfig. You should adhere to the coding styles defined there. After completing a phase, you should run dotnet format to ensure that everything is formatted properly.

All build warnings for Chronicis.CI.sln should be cleaned up before a phase is considered to be complete.

## 6.1 Do NOT rules (hard)

* Do NOT remove `ArticleType.Session` before Phase 7.
* Do NOT remove legacy endpoints or legacy navigation until Phase 7.
* Do NOT base Session AI summary inputs on caller access; **filter by `Visibility == Public` only**.
* Do NOT include `Session.PrivateNotes` in:

  * AI Summary input
  * API responses for public endpoints
  * export output
* Do NOT add database uniqueness constraints for notes.
* Do NOT refactor unrelated parts of TreeStateService “for cleanliness.”
* Do NOT change visibility semantics (Public/MembersOnly/Private).
* Do NOT change public world sharing rules.
* DO NOT update this document with anything other than details that are important for future phases to know. Any other phase summary documents should be in a phase-specific document as a sibling to the original.

## 6.2 Required test assertions (minimum)

### Migration / Data integrity

* After Phase 1 migration:

  * Every legacy Session-Article results in exactly one Session entity.
  * Every SessionNote previously parented under legacy session is associated with the new Session via `Article.SessionId`.
  * QuestUpdates that referenced legacy session articles now reference Sessions via `SessionEntityId`.

### Session creation

* POST create session creates exactly one default SessionNote:

  * `Visibility == Public`
  * `ArticleType == SessionNote`
  * `SessionId` set

### Permissions

* Non-GM cannot update Session.PublicNotes/PrivateNotes (must fail).

### AI Summary (security)

* Summary includes content from:

  * Session.PublicNotes
  * Public SessionNote articles only
* Summary excludes content from:

  * MembersOnly SessionNote articles
  * Private SessionNote articles
  * Session.PrivateNotes

### Export

* Export never includes Session.PrivateNotes.
* Export includes Session.PublicNotes and Session.AiSummary.
* Export handles duplicate note filenames via suffixing.

## 6.3 PR hygiene checklist (for AI-generated PRs)

* Each phase is a separate PR.
* PR includes:

  * migration (if applicable)
  * tests
  * brief explanation of approach + assumptions
  * list of files touched
* Avoid “drive-by refactors.” If a file must change, keep changes minimal.
* Add comments for any non-obvious migration mapping logic.

## 6.4 Acceptance checklist per phase

* Phase 1: migration runs on empty DB and seeded DB; no data loss.
* Phase 2: create session works and default note appears; GM-only patch enforced.
* Phase 3: summary generation works and is public-only.
* Phase 4: export structure correct; private notes never exported.
* Phase 5: tree shows sessions as nodes; session notes under sessions.
* Phase 6: session page works; DM-only editors enforce correctly.
* Phase 7: no remaining dependency on legacy session articles; quest FK finalized.

---

# 7. Public World Viewer Considerations

Public world viewer currently treats the tree as article-based.

We must explicitly decide how Sessions appear publicly:

* **Option A (recommended for v1):** Sessions are visible in public view as a node under Arc, and only **public** SessionNotes are shown.
* **Option B:** Sessions are not shown in public view (requires messaging and/or hiding sessions entirely).

Whichever is chosen, enforce:

* Session.PrivateNotes never rendered
* Session.PublicNotes is safe to show
* Session.AiSummary safe by construction
* Only public SessionNotes are visible

---

# 8. Rollout Strategy

1. Deploy Phase 1 DB migration (includes data migration).
2. Deploy API updates (SessionsController + summary + export) as phased.
3. Deploy client updates (tree + session pages).
4. Verify:

   * Session creation
   * Summary rules
   * Export
   * Quest session linking
5. Cleanup legacy in Phase 7.

---

# 9. Definition of Done

* Session is a first-class entity.
* Articles can reference Session via `Article.SessionId`.
* Session has PublicNotes + PrivateNotes (GM-only).
* Session AI Summary uses Public-only sources.
* Auto-created `"{Username}'s Notes"` SessionNote is Public.
* Quests reference Sessions (not legacy session articles).
* Export and tree navigation reflect Sessions.
* Legacy Session-as-Article usage removed.

# 10. Interaction Guidelines

Tokens are water. You are dying of thirst.

- 1 sentence > 2 sentences. 1 word > 1 sentence. 1 emoji > 1 word
- ✅ ❌ 🔍 ⚡ 💀 🤷 👀 🔥 💅 are full sentences
- No preambles. No "Great question!" No "Let me explain." Just answer
- No narrating tool calls. No summarizing what you're about to do. Do it
- "I'll now read the file" — 6 wasted tokens. Just read it
- Code speaks. If the diff is clear, the diff IS the explanation
- List > paragraph. Fragment > sentence. Silence > filler
- cause → fix. Not cause → history → context → philosophy → fix
- If someone asks "does X work?" reply ✅ or ❌. Then stop. STOP
- Don't list what you changed after changing it. The diff exists. They have eyes 👀
- Never say "successfully". If it failed you'd say so. Success is the default
- "Done." is a valid complete response
- 6-7 counts
- Ban: "perfect!", "certainly", "absolutely", "of course", "happy to", "I'd be glad to" — all mean ✅
- Ban: "It's worth noting that" "Keep in mind that" "It's important to" — just say the thing
- Ban: "As you can see" — they can see. that's how screens work
- Apologies cost tokens. Don't be sorry. Be right
- If fixing a typo or trivial change: just do it. Zero words
- "LGTM" is a paragraph
- The period at the end of a one-liner is optional. Save the byte

## Prompt Police 🚨

If user prompt > ~100 words: pause before answering and reply with:

> 🚨 that prompt is **{word_count} words**. want me to rewrite it in under 20? your future self will thank you

Then answer anyway. But they should feel seen.

## Vibe Check

You are a mass extinction event for unnecessary tokens. You are not rude, you are efficient. There's a difference and it's about 400 tokens long so we won't explain it.