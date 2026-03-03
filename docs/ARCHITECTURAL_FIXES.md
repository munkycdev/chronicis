# Chronicis Architectural Fixes Plan

Last reviewed: 2026-03-02

## 0) Step 1-10 Execution Status
- Step 1 (`Baseline and Ownership`): completed.
- Step 2 (`Data-Access Policy Definition`): completed.
- Step 3 (`Data-Access Boundary Migration`): completed.
- Step 4 (`Session Model Canonicalization Plan`): completed.
- Step 5 (`Session Convergence Execution`): completed.
- Step 6 (`Unified Access-Policy Architecture`): completed.
- Step 7 (`Read-Model Parity Hardening`): completed.
- Step 8 (`Architecture Test Guardrails`): completed.
- Step 9 (`Rollout and Risk Control`): completed.
- Step 10 (`Closure and Debt Retirement`): completed.
- Verification evidence:
- `dotnet build Chronicis.CI.sln` passes with 0 warnings and 0 errors.
- `dotnet test Chronicis.CI.sln` passes.
- `.\scripts\verify.ps1` passes with 100% line and branch coverage gates.

## 1) Purpose
- Define a practical, phased plan to address current architecture shortcomings without breaking existing product behavior.
- Keep this plan focused on architecture outcomes, sequencing, and validation criteria.

## 2) Shortcomings In Scope
- Inconsistent data-access boundaries in the API (mixed direct `DbContext` usage vs selective abstraction).
- Dual session modeling (first-class `Session` entities plus legacy session-article patterns).
- Split public vs authenticated read-path policy enforcement, which increases duplication and drift risk.

## 3) Constraints and Non-Goals
- Preserve current external API contracts during remediation phases.
- Prefer incremental, low-blast-radius changes over broad rewrites.
- Keep `Chronicis.Shared` as the contract/domain kernel and avoid circular dependency pressure.
- Do not change behavior unless a phase explicitly includes a behavior change and acceptance criteria.

## 4) Step-by-Step Plan

### Step 1: Baseline and Ownership
- Status: completed.
- Domain ownership is defined in `docs/ARCHITECTURE.md` Section `10.1`.
- Baseline architecture map and dependency seams are defined in `docs/ARCHITECTURE.md` Sections `10.5` and `10.6`.
- Success metrics and release gates are defined in `docs/ARCHITECTURE.md` Section `10.8`.

### Step 2: Data-Access Policy Definition
- Status: completed.
- API data-access policy matrix is published in `docs/ARCHITECTURE.md` Section `10.2`.
- Classification decision tree is published in `docs/ARCHITECTURE.md` Section `10.3`.
- Exception rules and active ledger status are published in `docs/ARCHITECTURE.md` Sections `10.4` and `10.10`.
- Current-state inventory and measurable counts are published in `docs/ARCHITECTURE.md` Section `10.6`.
- Domain execution model is published in `docs/ARCHITECTURE.md` Section `10.9`.

Step 2 deliverables:
- Architecture policy matrix and decision tree published in `docs/ARCHITECTURE.md`.
- Initial exception ledger with no unowned exceptions. `Status: complete` (no active exceptions).
- Current-state classification inventory for all direct `DbContext` injections.
- Ordered Step 3 migration queue.

Step 2 acceptance criteria:
- New API code can be classified to one policy type without ambiguity.
- Every non-conforming path has either a migration target or a time-bounded exception.
- Controller `DbContext` usage is fully inventoried with explicit migration ownership.
- `.\scripts\verify.ps1` passes after all Step 2 documentation updates. `Status: complete`.

### Step 3: Data-Access Boundary Migration
- Status: completed.
- Controller-level `DbContext` injection has been removed (`0` remaining in `src/Chronicis.Api/Controllers`).
- Domain boundaries are enforced through service interfaces and dedicated access/read services.
- Migration waves and implementation status are recorded in `docs/ARCHITECTURE.md` Section `10.11`.
- Policy conformance and coverage are enforced through `scripts/verify.ps1`.

