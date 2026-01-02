# Chronicis - Technical Architecture

**Last Updated:** December 30, 2025

---

## Overview

Chronicis is a web-based knowledge management application for tabletop RPG campaigns. Built with a modern .NET stack, it provides hierarchical content organization, wiki-style linking, and AI-powered summaries.

---

## Technology Stack

### Frontend
- **Framework:** Blazor WebAssembly (.NET 9)
- **UI Library:** MudBlazor
- **Rich Text Editor:** TipTap v3.11.0 (via CDN)
- **State Management:** Scoped services pattern
- **Authentication:** Auth0 OIDC

### Backend
- **API Framework:** Azure Functions (Isolated Worker, .NET 9)
- **Hosting Model:** Serverless (Azure Static Web Apps managed functions)
- **Authentication:** Auth0 JWT validation via middleware

### Data Layer
- **ORM:** Entity Framework Core 9
- **Database:** Azure SQL Database
- **Migration Strategy:** Code-first with EF migrations

### Infrastructure
- **Hosting:** Azure Static Web Apps
- **Secrets:** Azure Key Vault (`kv-chronicis-dev`)
- **CI/CD:** GitHub Actions
- **AI Services:** Azure OpenAI (GPT-4.1-mini)
- **Monitoring:** Application Insights (`appi-chronicis-dev`)
  - Telemetry from Azure Functions (Worker Service integration)
  - Availability test for keep-alive (5-minute interval)

### Auth0 Configuration
- **Custom Domain:** `auth.chronicis.app`
- **Tenant:** `dev-843pl5nrwg3p1xkq.us.auth0.com`
- **Social Connections:** Discord, Google
- **Key Vault Secrets:**
  - `Discord-ClientSecret` - Discord OAuth client secret (for Auth0 social connection)

---

## Solution Structure

```
chronicis/
├── src/
│   ├── Chronicis.Client/           # Blazor WASM
│   │   ├── Components/
│   │   │   ├── Articles/           # Article-related components
│   │   │   ├── Context/            # Context providers
│   │   │   ├── Dialogs/            # Modal dialogs
│   │   │   ├── Layout/             # App layout components
│   │   │   ├── Routing/            # Navigation components
│   │   │   └── Shared/             # Common components
│   │   ├── Models/                 # Client-side models
│   │   ├── Pages/                  # Routable pages
│   │   ├── Services/               # API services
│   │   ├── Utilities/              # Helper classes
│   │   └── wwwroot/                # Static assets
│   │       ├── css/                # Stylesheets
│   │       └── js/                 # JavaScript interop
│   │
│   ├── Chronicis.Api/              # Azure Functions
│   │   ├── Data/                   # DbContext
│   │   ├── Functions/              # HTTP endpoints
│   │   ├── Infrastructure/         # Auth, middleware
│   │   ├── Migrations/             # EF migrations
│   │   └── Services/               # Business logic
│   │
│   ├── Chronicis.Shared/           # Shared library
│   │   ├── DTOs/                   # Data transfer objects
│   │   ├── Enums/                  # Enumeration types
│   │   ├── Models/                 # Entity models
│   │   └── Utilities/              # Shared helpers
│   │
│   └── Chronicis.CaptureApp/       # Audio capture utility (WinForms)
│
├── docs/                           # Documentation
└── Chronicis.sln                   # Solution file
```

---

## Data Model

### Entity Hierarchy

```
World (top-level container)
├── Campaign (gaming group space)
│   └── Arc (story arc)
│       └── Article (type: Session)
│           └── Article (type: SessionNote)
├── Article (type: Character)
│   └── Article (type: CharacterNote)
├── Article (type: WikiArticle)
│   └── Article (type: WikiArticle)
└── Article (type: Legacy)
```

### Core Entities

**World**
- Top-level isolation boundary for content
- Owned by a single user (DM)
- Contains Campaigns, Characters, and Wiki content
- Has external links collection (Roll20, D&D Beyond, etc.)

**Campaign**
- Gaming group's collaborative space within a World
- Sequential campaigns share World resources
- Contains Arcs for story organization
- Supports AI summary generation

**Arc**
- Story arc within a Campaign
- Organizational container for Sessions
- Has sort order for sequencing

**Article**
- Core content entity with self-referencing hierarchy
- Types: WikiArticle, Character, CharacterNote, Session, SessionNote, Legacy
- Supports wiki links via `[[guid|display]]` format
- AI summary generation with backlink analysis

**ArticleLink**
- Junction table for wiki-style links
- Stores source/target article references
- Tracks display text and position in body

### Enumerations

