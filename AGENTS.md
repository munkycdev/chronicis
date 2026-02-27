# AGENTS.md

This repository contains Chronicis, including a Blazor client and an ASP.NET API developed on .NET 9.

Goal for agents: make high confidence changes that compile, pass tests, and preserve product behavior unless a change is explicitly requested.

If you are unsure, stop and propose a plan with file paths before making broad changes.

## Repo layout (high level)

- `/src/Chronicis.Client/`
  - Blazor client using MudBlazor components (MudTreeView) to display documents.
- `/src/Chronicis.Client.Host/`
  - Host for Chronicis.Client
- `/src/Chronicis.Api/`
  - ASP.NET API that the client calls for data and actions.
- `/src/Chronicis.Shared/`
  - Domain models and infrastructure adapters (storage, external services).
- `/src/Chronicis.CaptureApp/`
  - A WinForms app that should be ignored.

If project names differ, discover them by scanning the solution and update this file.

## Authentication
The API must enforce authentication using Auth0, and bearer tokens.

## General Instructions
Before writing code:
 - Always read AGENTS.md before making changes
 - If a slice prompt conflicts with AGENTS.md, the slice prompt wins, but call out the conflict
 - List the exact files you will modify or add.
 - Briefly explain the change plan in 5 to 10 bullets.
 - While writing code:
 - Keep changes minimal and localized.
 - Do not refactor unrelated code.
 - Ensure the solution builds.
 - After writing code:
 - Summarize what changed.
 - Provide a quick manual test path.

All phases should be validated via dotnet format .\Chronicis.CI.sln
All phases should have line and branch test coverage at 100% on Chronicis.CI.sln using the coverlet.runsettings
