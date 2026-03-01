# Chronicis Architectural Fixes Plan

Last reviewed: 2026-03-01

## 0) Step 1-4 Execution Status
- Step 1 (`Baseline and Ownership`): completed.
- Step 2 (`Data-Access Policy Definition`): completed.
- Step 3 (`Data-Access Boundary Migration`): completed.
- Step 4 (`Session Model Canonicalization Plan`): completed.
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
- Create one shared policy evaluation layer used by both public and authenticated read models.
- Separate policy decisions from projection assembly so visibility rules are enforced once.
- Require both controller families to consume the same policy outcomes.

### Step 7: Read-Model Parity Hardening
- Consolidate duplicate rule logic into shared projections or shared policy inputs.
- Add parity checks that compare public/auth behavior under equivalent visibility constraints.
- Track and resolve divergences as release blockers.

### Step 8: Architecture Test Guardrails
- Add architectural tests that enforce the new data-access boundary rules.
- Add regression tests that enforce canonical session flow usage.
- Add policy parity tests that protect public/auth access-rule equivalence.

### Step 9: Rollout and Risk Control
- Release each workstream behind scoped rollout controls where needed.
- Monitor operational indicators (error rates, latency, authorization denials, data consistency signals).
- Use staged rollout checkpoints with explicit rollback criteria.

### Step 10: Closure and Debt Retirement
- Remove temporary compatibility shims once migration exit criteria are met.
- Update architecture docs to reflect the post-fix target state as the new baseline.
- Record follow-up backlog items for remaining non-critical cleanup.

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
