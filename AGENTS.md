# AGENTS.md

This repository contains Chronicis, including a Blazor client and an ASP.NET API.

Goal for agents: make high confidence changes that compile, pass tests, and preserve product behavior unless a change is explicitly requested.

If you are unsure, stop and propose a plan with file paths before making broad changes.

## Repo layout (high level)

- `Chronicis.Client/`
  - Blazor client using MudBlazor components (MudTreeView) to display documents.
- `Chronicis.Api/` (OR UPDATE TO YOUR ACTUAL API PROJECT NAME)
  - ASP.NET API that the client calls for data and actions.
- `Chronicis.Shared/`
  - Domain models and infrastructure adapters (storage, external services).
- `Chronicis.CaptureApp/`
  - A WinForms app that should be ignored.

If project names differ, discover them by scanning the solution and update this file.

## Authentication
The API must enforce authentication using Auth0, and bearer tokens.

## Current problem statement

Today the client downloads documents directly from Azure Blob Storage. This leaks implementation details and pushes storage concerns into the UI.

We want to introduce an API proxy endpoint so the client never needs blob URLs, container names, SAS tokens, or blob SDK calls.

## Target behavior (acceptance criteria)

- The client requests document content from the API using a `DocumentId` or other stable identifier.
- The API performs authorization checks and then fetches the content from storage.
- The client does not contain any Azure Blob Storage implementation details.
- The solution builds successfully and tests pass.
- Any new endpoint returns correct `Content-Type` and a sensible file name via `Content-Disposition`.

## Approach and architectural rules

### Preferred API contract
Implement at least one of these:

1. Stream-through endpoint (default)
   - `GET /api/documents/{id}/content`
   - API returns the file content as a streamed response.

2. Optional scalable alternative (only if requested or strongly justified)
   - `GET /api/documents/{id}/download-url`
   - API returns a short-lived, read-only URL (for example SAS) and expiration time.

Do not change the public contract once implemented without updating all call sites.

### Storage abstraction
Azure Blob Storage must be behind an interface.

- Create an interface like `IDocumentContentStore` or similar.
- Provide an implementation `AzureBlobDocumentContentStore`.
- The API layer calls the interface, not Azure SDK directly.

### Authorization
The API must enforce document access rules based on the current user and the document metadata.

- Never accept blob paths, container names, or blob identifiers from the client.
- The client supplies only a document identifier (like `DocumentId`).
- Resolve storage keys only on the server.

## What to avoid

- Do not add Azure Blob SDK usage to `Chronicis.Client`.
- Do not embed blob URLs, container names, or storage account names in client configuration.
- Do not introduce large refactors unrelated to the requested change.
- Do not change naming, formatting, or code style across unrelated files.
- Do not introduce new frameworks unless necessary.
- Do not remove existing behavior unless required by acceptance criteria.

## Coding standards

- Follow existing patterns in the repo for controllers vs minimal APIs, DI registration, logging, and error handling.
- Keep changes small and reviewable.
- All newly written code should be SOLID and adhere to clean code conventions. If you encounter existing code which compromises the creation of new code, you should tell me and prompt the best path forward.