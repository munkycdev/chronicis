# Chronicis - Feature Ideas & Backlog

## Bugs

### Article
- The DM can choose to define article templates for various article types
- Keyboard shortcuts: Ctrl+N context should be smarter for session notes (currently creates siblings, should consider parent context)

### General
- Make it work on an iPad
    - Figure out how to perform the right-click operations
    - Drag and drop reorganization doesn't work
- Default world that new users get added to as an example of how to use the system
- Should I consider a different license, if I want to monetize this eventually?

### External Links
- External link autocomplete should not block internal autocomplete if a user types `[[` then backspaces into `[[srd/` or vice versa
- Preview drawer: ensure it closes cleanly and does not steal editor focus unexpectedly

---

## Cleanup

- Run a cleanup to remove all unused code and styles - this includes cleaning up styling overrides for the fixed mud theme

---

## Session Notes
- Some way to track quests?

---

## Article Features

- Add Azure AI Search as a replacement for the global search (deferred feature until we need it - it's expensive!)
- External link providers:
  - ✅ Blob-backed D&D SRD providers (2014 and 2024 editions) - COMPLETE
  - ✅ Cross-category search - COMPLETE
  - ✅ Hierarchical category navigation - COMPLETE
  - Add `kobold/` provider
  - Allow provider-specific categories (spells, monsters, items)
- Export behavior:
  - Decide how external tokens should export to Markdown (keep token vs convert to normal link)
- ✅ Backlinks for external links - COMPLETE
  - ✅ Show "external references used in this article" panel - COMPLETE

---

## Campaign and Arc Features

- Add the ability to generate an AI summary of child summary notes - this needs to be implemented first!
    - When the player creates a summary, maybe a parent dated article is created and then a child for the player's notes?

---

## World Features

* Ability to add maps and track activity across sessions
    - Maps
    - Nights slept
    - Other stuff?

---

## Characters

- Rich character builder

---

## Utilities

- Utility to run add links to all articles in a world
- Utility to suggest pages for regularly recurring topics/people/creatures
- Bulk AI summary generator - displays articles and allows them to be selected for a quick summary creation
- Provider health check page (list providers and verify suggestions/content endpoints)

---
