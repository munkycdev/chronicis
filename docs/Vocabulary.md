# Chronicis Vocabulary

## Purpose

This document defines the canonical terminology used throughout the Chronicis codebase. It exists to reduce semantic overload and provide clear distinctions between concepts that share similar implementation patterns but serve different purposes.

## Core Concepts

### WikiLink

**Definition:** An internal reference from one article to another within the same world/campaign.

**User-Facing Syntax:** `[[Article Title]]` or `[[Article Title|Display Text]]`

**Purpose:** Create a knowledge graph of interconnected campaign content, similar to Wikipedia's internal linking system.

**Examples:**
- `[[Waterdeep]]` - Links to the Waterdeep article
- `[[Vajra Safahr|the Blackstaff]]` - Links to Vajra Safahr but displays as "the Blackstaff"
- `[[Castle Ward]]` - Links to a location article

**Implementation Mapping:**
- **Model:** `ArticleLink` (in `Chronicis.Shared.Models`)
- **DTOs:** Various DTOs in `LinkDtos.cs` (e.g., `LinkSuggestionDto`, `BacklinkDto`)
- **Database:** `ArticleLinks` table with source/target article relationships
- **Features:** Autocomplete, backlinks panel, hover previews

**Key Characteristics:**
- Bidirectional (source article → target article)
- Resolved at save time to specific article IDs
- Broken links are detectable (target article deleted)
- Position-tracked for context snippets

---

### ExternalReference

**Definition:** An embedded reference to third-party D&D content from external resource providers (SRD, Open5e, etc.).

**User-Facing Syntax:** Rich editor insertion, rendered as styled spans with metadata attributes.

**Purpose:** Link campaign content to official/community D&D rules and lore without duplicating that content.

**Examples:**
- Reference to "The Fiend" warlock patron from SRD
- Link to "Fireball" spell from Open5e
- Reference to "Aarakocra" race from external source

**Implementation Mapping:**
- **Model:** `ArticleExternalLink` (in `Chronicis.Shared.Models`)
- **DTOs:** `ArticleExternalLinkDto` (in `ArticleExternalLinkDto.cs`)
- **Database:** `ArticleExternalLinks` table
- **Features:** Resource provider integration, external content display

**Key Characteristics:**
- One-way reference (article → external system)
- Contains source identifier (e.g., "srd14", "open5e")
- Contains external ID (e.g., "classes/the-fiend")
- Display title cached for performance
- No backlink tracking (external system is authoritative)

---

### WorldBookmark

**Definition:** A user-saved external URL associated with a world/campaign for quick access.

**User-Facing Syntax:** Managed through world settings/dashboard UI.

**Purpose:** Provide quick access to external tools and resources that support the campaign (Roll20, D&D Beyond, Discord servers, etc.).

**Examples:**
- "Roll20 Campaign" → https://roll20.net/campaigns/details/12345
- "D&D Beyond Party" → https://www.dndbeyond.com/campaigns/67890
- "Campaign Discord" → https://discord.gg/abcdef
- "Forgotten Realms Wiki" → https://forgottenrealms.fandom.com

**Implementation Mapping:**
- **Model:** `WorldLink` (in `Chronicis.Shared.Models`)
- **DTOs:** `WorldLinkDto`, `WorldLinkCreateDto`, `WorldLinkUpdateDto` (in `WorldLinkDtos.cs`)
- **Database:** `WorldLinks` table
- **Features:** World dashboard, quick access links

**Key Characteristics:**
- Associated with World, not individual articles
- User-managed (create, edit, delete)
- Simple URL + title + description
- No deep integration with external systems
- No content extraction or caching

---

## Terminology Decision Rationale

### Why "WikiLink" instead of "ArticleLink"?

- **User Mental Model:** Users understand "wiki links" from Wikipedia/Notion/Obsidian
- **Intent Communication:** "WikiLink" immediately conveys bidirectional knowledge graph linking
- **Domain Language:** TTRPG community already uses "wiki" terminology for campaign knowledge bases

### Why "ExternalReference" instead of "ExternalLink"?

- **Semantic Clarity:** "Reference" implies integration with external content, not just a hyperlink
- **Distinction from WorldBookmark:** Both involve external URLs, but ExternalReferences are embedded in content
- **Implementation Accuracy:** These references include metadata (source, ID, display title) beyond a simple URL

### Why "WorldBookmark" instead of "WorldLink"?

- **User Mental Model:** "Bookmark" is universally understood as "saved link for quick access"
- **Functional Clarity:** Distinguishes from WikiLinks (internal) and ExternalReferences (embedded)
- **Scope Accuracy:** These are world-level, not article-level or content-embedded

---

## Usage Guidelines

### For Developers

When working with linking functionality:

1. **Ask "What type of link?"**
   - Article-to-article? → WikiLink
   - Article-to-SRD? → ExternalReference  
   - World-to-external-tool? → WorldBookmark

2. **Use vocabulary terms in:**
   - Code comments
   - PR descriptions
   - Architecture discussions
   - Feature specifications

3. **Current code uses old names:**
   - `ArticleLink` means WikiLink
   - `ArticleExternalLink` means ExternalReference
   - `WorldLink` means WorldBookmark

### For Feature Design

When designing new features:

- **WikiLink features:** Autocomplete, hover previews, backlinks, graph visualization
- **ExternalReference features:** Resource provider integration, content sync, external content display
- **WorldBookmark features:** Dashboard widgets, quick access menus, URL management

---

## Migration Plan

This vocabulary document establishes the *desired* terminology. The codebase currently uses different names for historical reasons.

**Current Implementation Names:**
- WikiLink → `ArticleLink` (model), `LinkDtos` (DTOs)
- ExternalReference → `ArticleExternalLink` (model/DTO)
- WorldBookmark → `WorldLink` (model/DTO)

**Future Phases** (not part of this PR):
1. Rename internal/private classes and methods
2. Introduce new DTO names alongside old (with deprecation notices)
3. Update API endpoints to use new terminology
4. Remove deprecated DTOs after grace period

**For now:** Use vocabulary terms in documentation and comments, but reference existing implementation names when writing code.

---

## Related Documentation

- **Architecture:** See `docs/ARCHITECTURE.md` for system-level link handling
- **Features:** See `docs/FEATURES.md` for user-facing link functionality
- **API:** See endpoint documentation for current DTO contracts (uses pre-vocabulary names)

---

## Questions or Feedback?

If terminology is unclear or you discover edge cases not covered here, please update this document or raise the issue in the team discussion.

**Last Updated:** 2025-02-10
