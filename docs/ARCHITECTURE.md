# Chronicis Architecture

Chronicis is a Blazor WebAssembly + ASP.NET Core application designed to manage structured world-building data for tabletop RPG campaigns.

This document reflects the current architecture following the 2026 consolidation and refactoring effort.

---

# High-Level Overview

Chronicis consists of:

- **Client**: Blazor WebAssembly (Chronicis.Client)
- **API**: ASP.NET Core (Chronicis.Api)
- **Shared**: Shared models and contracts
- **Database**: EF Core with SQL Server
- **Observability**: Datadog tracing

The architecture emphasizes:

- Clear service boundaries
- Consolidated algorithms
- Internal decomposition before public API changes
- Testability across client and server

---

# API Architecture

## Article Hierarchy

All ancestor walking and breadcrumb logic is centralized in:

IArticleHierarchyService
ArticleHierarchyService


Responsibilities:

- Single parent-walk algorithm
- Cycle detection
- Public vs authenticated context support
- Virtual group resolution
- Breadcrumb and display path generation

Previously duplicated logic across:

- ArticleService
- PublicWorldService
- SearchController
- ArticlesController
- WorldsController

Approximately 340 lines of duplicated logic were consolidated into a single tested service.

Test coverage: 17 dedicated unit tests.

---

## External Links

External provider interactions are centralized in:



IExternalLinkService
ExternalLinkService


This service replaced:

- ExternalLinkSuggestionService
- ExternalLinkContentService
- ExternalLinkValidationService

Design decisions:

- Interface-based for testability
- Centralized cache key construction
- Preserved TTLs:
  - Suggestions: 2 minutes
  - Content: 5 minutes
- World-provider enablement validation unified

Test coverage: 23 unit tests.

Legacy wrapper classes remain temporarily on disk but are no longer used.

---

## World Service Structure

WorldService now retains only core CRUD responsibilities.

Membership and sharing concerns are delegated to specialized services:

- IWorldMembershipService
- IWorldInvitationService
- IPublicSharingService

30 new tests were added covering membership and invitation flows.

No controller contracts changed.

---

# Client Architecture

## Tree State

The original TreeStateService monolith was decomposed into:



TreeNodeIndex
TreeDataBuilder
TreeUiState
TreeMutations
TreeStateService (facade)


Public interface:



ITreeStateService


Remains unchanged.

65 new client-side unit tests were added.

This reduces blast radius and improves maintainability without altering behavior.

---

## Client Composition Root

Program.cs was reduced from 323 lines to 31 lines.

Configuration extracted into:

- ThemeConfig
- AuthenticationServiceExtensions
- MudBlazorServiceExtensions
- HttpClientServiceExtensions
- ApplicationServiceExtensions
- ServiceCollectionExtensions

This establishes a repeatable, type-safe service registration pattern.

---

# Database

The root `/Migrations` directory is canonical.

`ChronicisDbContextModelSnapshot.cs` is authoritative.

Legacy `Data/Migrations` files were archived to:



docs/legacy/migrations-data/


No schema changes occurred during canonicalization.

---

# Testing

Server:

- Article hierarchy: 17 tests
- External links: 23 tests
- World membership and invitations: 30 tests

Client:

- Tree state components: 65 tests

All tests pass.

---

# Design Principles

- Delete before refactor
- Consolidate algorithms, not features
- Strangle large services internally
- Preserve public contracts
- Prefer clarity over cleverness