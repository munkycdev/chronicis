# Chronicis - Technical Architecture

**Last Updated:** February 3, 2026

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
- **API Framework:** ASP.NET Core Web API (.NET 9)
- **Hosting Model:** Azure Container Apps (`ca-chronicis-api`)
- **Authentication:** Auth0 JWT Bearer authentication

### Data Layer
- **ORM:** Entity Framework Core 9
- **Database:** Azure SQL Database
- **Migration Strategy:** Code-first with EF migrations

### Infrastructure
- **Frontend Hosting:** Azure Container Apps (`ca-chronicis-client`, `chronicis.app`)
- **API Hosting:** Azure Container Apps (`ca-chronicis-api`, `api.chronicis.app`)
- **Container Registry:** Azure Container Registry
- **Database:** Azure SQL Database
- **Blob Storage:** Azure Blob Storage (documents, SRD data)
- **Secrets:** Azure Key Vault (`kv-chronicis`)
- **CI/CD:** GitHub Actions (containerized build and deployment workflows)
- **AI Services:** Azure OpenAI (GPT-4.1-mini)
- **Monitoring:** DataDog APM (in-image agent, direct-to-cloud)

### Auth0 Configuration
- **Custom Domain:** `auth.chronicis.app`
- **Tenant:** `dev-843pl5nrwg3p1xkq.us.auth0.com`
- **Social Connections:** Discord, Google
- **Key Vault Secrets:**
  - `Discord-ClientSecret` - Discord OAuth client secret (for Auth0 social connection)

---

## Deployment Architecture

### Container Apps Architecture

Both the frontend and backend are deployed as containerized applications on Azure Container Apps:

```
┌─────────────────────────────────────────────────────────┐
│ Azure Container Apps                                    │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ ca-chronicis-client (chronicis.app)             │   │
│  │ - Blazor WASM served via nginx                  │   │
│  │ - Static file hosting                           │   │
│  │ - Custom domain with SSL                        │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ ca-chronicis-api (api.chronicis.app)            │   │
│  │ - ASP.NET Core Web API                          │   │
│  │ - DataDog agent (in-image)                      │   │
│  │ - Direct-to-cloud APM traces                    │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
└─────────────────────────────────────────────────────────┘
         │                              │
         │                              └─────────────┐
         │                                            │
         ▼                                            ▼
┌─────────────────────┐                    ┌─────────────────────┐
│ Azure SQL Database  │                    │ DataDog Cloud       │
│ - EF Core           │                    │ - APM Traces        │
│ - Migrations        │                    │ - Logs              │
└─────────────────────┘                    │ - Metrics           │
         │                                 └─────────────────────┘
         │
         ▼
┌─────────────────────┐
│ Azure Blob Storage  │
│ - World documents   │
│ - SRD JSON data     │
│ - SAS URLs          │
└─────────────────────┘
```

### DataDog Observability

The API container includes the DataDog .NET APM tracer configured for direct-to-cloud reporting:

- **In-Image Agent:** DataDog tracer embedded in the application container
- **Transport:** Direct HTTPS to Datadog cloud (no local agent)
- **Traces:** Automatic instrumentation for HTTP, EF Core, SQL
- **Logs:** Structured logging with trace correlation via Serilog
- **Metrics:** .NET runtime metrics enabled

See [observability.md](observability.md) for detailed configuration.

### Blob Storage Architecture

Azure Blob Storage provides file storage for documents and external reference data:

**Document Storage:**
- **Container Pattern:** One container per world (`world-{worldId}-documents`)
- **Access Control:** World membership enforced at API layer
- **Download Strategy:** SAS URLs for direct browser downloads (no API streaming)
- **SAS Token Duration:** 1-hour read-only access
- **Blob Naming:** `{timestamp}_{filename}` for uniqueness
- **Storage Account:** `stchronicis` in `rg-chronicis` resource group

