import { futurePhaseBlockText } from "../phase-files.mjs";

export function buildCodexImplementationTask(
  phaseSpec,
  architecture,
  frozenAssumptions,
  approvedPlan,
  phaseFileName,
  futurePhaseNames
) {
  const futurePhaseBlock = futurePhaseBlockText(futurePhaseNames);

  return `
Implement the phase described in ${phaseFileName}.

Follow the phase specification exactly.
Use the approved implementation plan below.

PHASE GUARD
===========
You are only allowed to implement the current phase.
Do NOT implement work intended for future phases.
Do NOT redesign repository-wide systems.
Do NOT reopen frozen assumptions.

Future phases that are explicitly out of scope for this run:
${futurePhaseBlock}

If a tiny cross-phase adjustment is absolutely required for compilation or test stability:
- keep it minimal
- do not complete the future behavior
- do not expose unfinished future functionality
- do not expand scope beyond what is necessary to keep this phase correct

Hard requirements:
- implement only this phase
- preserve shared architecture and system rules
- preserve frozen assumptions
- add or update focused tests
- avoid unrelated refactors
- run .\scripts\verify.ps1
- if verification fails, fix the issues and rerun until it passes
- stop after this phase is complete
- end with a concise summary of changed files, tests, and any remaining risks
- explicitly call out any minimal cross-phase adjustments that were required

FEATURE ARCHITECTURE / SHARED RULES
===================================
${architecture || "(none provided)"}

FROZEN ASSUMPTIONS
==================
${frozenAssumptions || "(none provided)"}

PHASE SPECIFICATION
===================
${phaseSpec}

APPROVED IMPLEMENTATION PLAN
============================
${approvedPlan}
`.trim();
}

export function buildRepairPrompt(
  phaseFileName,
  architecture,
  frozenAssumptions,
  phaseSpec,
  approvedPlan,
  futurePhaseNames,
  verifyResult
) {
  const futurePhaseBlock = futurePhaseBlockText(futurePhaseNames);

  return `
The previous implementation for phase ${phaseFileName} did not pass verification.

Fix the issues shown below and rerun .\scripts\verify.ps1.

Stay within this phase's scope.
Do NOT implement future phases.
Do NOT redesign repository-wide systems.
Do NOT reopen frozen assumptions.

Future phases still out of scope:
${futurePhaseBlock}

If a tiny cross-phase adjustment is absolutely required for compilation or test stability:
- keep it minimal
- do not complete future behavior
- do not expose unfinished future functionality

FEATURE ARCHITECTURE / SHARED RULES
===================================
${architecture || "(none provided)"}

FROZEN ASSUMPTIONS
==================
${frozenAssumptions || "(none provided)"}

PHASE SPECIFICATION
===================
${phaseSpec}

APPROVED IMPLEMENTATION PLAN
============================
${approvedPlan}

VERIFY STDOUT
=============
${verifyResult.stdout}

VERIFY STDERR
=============
${verifyResult.stderr}
`.trim();
}
