# Chronicis Feature Inventory

Last reviewed: 2026-03-01

Scope:
- Included: `src/Chronicis.Api`, `src/Chronicis.Client`
- Excluded: `src/Chronicis.CaptureApp`
- Included here: currently implemented product features only
- Excluded here: architecture/runtime details, endpoint references, and future roadmap ideas

## 1) Account, Access, and Onboarding
- Authenticated sign-in experience for protected app areas.
- Anonymous access for public pages and shared-world viewing.
- Current user profile retrieval for in-app personalization.
- First-login user provisioning.
- Onboarding completion tracking.
- Multi-step getting-started experience.
- Tutorial world provisioned for new users.
- Contextual tutorial/help drawer with page-specific guidance.
- Sysadmin-restricted experiences and controls.

## 2) Dashboard and Home Experience
- Personalized dashboard aggregating a user’s worlds and campaigns.
- Claimed character highlights on dashboard.
- Contextual prompt cards to guide next actions.
- World and campaign quick-entry navigation.
- World creation and world-join actions from dashboard flows.
- Legacy cosmos landing experience for article-first navigation.
- Inspirational quote surface on home/dashboard experiences.

## 3) World Management and Collaboration
- Create, view, and update worlds.
- World-level description and metadata editing.
- Default world bootstrap content on creation.
- World membership list and role management.
- Member removal management with role guardrails.
- Invitation-code creation and revocation.
- Join-world flow via invitation code.
- World-level private notes for GM/owner contexts.
- Public sharing toggle per world.
- Public slug validation and availability checks.
- Public URL preview and copy actions.

## 4) Campaign, Arc, and Session Management
- Create, view, and edit campaigns.
- Activate a campaign as the current active campaign.
- Create, view, and edit arcs.
- Activate an arc as the current active arc.
- Arc ordering and organization controls.
- Arc deletion constraints when dependent content exists.
- Create, view, edit, and delete sessions.
- Session date and title management.
- Session list and navigation from arc context.
- Automatic default session-note creation for new sessions.
- Session-level private notes for GM/owner contexts.
- Session summary generation and clearing.
- Cleanup of session-linked content on deletion.
- Canonical session workflows use first-class `Session` entities; legacy `ArticleType.Session` handling is compatibility-only.
- Arc tree add-child and quick-add session actions create `Session` entities directly.

## 5) Articles and Knowledge Base
- Hierarchical article tree with unlimited nesting.
- Article creation at root, child, and sibling levels.
- Article editing, moving, and deletion.
- Article path-based navigation.
- Article aliases for alternate naming.
- Article icon customization.
- Article type support (including wiki, character, character-note, and session-note workflows).
- Article visibility controls (public, members-only, private).
- Backlinks panel showing inbound references.
- Outgoing links panel showing referenced content.
- Bulk link-resolution behavior for content references.
- Auto-link suggestion and apply workflow.
- Tutorial/system article protection behaviors.

## 6) Wiki Linking and Rich Editing
- Rich text editor for articles and notes.
- Internal wiki-link syntax support.
- Wiki-link autocomplete and keyboard selection.
- Create-new-article directly from autocomplete.
- Broken-link detection and handling in editor workflows.
- Inline editing model across core entity detail pages.
- Keyboard save shortcut from editing contexts.
- Keyboard shortcut to create sibling article.

## 7) External References and Linked Resources
- External reference insertion using provider-based link syntax.
- External reference autocomplete with provider-aware suggestions.
- External reference preview drawer.
- External references metadata panel on articles.
- Provider support for Open5e content.
- Provider support for SRD 2014 content.
- Provider support for SRD 2024 content.
- Provider support for Ruins of Symbaroum content.
- World-level provider enable/disable settings.

## 8) Documents and Inline Images
- World document upload, listing, update, download, and deletion.
- Document metadata management.
- World-level document visibility via membership access rules.
- Inline image insertion into article/note content.
- Inline image upload via drag-and-drop, paste, or file picker.
- Inline image rendering in authenticated and public viewing contexts.
- Image cleanup when related content is deleted.

## 9) Quests and Session Progress Tracking
- Quest list and detail workflows within arc context.
- Quest create, update, status management, and deletion.
- GM-only quest management controls.
- GM-only quest visibility enforcement.
- Quest timeline/update feed.
- Add and remove quest updates.
- Optional association of quest updates to sessions.
- Concurrency conflict handling for quest edits.

## 10) Character Features
- Character article workflows in the main content model.
- Claim character workflow for players.
- Unclaim character workflow.
- Claimed-character visibility in dashboard context.

## 11) AI Summary Features
- Summary generation for articles.
- Summary generation for campaigns.
- Summary generation for arcs.
- Summary generation for sessions.
- Summary estimate before generation.
- Summary template selection.
- Custom prompt support for summary generation.
- Summary regeneration and clearing flows.
- Summary preview behavior for linked content contexts.

## 12) Search and Navigation
- Global search across user-accessible content.
- Search across titles, content, and hashtag-like tokens.
- Grouped search result presentation.
- Snippet/context display for search matches.
- Click-through navigation from search results to target content.
- Left navigation tree with filtering and expansion state.
- Virtual content grouping in tree (Campaigns, Player Characters, Wiki, External Resources, Uncategorized).
- Breadcrumb navigation for article/public content paths.
- App context persistence for selected world/campaign.

## 13) Drawers, Panels, and Shortcut Surfaces
- Unified right-side drawer host for contextual tools.
- Tutorial drawer surface.
- Metadata drawer surface.
- Quests drawer surface.
- Featured/help drawer surface.
- Keyboard shortcut to open quests drawer.
- Keyboard shortcut to open tutorial drawer.
- Keyboard shortcut to open metadata drawer.
- Keyboard shortcut to open featured/help drawer.

## 14) Public Sharing and Anonymous Viewer
- Public world landing page by slug.
- Public article tree navigation.
- Public article-by-path viewing.
- Public compatibility routing for legacy session-article URL segments when resolving session-note paths.
- Public breadcrumbs and path-aware navigation.
- Public rendering of visible article content only.
- Public read-only behavior for shared content.
- Public visibility enforcement (hidden non-public content).
- Public document/image access for eligible shared content.

## 15) Admin and Operational Features
- Admin world inventory view.
- Admin world deletion workflow.
- Sysadmin tutorial mapping management.
- Tutorial content resolution by page context.
- Admin dependency/status dashboard.
- Admin external-resource tooling surface.
- Render-definition generation and editing utility.

## 16) Settings, Profile, and Supporting Public Pages
- Settings page with profile and account context.
- Data export workflow from settings.
- Import placeholder surface.
- Preferences placeholder surface.
- About page.
- Privacy page.
- Terms of service page.
- Licenses page.
- Change log page.

## 17) Explicitly Out of Scope
- `src/Chronicis.CaptureApp` features are intentionally excluded from this inventory.
