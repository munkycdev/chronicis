# Chronicis

<div align="center">
  <img src="https://raw.githubusercontent.com/munkycdev/chronicis/refs/heads/main/src/Chronicis.Client/wwwroot/images/logo.png" alt="Chronicis Logo" width="200"/>
  
  **Your Chronicle Awaits**
  
  A modern knowledge management platform for tabletop RPG campaigns
  
  [![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
  [![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
  [![Azure](https://img.shields.io/badge/Azure-Container%20Apps-0078D4?logo=microsoftazure)](https://azure.microsoft.com/en-us/products/container-apps/)
  [![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
</div>

---

## 📖 About

Chronicis is a web-based knowledge management application designed specifically for Dungeons & Dragons and other tabletop RPG campaigns. Think of it as **Obsidian for D&D** — built with modern web technologies, it provides an elegant, efficient way to organize campaign notes, track entities, discover connections, and collaborate with your gaming group.

Chronicis is developed in active partnership with [Claude](https://claude.ai) (Anthropic) and [Codex](https://openai.com/codex) (OpenAI), serving as both a fully functional product and a real-world testbed for AI-assisted software development at scale.

---

## ✨ Features

### Content Management
- **Hierarchical Article Organization** — Nest articles infinitely deep to mirror your campaign structure
- **Wiki-Style Links** — `[[Article Name]]` syntax for intuitive cross-references with autocomplete
- **Inline WYSIWYG Editor** — Real-time rich text editing with TipTap
- **Auto-Save** — Never lose your work (automatic save on changes with 0.5s debounce)
- **Drag & Drop** — Reorganize your content hierarchy with ease
- **Custom Icons** — Emoji icons for visual distinction in the tree
- **Inline Images** — Drag, paste, or upload images directly into article content

### Campaign Structure
- **World → Campaign → Arc → Session** — Full taxonomy for organizing your games
- **Virtual Groups** — Automatic organization into Characters, Wiki, and Campaigns sections
- **Character Management** — Player and NPC profiles with claiming system
- **Multi-Author Session Notes** — Every player captures their own perspective on each session
- **GM Private Notes** — Dedicated private planning area on every major entity page, invisible to players

### Knowledge Discovery
- **Backlinks Panel** — See all articles that reference the current one
- **Full-Text Search** — Search across titles, content, and wiki links
- **AI Summaries** — Generate comprehensive entity summaries using Azure OpenAI
- **Context Snippets** — See exactly where search terms appear in results
- **External Reference Library** — Embed D&D SRD content (spells, monsters, magic items, and more) directly into articles via `[[srd/`, `[[srd14/`, or `[[srd24/` autocomplete

### Collaboration
- **Multi-User Worlds** — Invite players with shareable codes (XXXX-XXXX format)
- **Role-Based Access** — Game Master, Player, and Observer roles
- **Private Articles** — Keep GM secrets hidden from players with a lock toggle
- **Public Sharing** — Share your world publicly with a unique URL (no login required for viewers)
- **Shared Quest Log** — GM-created quests visible to all players; Ctrl+Q to open from anywhere

### Onboarding
- **Tutorial World** — New users receive a fully populated example world on first login to explore immediately
- **Contextual Help Sidebar** — Page-specific guidance that follows you through the app; stays pinned open in the tutorial world

### Document Management
- **File Uploads** — Store PDFs, images, Word docs, Excel sheets, and more (up to 200MB)
- **Azure Blob Storage** — Secure, scalable file storage with SAS URL downloads

### Data Portability
- **Export to Markdown** — Download your entire world as organized Markdown files
- **YAML Frontmatter** — Metadata preserved in exports
- **Obsidian Compatible** — Folder structure works with Obsidian and similar tools

---

## 🎨 Design Philosophy

Chronicis follows an **Obsidian-inspired inline editing paradigm**:

- **Always Editable** — No modal dialogs; edit directly in place
- **Auto-Save** — Changes save automatically as you type
- **Hierarchical** — Infinitely nested articles mirror campaign structure
- **Connected** — Wiki links create automatic relationships between entities
- **Fast** — Optimized for quick note-taking during game sessions

### Visual Style

- **Color Palette**: Deep blue-grey (#1F2A33), beige-gold (#C4AF8E), soft off-white (#F4F0EA)
- **Typography**: Spellweaver Display (headings), Roboto (body)
- **Effects**: Soft gold glows, smooth transitions, subtle shadows
- **Theme**: Fantasy-inspired but modern and professional

---

## 🖼️ Screenshots

<div align="center">
  <img src="docs/ScreenshotDemo.png" alt="Chronicis Demo" width="800"/>
</div>

---

## 🚀 Getting Started

### Prerequisites

- .NET 9 SDK
- Azure Functions Core Tools v4
- Visual Studio 2022 or VS Code with C# extension
- SQL Server (LocalDB, Express, or Docker)
- Azure subscription (for full deployment)

### Running Locally

```powershell
# Clone the repository
git clone https://github.com/munkycdev/chronicis.git
cd chronicis

# Set up the database
cd src\Chronicis.Api
dotnet ef database update

# Run API (terminal 1)
func start

# Run Client (terminal 2)
cd ..\Chronicis.Client
dotnet watch run
```

The client will be available at `https://localhost:5001` and the API at `http://localhost:7071`.

### Configuration

Create `local.settings.json` in the Api project:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "Server=(localdb)\\mssqllocaldb;Database=Chronicis;Trusted_Connection=True;"
  }
}
```

---

## 🏗️ Architecture

### Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | Blazor WebAssembly, MudBlazor, TipTap |
| Backend | Azure Functions (.NET 9 Isolated) |
| Database | Azure SQL Database, Entity Framework Core |
| Storage | Azure Blob Storage |
| Auth | Auth0 (Google, Discord OAuth) |
| AI | Azure OpenAI (GPT-4.1-mini) |
| Hosting | Azure Container Apps |
| Observability | DataDog APM |

### Project Structure

```
chronicis/
├── src/
│   ├── Chronicis.Client/      # Blazor WASM frontend
│   │   ├── Components/        # Razor components
│   │   ├── Pages/             # Route pages
│   │   ├── Services/          # API clients and state
│   │   └── wwwroot/           # Static assets, JS, CSS
│   ├── Chronicis.Api/         # Azure Functions backend
│   │   ├── Functions/         # HTTP endpoints
│   │   ├── Services/          # Business logic
│   │   ├── Data/              # EF Core DbContext
│   │   └── Infrastructure/    # Auth, middleware
│   ├── Chronicis.Shared/      # Shared models and DTOs
│   └── Chronicis.CaptureApp/  # Windows audio capture (prototype)
├── docs/                      # Documentation
└── Chronicis.sln
```

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [QUICK-START.md](docs/QUICK-START.md) | User guide and tutorials |
| [STATUS.md](docs/STATUS.md) | Current project state and progress |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Technical architecture and data model |
| [FEATURES.md](docs/FEATURES.md) | Feature documentation and API reference |
| [CHANGELOG.md](docs/CHANGELOG.md) | Version history |

---

## 🗺️ Roadmap

### Completed ✅
- Hierarchical article organization with infinite nesting
- Wiki-style linking (`[[Article Name]]`) with autocomplete and GUID-based resolution
- TipTap WYSIWYG editor with auto-save and inline image upload
- World → Campaign → Arc → Session taxonomy
- Multi-author session notes (every player's perspective captured independently)
- GM private notes on World, Campaign, Arc, and Session pages
- Tutorial world cloned for every new user on first login
- Contextual help sidebar with page-specific guidance
- Multi-user collaboration with invitation codes
- Role-based access control (GM, Player, Observer)
- Private articles with lock icon in tree
- Public world sharing with unique slugs and anonymous read-only viewer
- AI-powered summary generation (Azure OpenAI GPT-4.1-mini)
- Full-text search across titles, bodies, and wiki links
- External reference library: Open5e live API + blob-backed SRD 2014 & 2024
- Document upload and management (Azure Blob Storage, up to 200MB)
- Shared quest log with per-player notes (Ctrl+Q)
- Export to Markdown zip archive with YAML frontmatter
- DataDog APM with in-image agent
- Migration from Azure Static Web Apps to Azure Container Apps

### Planned 🔜
- Knowledge base Q&A (RAG via Qdrant + Azure OpenAI)
- Import from Obsidian / Notion
- Mobile-optimized experience
- Real-time collaborative editing
- Session audio transcription integration

---

## 🤝 Contributing

Suggestions and feedback are welcome! Feel free to:

- Open issues for bugs or feature ideas
- Submit PRs for improvements
- Share your experience using Chronicis for your campaigns

---

## 📝 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **[Claude](https://claude.ai) by Anthropic** — Primary AI development partner throughout the project
- **[Codex](https://openai.com/codex) by OpenAI** — AI development partner for targeted feature work
- **MudBlazor** — Excellent Blazor component library
- **TipTap** — Beautiful WYSIWYG editor
- **Auth0** — Authentication made easy
- **The D&D Community** — Inspiration for campaign management needs

---

<div align="center">
  
  **[Live Demo](https://chronicis.app)** · **[Documentation](docs/QUICK-START.md)** · **[Changelog](docs/CHANGELOG.md)**
  
  <sub>Developed in partnership with Claude (Anthropic) and Codex (OpenAI) 🤖✨</sub>
  
</div>
