# Modular AI Pipeline Orchestrator

This package is a refactoring of a single-file `orchestrator.mjs` into smaller modules organized by responsibility.

This version adds a stricter, more practical planning and review loop:

- feature-local architecture is loaded from `_architecture.md`
- frozen repo-level assumptions are loaded from `_frozen-assumptions.md`
- ChatGPT reviews Codex plans for **blocking issues only**
- settled decisions are carried forward across review rounds
- plans are approved once they are viable, not philosophically perfect

## Layout

- `orchestrator.mjs` - tiny entrypoint
- `src/config.mjs` - configuration constants
- `src/paths.mjs` - important repo and pipeline paths
- `src/fs-utils.mjs` - filesystem helpers
- `src/console-ui.mjs` - terminal title and console helpers
- `src/process-utils.mjs` - child process runner and ANSI stripping
- `src/openai-client.mjs` - OpenAI Responses API wrapper
- `src/codex-cli.mjs` - Codex CLI integration
- `src/phase-files.mjs` - phase discovery and state-file helpers
- `src/prompts/` - prompt builders
- `src/planning/` - planning brief and plan approval loop
- `src/execution/` - phase execution, verify, and repair
- `src/run/run-orchestrator.mjs` - top-level orchestration flow

## Expected placement

Place these files under `ai-pipeline/` in your repository so the relative paths continue to work:

- phases expected at `ai-pipeline/phases/`
- state written to `ai-pipeline/state/`
- verify script expected at `../scripts/verify.ps1`

## Required phase support files

Put these in `ai-pipeline/phases/`:

- `_architecture.md` - feature-local architecture and rules
- `_frozen-assumptions.md` - repo-level assumptions that must not be re-adjudicated during narrow feature phases
- numbered phase files like `01-...md`, `02-...md`, etc.

A starter `_frozen-assumptions.md` is included in this package.

## Entry point

Run:

```powershell
node orchestrator.mjs
```