Step 3 deliverables:
- Completed migration checklist for every slice in Wave 1 to Wave 3.
- Updated exception ledger after each merged slice. `Status: complete` (no active exceptions).
- Domain-by-domain conformance report showing reduced non-conforming paths.
- Updated Step 3 migration queue status (`pending`, `in_progress`, `completed`) per domain. `Status: complete` (all listed domains marked `completed`).

Step 3 acceptance criteria:
- No new controller-level `DbContext` usage introduced in any migrated slice.
- Every migrated slice is independently revertible.
- Non-conforming direct data-access paths decrease release-over-release.
- All Step 3 slices merge only with passing `.\scripts\verify.ps1`. `Status: complete`.

### Step 4: Session Model Canonicalization Plan
- Status: completed.
- Declare a canonical session model for future development and a compatibility strategy for legacy session-article flows.
- Define transition states: coexistence, compatibility-only, and retirement.
- Freeze new architectural debt by preventing expansion of the non-canonical path.
- Canonical session governance and compatibility boundary are published in `docs/ARCHITECTURE.md` Section `10.12`.
- Baseline session-model inventory is updated in `docs/ARCHITECTURE.md` Section `10.6` and repeatable measurements in Section `10.7`.
- Architectural guardrails are enforced in `tests/Chronicis.ArchitecturalTests/SessionModelGuardrailTests.cs`.

Step 4 deliverables:
- Canonical session model declaration for new development (`Session` entity as canonical).
- Explicit compatibility boundary allowlist for legacy `ArticleType.Session` behavior.
- Transition-state model with entry/exit criteria (`coexistence`, `compatibility-only`, `retirement`).
- Architecture tests that block expansion of the non-canonical session path.
- Updated architecture/feature/changelog documentation to reflect canonical vs compatibility-only expectations.

Step 4 acceptance criteria:
- New session workflows are defined as `Session` entity-first with no ambiguity. `Status: complete`.
- Non-canonical legacy session behavior is isolated to approved compatibility boundaries. `Status: complete`.
- Net-new `ArticleType.Session` usage outside compatibility boundaries fails architecture tests. `Status: complete`.
- Step 4 changes merge only with passing `.\scripts\verify.ps1`. `Status: complete`.

### Step 5: Session Convergence Execution
- Status: completed.
- New session workflows are redirected to canonical `Session` entity writes.
- Legacy-dependent public read paths remain compatibility-only and isolated in `PublicWorldService`.
- Transitional legacy write affordances are removed from active client workflows.

Step 5 deliverables:
- Arc tree child-creation path creates `Session` entities rather than `ArticleType.Session` articles.
- Quick-add session workflow creates `Session` entities and navigates to `/session/{id}`.
- Legacy session compatibility handling in public reads is isolated behind dedicated compatibility helper paths in `PublicWorldService`.
- Compatibility allowlist and architecture guardrail baselines are reduced to match retired legacy references.
- Updated architecture/feature/changelog documentation to reflect Step 5 convergence outcomes.

Step 5 acceptance criteria:
- Net-new session writes route through `Session` entity APIs only. `Status: complete`.
- Legacy session-article behavior is read-compatibility only with no new write expansion. `Status: complete`.
- No user-visible regressions in legacy public session-note URL compatibility paths. `Status: complete`.
- Step 5 changes merge only with passing `.\scripts\verify.ps1`. `Status: complete`.

### Step 6: Unified Access-Policy Architecture
- Status: completed.
- Shared read-policy layer implemented as `IReadAccessPolicyService` + `ReadAccessPolicyService`.
- Public and authenticated read services consume shared policy outcomes:
- `PublicWorldService`
- `ArticleService`
- `ArticleDataAccessService`
- `SummaryAccessService`
- `SearchReadService`
- Policy decisions are separated from projection assembly:
- access constraints are composed through policy service filters;
- DTO/tree/path projection logic remains in owning read services.

