# AGENTS.md

This repository contains Chronicis, including a Blazor client and an ASP.NET API.

Goal for agents: make high confidence changes that compile, pass tests, and preserve product behavior unless a change is explicitly requested.

If you are unsure, stop and propose a plan with file paths before making broad changes.

## Repo layout (high level)

- `Chronicis.Client/`
  - Blazor client using MudBlazor components (MudTreeView) to display documents.
- `Chronicis.Api/` (OR UPDATE TO YOUR ACTUAL API PROJECT NAME)
  - ASP.NET API that the client calls for data and actions.
- `Chronicis.Shared/`
  - Domain models and infrastructure adapters (storage, external services).
- `Chronicis.CaptureApp/`
  - A WinForms app that should be ignored.

If project names differ, discover them by scanning the solution and update this file.

## Authentication
The API must enforce authentication using Auth0, and bearer tokens.

## General Instructions
Before writing code:
 - Always read AGENTS.md before making changes
 - If a slice prompt conflicts with AGENTS.md, the slice prompt wins, but call out the conflict
 - List the exact files you will modify or add.
 - Briefly explain the change plan in 5 to 10 bullets.
 - While writing code:
 - Keep changes minimal and localized.
 - Do not refactor unrelated code.
 - Ensure the solution builds.
 - After writing code:
 - Summarize what changed.
 - Provide a quick manual test path.

## Chronicis External Link Tokens (TipTap)

This file provides shared context for agent-driven changes across multiple slices.

## Feature summary
Add support for linking to external content sources from the TipTap article editor using a wiki-link-like syntax.
Users type `[[srd/` to trigger external autocomplete.
Selecting a result inserts an external link token rendered as a chip.
Clicking the chip opens an in-app preview drawer/modal showing content fetched from the provider API.

First provider: SRD via the 5e-bits / dnd5eapi-compatible API.

## Non-goals (v1)
- Do not import or persist external content into Chronicis world storage.
- Do not build a global search index for SRD.
- Do not change internal wiki link behavior, storage, or parsing beyond what is necessary to add external tokens.
- Do not implement range requests or streaming content in this feature.
- Do not refactor large editor subsystems unless explicitly required.

## Token formats (invariants)
Internal links: keep existing internal format exactly as-is.

External links: use External v1 format:
`[[source|id|title]]`

Where:
- `source` is a provider key (example: `srd`, future: `kobold`)
- `id` is the provider-specific stable identifier (for SRD, use the `url` field returned by the API, typically a relative path like `/api/2014/spells/acid-arrow`)
- `title` is display text shown in the editor chip and rendered views

Parsing rules:
- External token is detected when the content inside `[[...]]` contains two `|` separators and begins with a non-empty source key.
- Editor trigger for external autocomplete is `[[<sourceKey>/` (slash after key).

Security rule for ids:
- External `id` must be a relative path, not an absolute URL.
- For SRD: enforce that `id` starts with `/api/` to prevent SSRF vectors.

## Server API (normalized contracts)
### Suggestions endpoint
`GET /api/external-links/suggestions?source=<source>&query=<query>`

Response: list of `ExternalLinkSuggestionDto`:
- Source (string)
- Id (string)
- Title (string)
- Subtitle (string, optional)
- Icon (string, optional)
- Href (string, optional) - a clickable URL if available, but UI should prefer in-app preview

### Content endpoint
`GET /api/external-links/content?source=<source>&id=<id>`

Response: `ExternalLinkContentDto`:
- Source (string)
- Id (string)
- Title (string)
- Kind (string) - e.g., spell, monster
- Markdown (string) - normalized display body
- Attribution (string, optional)
- ExternalUrl (string, optional) - for "Open on source site"

## Provider architecture (extensible)
Introduce `IExternalLinkProvider`:
- `string Key { get; }`
- `Task<IReadOnlyList<ExternalLinkSuggestion>> SearchAsync(string query, CancellationToken ct)`
- `Task<ExternalLinkContent> GetContentAsync(string id, CancellationToken ct)`

A provider registry resolves by `Key` and is used by the endpoints.

Implement `SrdExternalLinkProvider`:
- Base URL configured in app settings
- Suggestions: spells and monsters at minimum
- Content: fetch resource JSON and build markdown including `desc` content and basic metadata

