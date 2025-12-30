# Chronicis

<div align="center">
  <img src="https://raw.githubusercontent.com/munkycdev/chronicis/refs/heads/main/src/Chronicis.Client/wwwroot/images/logo.png" alt="Chronicis Logo" width="200"/>
  
  **Your Chronicle Awaits**
  
  A modern knowledge management platform for tabletop RPG campaigns
  
  [![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
  [![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
  [![Azure](https://img.shields.io/badge/Azure-Functions-0078D4?logo=microsoftazure)](https://azure.microsoft.com/en-us/services/functions/)
  [![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
</div>

---

## ⚠️ About This Project

**This is a learning exercise in AI-assisted development ("vibe coding").** The entire codebase has been generated through iterative conversations with Claude AI as part of exploring how software engineering managers can effectively guide and collaborate with AI coding assistants.

**What this means:**
- 🤖 All code is Claude-generated
- 🎓 Primary goal is learning AI-assisted development workflows
- 🔬 Experimental approach to architecture and implementation
- 📚 Not production-ready or following all best practices
- 🚧 Code quality reflects AI generation patterns, not human engineering standards

**Please judge accordingly!** This is about the journey of learning to work with AI tools, not about showcasing perfect code.

---

## 📖 About

Chronicis is a web-based knowledge management application designed specifically for Dungeons & Dragons and other tabletop RPG campaigns. Built with modern web technologies, it provides an elegant, efficient way to organize campaign notes, track entities, and discover connections across your game world.

### Key Features

- **✅ Hierarchical Article Organization** - Nest articles infinitely deep to mirror your campaign structure
- **✅ Wiki-Style Links** - `[[Article Name]]` syntax for intuitive cross-references
- **✅ Inline WYSIWYG Editor** - Real-time markdown rendering with TipTap
- **✅ Auto-Save** - Never lose your work (0.5s debounce on changes)
- **✅ AI Summaries** - Generate entity summaries from backlink analysis
- **✅ Full-Text Search** - Search across titles, content, and links
- **✅ Campaign Taxonomy** - World → Campaign → Arc → Session hierarchy
- **✅ Backlinks Panel** - See what articles reference the current one

### Coming Soon

- **🔜 Drag & Drop** - Reorganize hierarchy with mouse
- **🔜 Custom Icons** - Emoji icons for visual distinction
- **🔜 Multi-User Collaboration** - Share worlds with your gaming group

---

## 🎨 Design Philosophy

Chronicis follows an **Obsidian-inspired inline editing paradigm**:

- **Always Editable** - No modal dialogs; edit directly in place
- **Auto-Save** - Changes save automatically (no more lost work)
- **Hierarchical** - Infinitely nested articles mirror campaign structure
- **Connected** - Wiki links create automatic relationships between entities
- **Fast** - Optimized for quick note-taking during game sessions

### Visual Style

- **Color Palette**: Deep blue-grey (#1F2A33), beige-gold (#C4AF8E)
- **Typography**: Spellweaver Display (headings), Roboto (body)
- **Effects**: Soft gold glows, smooth transitions, subtle shadows

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [STATUS.md](docs/STATUS.md) | Current project state and progress |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Technical architecture and data model |
| [FEATURES.md](docs/FEATURES.md) | Feature documentation and API reference |
| [CHANGELOG.md](docs/CHANGELOG.md) | Version history |
| [Feature Ideas.md](docs/Feature%20Ideas.md) | Backlog and known issues |

---

## 🚀 Getting Started

### Prerequisites

- .NET 9 SDK
- Azure Functions Core Tools
- Visual Studio 2022 or VS Code
- SQL Server (LocalDB, Express, or Docker)

### Running Locally

```powershell
# Run API (terminal 1)
cd src\Chronicis.Api
func start

# Run Client (terminal 2)
cd src\Chronicis.Client
dotnet watch run
```

### Database Setup

```powershell
cd src\Chronicis.Api
dotnet ef database update
```

---

## 🏗️ Project Structure

```
chronicis/
├── src/
│   ├── Chronicis.Client/      # Blazor WASM frontend
│   ├── Chronicis.Api/         # Azure Functions backend
│   ├── Chronicis.Shared/      # Shared models and DTOs
│   └── Chronicis.CaptureApp/  # Audio capture utility
├── docs/                      # Documentation
└── Chronicis.sln
```

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **Claude by Anthropic** - For generating all the code
- **MudBlazor** - Excellent Blazor component library
- **TipTap** - Beautiful WYSIWYG editor
- **The D&D Community** - Inspiration for campaign management needs

---

<div align="center">
  <sub>Built with Claude AI | Learning to vibe code, one feature at a time 🤖✨</sub>
</div>
