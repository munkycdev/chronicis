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

# Inline Image Architecture

Inline images use the existing WorldDocument/BlobStorage infrastructure with an article-level association.

## Storage Model

WorldDocument has a nullable ArticleId FK. Documents with ArticleId set are inline images; documents without are standalone uploads. This distinction drives filtering in the tree view (inline images excluded from External Resources) and cleanup on article deletion.

## Reference Format

Article HTML stores `chronicis-image:{documentId}` as the img src. This is a stable, non-expiring reference. On editor initialization, `resolveEditorImages()` walks the DOM, finds these references, and calls back to Blazor to resolve each to a fresh SAS URL via the existing `DownloadDocumentAsync` API. Resolved URLs are cached in-memory per session.

## Upload Flow

1. JS validates file type and size client-side
2. JS calls Blazor `OnImageUploadRequested` → `WorldApi.RequestDocumentUploadAsync` (with ArticleId)
3. JS uploads bytes directly to blob storage via SAS URL
4. JS calls Blazor `OnImageUploadConfirmed` → `WorldApi.ConfirmDocumentUploadAsync`
5. TipTap `setImage` command inserts the `chronicis-image:{documentId}` node
6. `resolveEditorImages` immediately resolves to SAS URL for display

## Cleanup

`IWorldDocumentService.DeleteArticleImagesAsync(articleId)` deletes all blobs and DB records for an article's images. Called from `ArticlesController.DeleteArticleAndDescendantsAsync` during recursive article deletion.

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