## Likely document locations
 - src/Chronicis.Shared/Dtos/ExternalLinks/*
 - src/Chronicis.Api/Services/ExternalLinks/*
 - src/Chronicis.Api/Functions/ExternalLinksFunctions.cs
 - src/Chronicis.Web/Services/ExternalLinks/*
 - src/Chronicis.Client/wwwroot/js/*

## Client/editor behavior
- Typing `[[` triggers existing internal autocomplete.
- Typing `[[<sourceKey>/` routes autocomplete to the external suggestions endpoint.
- Enter inserts the External v1 token: `[[source|id|title]]`.
- Render external tokens as a distinct chip:
  - Include a small source badge like `SRD`
  - Include an external indicator icon (arrow out)
  - Keep styling consistent with internal chips but clearly different
- Clicking external chip opens an in-app preview drawer/modal:
  - Fetch content via content endpoint
  - Render markdown
  - Show loading and error states
  - Cache content in-memory per session

## Implementation constraints
- Keep changes localized; avoid unrelated refactors.
- Follow existing project patterns for Services, Functions, Shared DTOs, and DI registration.
- Preserve internal wiki link behavior.
- Do not introduce new heavy dependencies without justification.
- Avoid em-dashes in text outputs and docs.
- When asked to implement a slice: first list files to change, then implement, then provide a manual test path.
- If a change seems to require refactoring unrelated code, stop and explain why, do not refactor.

## slice definition of done
 - API and Client projects build

## Slice acceptance checks (high-level)
Slice 1: Suggestions endpoint returns SRD results for query "acid".
Slice 2: Content endpoint returns non-empty markdown for a known SRD id.
Slice 4: Editor shows external suggestions on `[[srd/acid` and inserts correct token.
Slice 5: Clicking external chip opens preview and renders markdown.

## Manual smoke test
1) Open article editor.
2) Type `[[srd/acid`.
3) Select a suggestion and press Enter.
4) Confirm a distinct external chip is inserted.
5) Click the chip and confirm preview drawer shows markdown content.

## Decisions made
 - Token format chosen: [[source|id|title]]
 - Trigger chosen: [[sourceKey/
 - Preview format chosen: markdown returned by API
 - External chip differences: badge + external icon + distinct styling
## Slice 0 Findings (Canonical)

### Architecture Map
 - Parsing and sync on server: LinkParser.cs parses [[guid]] or [[guid|display]] and LinkSyncService.cs persists ArticleLink rows from parsed links.
 - Link suggestion endpoint: LinkSuggestionsFunctions.cs serves GET api/worlds/{worldId}/link-suggestions?query=... using LinkSuggestionDto from LinkDtos.cs.
 - Link resolution and backlink APIs: LinkResolutionFunctions.cs for POST api/articles/resolve-links, BacklinkFunctions.cs for backlinks and outgoing links, AutoLinkFunctions.cs for auto-linking.
 - Client autocomplete and selection flow: wikiLinkAutocomplete.js detects [[ and calls OnAutocompleteTriggered; ArticleDetail.razor loads suggestions via ILinkApiService and inserts links via insertWikiLink.
 - TipTap rendering and click handling: wikiLinkExtension.js defines the wikiLink node; tipTapIntegration.js registers it, converts wiki links between markdown and HTML, and handles click and hover for span[data-type="wiki-link"] to call OnWikiLinkClicked.
 - UI for suggestions: WikiLinkAutocomplete.razor renders the dropdown; styles live in chronicis-wiki-links.css.

### Current Internal Link Storage Format
 - Markdown format: [[guid]] or [[guid|display]] as parsed by LinkParser.cs and emitted by htmlToMarkdown in tipTapIntegration.js.
 - HTML format stored in editor content: <span data-type="wiki-link" data-target-id="guid" data-display="display">display</span> as produced by markdownToHTML in tipTapIntegration.js and rendered by wikiLinkExtension.js.
 - Persistence: parsed links are stored in ArticleLink rows in ArticleLink.cs via LinkSyncService.

### Best Integration Point for External Suggestions Triggered by [[srd/
 - Minimal touch point on client: OnAutocompleteTriggered in ArticleDetail.razor already branches on query length and calls LinkApiService. This is the cleanest place to detect a sourceKey/ prefix like srd/ and route to a new external suggestions service without changing the JS keyboard handling.
 - Optional enhancement in JS if needed later: wikiLinkAutocomplete.js already extracts the raw query after [[. It can remain unchanged if the routing happens in C#.

### Proposed Minimal Additions for External Providers
 - Shared DTOs for external results: ExternalLinkSuggestionDto.cs and ExternalLinkContentDto.cs.
 - API provider interfaces and implementation: IExternalLinkProvider.cs, ExternalLinkProviderRegistry.cs, SrdExternalLinkProvider.cs.
 - API endpoints: ExternalLinksFunctions.cs for
 - GET /api/external-links/suggestions?source=<source>&query=<query>
 - GET /api/external-links/content?source=<source>&id=<id>
 - Client service: ExternalLinkApiService.cs to call the new endpoints.
 - Client rendering and click handling: extend tipTapIntegration.js to recognize data-type="external-link" and open a preview; add styling in chronicis-wiki-links.css and a small UI component for the preview drawer.
 - External Link Storage Format Proposal

### Use the External v1 token format: [[source|id|title]].
 - Example: [[srd|/api/2014/spells/acid-arrow|Acid Arrow]].
 - Parsing rule: only treat as external if there are two | separators and the token starts with a non-empty source key.
 - Security rule: id must be a relative path, for SRD enforce /api/ prefix.