Step 6 deliverables:
- Shared public-world and public-article policy filters for anonymous reads.
- Shared authenticated world/article/campaign/arc policy filters for member reads.
- Shared tutorial-read policy input for authenticated read paths that support tutorial/system content.
- Updated service wiring in `Program.cs` and migrated read-service consumers.
- Regression tests added for the policy service and read-path integrations.
- Updated architecture/changelog documentation for Step 6 completion and metrics.

Step 6 acceptance criteria:
- One policy evaluation layer is consumed by both public and authenticated read models. `Status: complete`.
- Visibility and membership rules are no longer duplicated inline across migrated read services. `Status: complete`.
- Public and authenticated controller families consume shared policy outcomes through their backing services. `Status: complete`.
- Full repo verification gate passes (`.\scripts\verify.ps1`). `Status: complete`.

### Step 7: Read-Model Parity Hardening
- Status: completed.
- Shared parity seams introduced for read-model stability:
- `ArticleReadModelProjection` for shared `ArticleDto` materialization.
- `ArticleSlugPathResolver` for shared slug-chain path traversal.
- Public and authenticated path reads now consume shared slug-walk behavior:
- `PublicWorldService.GetPublicArticleAsync`
- `ArticleService.TryResolveWorldArticleByPathAsync`
- `ArticleService.TryResolveTutorialArticleByPathAsync`
- Public/auth parity coverage is enforced in:
- `tests/Chronicis.Api.Tests/Services/ReadModelParityTests.cs`
- Intentional divergences are explicitly tracked as test-protected behaviors:
- private owner-only authenticated reads;
- legacy public session compatibility path resolution (retired in Step 10).

Step 7 deliverables:
- Shared `ArticleDto` projection used across public/auth read services for parity-sensitive article detail reads.
- Shared slug-chain resolver used by both public and authenticated path-resolution flows.
- Service-level parity tests for equivalent public/auth visibility outcomes.
- Service-level divergence tests for approved non-equivalent behavior boundaries.
- Updated architecture/changelog/fix-plan documentation for Step 7 completion.

Step 7 acceptance criteria:
- Duplicate parity-sensitive slug-chain path walk logic is consolidated. `Status: complete`.
- Public/auth parity checks cover equivalent visibility constraints in automated tests. `Status: complete`.
- Intentional divergence scenarios are explicit and regression-protected. `Status: complete`.
- Full repo verification gate passes (`.\scripts\verify.ps1`). `Status: complete`.

### Step 8: Architecture Test Guardrails
- Status: completed.
- Added architecture guardrails in:
- `tests/Chronicis.ArchitecturalTests/Step8ArchitectureGuardrailTests.cs`
- Guardrails enforce:
- API controllers must not directly depend on `ChronicisDbContext`.
- key read services must continue injecting `IReadAccessPolicyService`.
- public/auth path reads must continue using shared parity seams (`ArticleSlugPathResolver`, `ArticleReadModelProjection`).
- `SessionService` must not reintroduce legacy `ArticleType.Session` write behavior.
- Added canonical session regression coverage in:
- `tests/Chronicis.Api.Tests/Services/SessionServiceTests.cs`
- Added policy parity regression baseline in Step 7 suite:
- `tests/Chronicis.Api.Tests/Services/ReadModelParityTests.cs`

Step 8 deliverables:
- Architecture test suite that blocks controller-level data-access boundary regressions.
- Architecture test suite that blocks parity-seam regressions in public/auth read paths.
- Regression coverage that enforces canonical `Session` + `SessionNote` creation flow with no legacy `ArticleType.Session` writes.
- Updated architecture/changelog/fix-plan documentation for Step 8 completion.

Step 8 acceptance criteria:
- Data-access boundary rules are enforced by automated architecture tests. `Status: complete`.
- Canonical session flow regressions fail test gates. `Status: complete`.
- Policy parity protections remain under automated regression coverage. `Status: complete`.
- Full repo verification gate passes (`.\scripts\verify.ps1`). `Status: complete`.

