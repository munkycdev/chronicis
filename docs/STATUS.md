# Chronicis - Project Status

**Last Updated:** December 30, 2025  
**Project Phase:** Implementation Complete (Phase 10 of 13)

---

## Current State

Chronicis is a fully functional knowledge management application for tabletop RPG campaigns. The core application is deployed and operational with all foundational features implemented.

### What's Working

**Core Functionality:**
- Hierarchical article organization with infinite nesting
- Tree navigation with lazy loading and search
- Inline WYSIWYG editing with TipTap
- Auto-save on content changes (0.5s debounce)
- Wiki-style links using `[[Article Name]]` syntax
- Backlinks panel showing articles that reference current article
- AI-powered summary generation using Azure OpenAI
- Full-text search across titles, bodies, and links
- URL routing with slugs for bookmarking

**Entity Management:**
- World → Campaign → Arc → Session hierarchy
- Virtual groups for article organization (Characters, Wiki, Uncategorized)
- Entity detail pages for Worlds, Campaigns, and Arcs
- Creation dialogs for all entity types
- Article type categorization (WikiArticle, Character, Session, etc.)

**Infrastructure:**
- Auth0 authentication (Discord and Google OAuth)
- Azure Functions backend with global authentication middleware
- Azure SQL Database with Entity Framework Core
- Centralized HttpClient with automatic token attachment

### Known Issues

- Autocomplete popup occasionally fails to appear for `[[` links in long articles after scrolling
- Navigation tree doesn't reload after adding external links to a World
- First API call is slow when Azure Functions cold-starts

See `Feature Ideas.md` for the complete bug list.

---

## Progress Summary

| Phase | Name | Status |
|-------|------|--------|
| 0 | Infrastructure & Setup | ✅ Complete |
| 1 | Data Model & Tree Nav | ✅ Complete |
| 2 | CRUD Operations | ✅ Complete |
| 3 | Search & Discovery | ✅ Complete |
| 4 | Markdown & Rich Content | ✅ Complete |
| 5 | Visual Design & Polish | ✅ Complete |
| 6 | Hashtag System | ✅ Complete → Replaced by Wiki Links |
| 7 | Backlinks & Graph | ✅ Complete |
| 8 | AI Summaries | ✅ Complete |
| 9 | Advanced Search | ✅ Complete |
| 9.5 | Auth Architecture | ✅ Complete |
| 10 | Taxonomy & Entities | ✅ Complete |
| 11 | Drag & Drop Enhancements | 📜 Next |
| 12 | Icons & Polish | ⏳ Pending |
| 13 | Testing & Deploy | ⏳ Pending |

**Overall Progress:** 10 of 13 phases complete (~77%)

---

## Next Steps

### Phase 11: Drag & Drop Enhancements
- Drag articles between parent articles
- Drag to reorder within same parent
- Visual drop indicators
- Undo functionality for moves

### Phase 12: Icons & Polish
- EmojiPicker component improvements
- Icon display in breadcrumbs and headers
- Animation polish
- Enhanced tooltips

### Phase 13: Testing & Deployment
- Unit tests for all services
- Integration tests for API endpoints
- Performance optimization
- Production deployment validation

---

## Environment Setup

**Prerequisites:**
- .NET 9 SDK
- Azure Functions Core Tools
- Visual Studio 2022 or VS Code
- SQL Server (LocalDB, Express, or Docker)

**Running Locally:**

```powershell
# Run API
cd src\Chronicis.Api
func start

# Run Client (separate terminal)
cd src\Chronicis.Client
dotnet watch run
```

**Database Migrations:**

```powershell
cd src\Chronicis.Api
dotnet ef migrations add MigrationName
dotnet ef database update
```

---

## Key Routes

| Route | Page | Description |
|-------|------|-------------|
| `/` | Landing | Public landing page |
| `/dashboard` | Dashboard | Home with stats and quick actions |
| `/world/{id}` | WorldDetail | Edit world, create content |
| `/campaign/{id}` | CampaignDetail | Edit campaign, manage arcs |
| `/arc/{id}` | ArcDetail | Edit arc, manage sessions |
| `/article/{path}` | Articles | Article detail with editing |
| `/search` | Search | Global search results |

---

## Related Documents

- [ARCHITECTURE.md](ARCHITECTURE.md) - Technical architecture
- [FEATURES.md](FEATURES.md) - Feature documentation
- [CHANGELOG.md](CHANGELOG.md) - Version history
- [Feature Ideas.md](Feature%20Ideas.md) - Backlog and bug list