**ArticleType:**
- `WikiArticle (1)` - General wiki content
- `Character (2)` - Player/NPC character
- `CharacterNote (3)` - Notes under a character
- `Session (10)` - Game session article
- `SessionNote (11)` - Notes under a session
- `Legacy (99)` - Unmigrated content

**ArticleVisibility:**
- `Public` - Visible to all campaign members
- `Private` - Visible to author only

**CampaignRole:**
- `GameMaster` - Full access
- `Player` - Standard access
- `Observer` - Read-only access

---

## Authentication Architecture

### Backend (Azure Functions)

Global authentication middleware handles JWT validation:

```
Request → AuthenticationMiddleware → Function
              ↓
         Validate JWT
              ↓
         Store user in FunctionContext.Items["User"]
              ↓
         [AllowAnonymous] endpoints skip validation
```

**Key Components:**
- `AuthenticationMiddleware` - Validates Auth0 JWT tokens
- `AllowAnonymousAttribute` - Marks public endpoints
- `FunctionContextExtensions` - `GetUser()`, `GetRequiredUser()` helpers

### Frontend (Blazor WASM)

Centralized HttpClient with automatic token attachment:

```
Service → HttpClient → AuthorizationMessageHandler → API
                              ↓
                       Attach Bearer token
```

**Key Components:**
- `AuthorizationMessageHandler` - DelegatingHandler for token attachment
- `IHttpClientFactory` - Named client "ChronicisApi"

---

## API Structure

### Endpoint Patterns

```
GET    /api/articles              - List root articles
GET    /api/articles/{id}         - Get article by ID
GET    /api/articles/path/{path}  - Get article by slug path
POST   /api/articles              - Create article
PUT    /api/articles/{id}         - Update article
DELETE /api/articles/{id}         - Delete article
PATCH  /api/articles/{id}/parent  - Move article

GET    /api/worlds                - List user's worlds
GET    /api/worlds/{id}           - Get world details
POST   /api/worlds                - Create world
PUT    /api/worlds/{id}           - Update world
DELETE /api/worlds/{id}           - Delete world

GET    /api/campaigns/{id}        - Get campaign details
POST   /api/campaigns             - Create campaign
PUT    /api/campaigns/{id}        - Update campaign

GET    /api/arcs/{id}             - Get arc details
POST   /api/arcs                  - Create arc
PUT    /api/arcs/{id}             - Update arc

GET    /api/articles/{id}/backlinks        - Get backlinks
POST   /api/articles/{id}/summary/generate - Generate AI summary
GET    /api/articles/search                - Full-text search
GET    /api/worlds/{id}/link-suggestions   - Autocomplete suggestions
POST   /api/articles/resolve-links         - Resolve link targets
```

---

## Frontend Architecture

### Service Layer

**API Services:**
- `ArticleApiService` - Article CRUD operations
- `WorldApiService` - World management
- `CampaignApiService` - Campaign operations
- `ArcApiService` - Arc operations
- `SearchApiService` - Global search
- `LinkApiService` - Link suggestions and resolution
- `AISummaryApiService` - Summary generation

**State Services:**
- `TreeStateService` - Tree view state management
- `AppContextService` - Current world/campaign context
- `ArticleCacheService` - Article data caching
- `BreadcrumbService` - Navigation breadcrumbs

### TipTap Integration

Custom TipTap extensions via JavaScript interop:

- `wikiLinkExtension.js` - Wiki link node handling
- `tipTapIntegration.js` - Editor lifecycle management

**Markdown Format:**
- Links stored as: `[[guid|display text]]` or `[[guid]]`
- Rendered as clickable inline elements
- Broken links shown with visual indicator

---

## Key Design Decisions

### Inline Editing Paradigm
- No modal dialogs for content editing
- Auto-save with 0.5s debounce
- Always-editable fields like Obsidian

### Wiki Links over Hashtags
- `[[Article Name]]` syntax for intuitive linking
- GUID-based storage for rename stability
- Server-side link parsing and sync

### Virtual Groups
- Tree navigation uses virtual groups (Campaigns, Characters, Wiki)
- Groups determined by ArticleType, not folder structure
- Simplifies organization without rigid hierarchy

### Authentication Middleware
- Global JWT validation vs per-function auth
- Cleaner function code
- Consistent error responses

---

## Performance Targets

| Operation | Target |
|-----------|--------|
| Initial Load | < 3 seconds |
| Tree Expansion | < 300ms |
| Article Display | < 500ms |
| Search Results | < 1 second |
| Auto-Save | < 500ms |
| AI Summary Generation | < 30 seconds |
| Entity Page Load | < 500ms |

---

## Related Documents

- [STATUS.md](STATUS.md) - Project status
- [FEATURES.md](FEATURES.md) - Feature documentation
- [CHANGELOG.md](CHANGELOG.md) - Version history