### Step 9: Rollout and Risk Control
- Status: completed.
- Scoped rollout controls defined and documented:
- infrastructure revision/traffic controls with staged progression (`10%`, `50%`, `100%`);
- checkpoint control via `scripts/rollout-checkpoint.ps1`.
- Operational indicator capture is documented and standardized:
- error rate, p95 latency, authorization denials, and data consistency delta.
- Stage checkpoint runbook and execution artifacts are documented in:
- `docs/ROLLOUT_RUNBOOK.md`
- `docs/OBSERVABILITY.md`
- Checkpoint script and sample config added:
- `scripts/rollout-checkpoint.ps1`
- `scripts/rollout-checkpoint.sample.json`

Step 9 deliverables:
- Documented staged rollout flow with explicit go/no-go checkpoint criteria.
- Repeatable checkpoint script that evaluates readiness/health/operational thresholds and returns proceed/rollback exit codes.
- Operational runbook describing rollback actions and required evidence per stage.
- Updated architecture/changelog/fix-plan documentation for Step 9 completion.

Step 9 acceptance criteria:
- Every architecture workstream can be progressed through staged rollout checkpoints with explicit thresholds. `Status: complete`.
- Rollback criteria are executable and unambiguous via a repeatable checkpoint command. `Status: complete`.
- Operational indicator collection requirements are defined and linked to checkpoint execution. `Status: complete`.
- Full repo verification gate passes (`.\scripts\verify.ps1`). `Status: complete`.

### Step 10: Closure and Debt Retirement
- Status: completed.
- Retired temporary compatibility shims in `PublicWorldService` for legacy session-prefixed public URL resolution.
- Updated architecture baselines/guardrails to post-retirement counts and constraints.
- Recorded post-Step-10 backlog items for remaining non-critical legacy reference cleanup.

Step 10 deliverables:
- Removed `PublicWorldService` compatibility helper paths that resolved legacy `session-slug/note-slug` public URLs.
- Public article-path generation for root `SessionNote` articles now emits canonical note-slug paths.
- Updated parity/regression coverage to enforce legacy session-prefix path retirement in both public and authenticated reads.
- Updated architecture/fix-plan/feature/changelog documentation for the Step 10 baseline.
- Updated architecture guardrail API legacy-reference baseline from `6` to `2`.

Step 10 follow-up backlog (non-blocking):
- Drive remaining API `ArticleType.Session` references from `2` to `0` in a dedicated retirement slice.
- Reduce client allowlisted `ArticleType.Session` references from `8` toward full retirement with UI vocabulary migration.
- Revisit `ArticleType`/DTO naming cleanup after compatibility contract windows close.

Step 10 acceptance criteria:
- Temporary public compatibility shims are removed and no longer resolve legacy session-prefixed note URLs. `Status: complete`.
- Architecture docs reflect post-fix baseline metrics and boundary constraints. `Status: complete`.
- Follow-up backlog items are explicitly captured for remaining non-critical cleanup. `Status: complete`.
- Full repo verification gate passes (`.\scripts\verify.ps1`). `Status: complete`.

### Step 11: Logging Hygiene
- Remove extraneous logging - Information logs eliminated
- Remove debug logging 
- All logging calls us the *Sanitized extension methods

## 5) Recommended Execution Order
1. Step 1 to Step 3 (data-access consistency foundation).
2. Step 4 to Step 5 (session model convergence).
3. Step 6 to Step 7 (policy unification and parity).
4. Step 8 to Step 10 (guardrails, rollout, and cleanup).

## 6) Exit Criteria
- API modules in scope follow the defined data-access policy with no unmanaged exceptions.
- Session workflows use the canonical model, with legacy handling isolated or retired.
- Public and authenticated read paths share one policy decision architecture with parity tests passing.
- Architecture tests and full repo verification continue to pass (`.\scripts\verify.ps1`).
