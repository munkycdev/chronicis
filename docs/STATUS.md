# Chronicis - Project Status

**Last Updated:** January 2, 2026  
**Project Phase:** Private Articles & Multi-User Collaboration Complete

---

## Current State

Chronicis is a fully functional knowledge management application for tabletop RPG campaigns. The core application is deployed and operational with all foundational features implemented, including multi-user collaboration with invitation codes and private article support.

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
- Drag & drop article reorganization

**Entity Management:**
- World → Campaign → Arc → Session hierarchy
- Virtual groups for article organization (Characters, Wiki, Uncategorized)
- Entity detail pages for Worlds, Campaigns, and Arcs
- Creation dialogs for all entity types
- Article type categorization (WikiArticle, Character, Session, etc.)
- Character claiming system

**Multi-User Collaboration:**
- World-level membership (all campaigns in a world share members)
- Invitation codes in XXXX-XXXX format for easy sharing
- Role-based access (GM, Player, Observer)
- Member management UI (view, change roles, remove)
- Join world flow from Dashboard
- New users start with empty dashboard (no auto-created world)
- Private articles visible only to their creator

**Private Articles:**
- Toggle article privacy from the metadata drawer (right panel)
- Only article creators can mark their articles as private
- Private articles show a lock icon in the tree view (replaces normal icon)
- Lock icon updates immediately without tree reload
- Private articles filtered from other users' views

**Public Sharing:**
- Public world toggle with globally unique slugs
- Three-tier article visibility (Public, MembersOnly, Private)
- Anonymous read-only access at `/w/{publicSlug}`
- Public article tree with navigation
- Public slug availability checking with suggestions
- Copy-to-clipboard for public URLs

**Dashboard:**
- Hero section with gradient background and welcome message
- "Create New World" and "Join a World" action buttons
- Contextual server-generated prompts based on user state
- World-centric panels with expandable content
- Active campaign highlighting with session stats
- Claimed characters display with click-to-navigate
- Stats panel showing chronicle totals

**Infrastructure:**
- Auth0 authentication (Discord and Google OAuth)
- Azure Functions backend with global authentication middleware
- Azure SQL Database with Entity Framework Core
- Centralized HttpClient with automatic token attachment
- Application Insights telemetry with availability test keep-alive

### Known Issues

See `Feature Ideas.md` for the complete bug list and backlog.

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
| 10 | Drag & Drop | ✅ Complete |
| 10.5 | Public World Sharing | ✅ Complete |
| 10.6 | Multi-User Collaboration | ✅ Complete |
| 10.7 | Private Articles | ✅ Complete |
| 11 | Icons & Polish | ⏳ Pending |
| 12 | Testing & Deploy | ⏳ Pending |

**Additional Work Completed:**
- Taxonomy & Entity System (Worlds, Campaigns, Arcs)
- Dashboard Redesign with World Panels
- Character Claiming System
- First-time User Onboarding
- Public World Sharing with Anonymous Access
- Multi-User Collaboration with Invitation Codes

**Overall Progress:** ~92% of core features complete

---

## Next Steps

### Phase 11: Icons & Polish
- EmojiPicker component improvements
- Icon display in breadcrumbs and headers
- Animation polish
- Enhanced tooltips

### Phase 12: Testing & Deployment
- Unit tests for all services
- Integration tests for API endpoints
- Performance optimization
- Production deployment validation

### Optional Enhancements
- Contextual prompt system for dashboard
- Advanced collaboration features
- Audio capture integration

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
| `/dashboard` | Dashboard | World panels and quick actions |
| `/getting-started` | Onboarding | First-time user wizard |
| `/world/{slug}` | WorldDetail | Edit world, create content, public sharing settings |
| `/campaign/{id}` | CampaignDetail | Edit campaign, manage arcs |
| `/arc/{id}` | ArcDetail | Edit arc, manage sessions |
| `/article/{path}` | Articles | Article detail with editing |
| `/search` | Search | Global search results |
| `/w/{publicSlug}` | PublicWorld | Anonymous world view |
| `/w/{publicSlug}/{path}` | PublicArticle | Anonymous article view |

---

## Related Documents

- [ARCHITECTURE.md](ARCHITECTURE.md) - Technical architecture
- [FEATURES.md](FEATURES.md) - Feature documentation
- [CHANGELOG.md](CHANGELOG.md) - Version history
- [Feature Ideas.md](Feature%20Ideas.md) - Backlog and bug list