**SRD Reference Data:**
- **Container:** Single container with hierarchical folder structure
- **Format:** Normalized JSON (one file per entity)
- **Organization:** Folders represent categories (e.g., `items/armor/heavy/`)
- **Indexing:** Filename-based for instant startup (~300x faster than content-based)
- **Providers:** SRD 2014 and SRD 2024 editions
- **Access Pattern:** Read-only, cached at provider level

**Connection Management:**
- Connection string stored in Azure Key Vault
- Retrieved via `BlobStorageConnectionString` secret name
- Injected into Container Apps as environment variable

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
│   ├── Chronicis.Api/              # ASP.NET Core Web API
│   │   ├── Controllers/            # API controllers
│   │   ├── Data/                   # DbContext
│   │   ├── Infrastructure/         # Auth, services
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

### Backend (ASP.NET Core)

Standard ASP.NET Core JWT Bearer authentication:

```
Request → JWT Bearer Middleware → Controller
              ↓
         Validate Auth0 JWT
              ↓
         Populate HttpContext.User
              ↓
         [AllowAnonymous] endpoints skip validation
```

**Key Components:**
- `JwtBearerDefaults.AuthenticationScheme` - Standard JWT validation
- `ICurrentUserService` - Extracts user from HttpContext claims
- `[Authorize]` / `[AllowAnonymous]` - Standard ASP.NET Core attributes

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

GET    /api/external-links/suggestions   - External autocomplete suggestions by provider
GET    /api/external-links/content       - External content preview by provider + id

```

### External Link Providers

External link providers are resolved by key via `IExternalLinkProviderRegistry`.

**Provider Types:**

*API-Based Providers (e.g., Open5e):*
- Query external REST APIs in real-time
- Configuration requires `BaseUrl` in app settings
- Example: `ExternalLinks:srd:BaseUrl` for Open5e provider

*Blob-Based Providers (e.g., SRD 2014/2024):*
- Read normalized JSON data from Azure Blob Storage
- Hierarchical folder structure represents categories
- Filename-based indexing for instant startup
- Configuration requires blob storage connection string
- Example providers: `srd14` (2014 SRD), `srd24` (2024 SRD)

**Adding a New Provider:**
1. Create provider class implementing `IExternalLinkProvider` in `src/Chronicis.Api/Services/ExternalLinks/`
2. Implement required methods:
   - `GetSuggestionsAsync(query)` - Returns autocomplete suggestions
   - `GetContentAsync(id)` - Returns preview content as Markdown
   - `Key` property - Provider identifier (e.g., "srd", "srd14")
3. Register provider in `src/Chronicis.Api/Program.cs`
4. Add configuration:
   - API providers: `ExternalLinks:<ProviderKey>:BaseUrl` in local.settings.json
   - Blob providers: Blob storage connection string (already configured)

**Blob Provider Pattern:**
- Storage structure: `/category/subcategory/entity-name.json`
- Index building: Parse filenames (no content downloads)
- Category detection: Longest-match algorithm for hierarchical paths
- Search modes: Category-specific or cross-category
- Performance: <100ms for autocomplete, ~300x faster indexing

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

### External Link Tokens

Chronicis supports external link tokens in the editor for integrating third-party reference data sources.

**Editor Trigger:**
- Typing `[[<sourceKey>/` routes autocomplete to an external provider
- Example: `[[srd/` queries the SRD provider

**Storage Format (External v1):**
- External links are stored as: `[[source|id|title]]`
- `source` is the provider key (example: `srd`)
- `id` is provider-specific stable identifier (for SRD, the API resource path)
- `title` is the display text

**Rendering and Interaction:**
- External links render as distinct "chips" with external styling
- Clicking an external chip opens an in-app preview drawer
- Preview content is normalized as Markdown returned by the API layer

### External Link Providers

External sources are integrated via provider services:

- `IExternalLinkProvider` abstraction (Key, Search, Content)
- Provider registry resolves providers by `sourceKey` (example: `srd`)
- SSRF-safe validation for provider ids (ids are relative paths, not full URLs)

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
