# Chronicis

<div align="center">
  <img src="docs/images/logo.png" alt="Chronicis Logo" width="200"/>
  
  **Your Chronicle Awaits**
  
  A modern knowledge management platform for tabletop RPG campaigns
  
  [![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
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

**Please judge accordingly!** This is about the journey of learning to work with AI tools, not about showcasing perfect code. If you're looking for examples of production-quality .NET applications, this probably isn't it. But if you're curious about what AI can build with the right guidance, read on!

---

## 📖 About

Chronicis is a web-based knowledge management application designed specifically for Dungeons & Dragons and other tabletop RPG campaigns. Built with modern web technologies, it provides an elegant, efficient way to organize campaign notes, track entities, and discover connections across your game world.

### Key Features

- **✅ Hierarchical Article Organization** - Nest articles infinitely deep to mirror your campaign structure
- **✅ Tree Navigation** - Expandable sidebar with visual hierarchy and search
- **✅ Inline WYSIWYG Editor** - Real-time markdown rendering with TipTap
- **✅ Auto-Save** - Never lose your work (0.5s delay on content changes)
- **✅ Enhanced Dashboard** - Campaign statistics, recent articles, quick actions
- **✅ URL Routing** - Readable URLs with article slugs for bookmarking
- **✅ Hashtag System** - Automatic entity detection and visual styling (#NPC, #Location)
- **✅ Smart Search** - Title-based tree filtering with ancestor expansion

### Coming Soon

- **🔜 Backlinks & Graph** - Discover which articles reference current entity
- **🔜 AI Summaries** - Generate comprehensive entity summaries from mentions
- **🔜 Content Search** - Full-text search across article bodies
- **🔜 Drag & Drop** - Reorganize hierarchy with mouse
- **🔜 Custom Icons** - Emoji icons for visual distinction

---

## 🎨 Design Philosophy

Chronicis follows an **Obsidian-inspired inline editing paradigm**:

- **Always Editable** - No modal dialogs; edit directly in place
- **Auto-Save** - Changes save automatically after 0.5s (no more lost work)
- **Hierarchical** - Infinitely nested articles mirror campaign structure
- **Connected** - Hashtags create automatic relationships between entities
- **Fast** - Optimized for quick note-taking during game sessions

### Visual Style

- **Color Palette**: Deep blue-grey, beige-gold, slate grey
- **Typography**: Spellweaver Display (headings), Roboto (body)
- **Effects**: Soft gold glows, smooth transitions, subtle shadows
- **Inspiration**: Fantasy aesthetic meets modern UI

---

## 🐛 Known Issues & Quirks

As AI-generated code, there are some expected quirks:

- **Occasional over-engineering** - AI sometimes adds unnecessary abstractions
- **Inconsistent patterns** - Code style varies based on conversation context
- **Verbose comments** - AI loves to explain everything
- **Conservative error handling** - Lots of try-catch blocks and null checks
- **Testing gaps** - AI-generated tests tend to be happy-path focused

These are features, not bugs! They're part of understanding how AI generates code.

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **Claude by Anthropic** - For generating all the code you see here
- **MudBlazor** - Excellent Blazor component library
- **TipTap** - Beautiful WYSIWYG editor
- **The D&D Community** - Inspiration for campaign management needs

---

<div align="center">
  <sub>Built with Claude AI | Learning to vibe code, one feature at a time 🤖✨</sub>
</div>