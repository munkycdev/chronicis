import { futurePhaseBlockText } from "../phase-files.mjs";

export function buildPlanningBriefPrompt(
  phaseSpec,
  architecture,
  frozenAssumptions,
  phaseFileName,
  futurePhaseNames
) {
  const futurePhaseBlock = futurePhaseBlockText(futurePhaseNames);

  return `
You are preparing a planning brief for a single implementation phase in an existing repository.

This is not a redesign exercise.
Do not reopen settled repository-wide decisions.

Return only these sections:
- Goal
- In-Scope Work
- Frozen Assumptions
- Likely Files
- Required Tests
- Risks
- Phase Guard

Requirements:
- stay within the current phase
- do not suggest future-phase work
- do not suggest auth, authorization, transport, hosting, or repo-wide redesign unless the phase explicitly changes it
- keep the output under 300 words
- be concise and concrete

Current phase file: ${phaseFileName}

Future phases that must NOT be implemented in this phase:
${futurePhaseBlock}

FEATURE ARCHITECTURE / SHARED RULES
===================================
${architecture || "(none provided)"}

FROZEN ASSUMPTIONS
==================
${frozenAssumptions || "(none provided)"}

PHASE SPECIFICATION
===================
${phaseSpec}
`.trim();
}

export function buildCodexPlanDraftPrompt(
  phaseSpec,
  architecture,
  frozenAssumptions,
  planningBrief,
  phaseFileName,
  futurePhaseNames
) {
  const futurePhaseBlock = futurePhaseBlockText(futurePhaseNames);

  return `
Create the implementation plan for ${phaseFileName}.

You are planning only.
Do NOT implement code.
Do NOT edit files.
Do NOT redesign repository-wide systems.

Return only markdown with these sections:
# Implementation Plan
## Files To Change
## Step By Step Changes
## Tests
## Verification
## Risks
## Out Of Scope

Requirements:
- obey phase boundaries strictly
- assume existing authentication, authorization, transport, and repo-wide architecture remain valid unless this phase explicitly changes them
- keep the plan specific enough for direct execution
- avoid unrelated refactors
- do not propose future-phase work
- do not reopen frozen assumptions

Future phases that are out of scope:
${futurePhaseBlock}

FEATURE ARCHITECTURE / SHARED RULES
===================================
${architecture || "(none provided)"}

FROZEN ASSUMPTIONS
==================
${frozenAssumptions || "(none provided)"}

PLANNING BRIEF
==============
${planningBrief}

PHASE SPECIFICATION
===================
${phaseSpec}
`.trim();
}

export function buildPlanReviewPrompt(
  phaseSpec,
  architecture,
  frozenAssumptions,
  planningBrief,
  codexPlan,
  phaseFileName,
  futurePhaseNames,
  previousReview = "",
  settledDecisions = ""
) {
  const futurePhaseBlock = futurePhaseBlockText(futurePhaseNames);

  return `
You are reviewing an implementation plan for a single existing-repository phase.

This is NOT a greenfield design review.
Do NOT reopen settled repository-level decisions unless the phase explicitly changes them.
Your job is to identify BLOCKING issues only.

A BLOCKING issue is one that:
- violates the phase specification
- violates frozen assumptions
- likely causes compile, runtime, or test failure
- omits clearly required tests
- pulls in future-phase work
- breaks established project patterns that this phase is not allowed to redesign

A NON-BLOCKING issue is one that:
- is a style preference
- is an optional improvement
- suggests a different but still viable design
- reopens settled repository-wide concerns

If there are no BLOCKING issues, you MUST approve.

Return only markdown in exactly this structure:

VERDICT: APPROVED
or
VERDICT: REVISE

# Review
## Blocking Issues
- [BLOCKER] ...
## Non-Blocking Notes
- [NON-BLOCKING] ...
## Required Changes
- ...
## Settled Decisions
- ...

Rules:
- If there are zero BLOCKING issues, use VERDICT: APPROVED
- Do not ask for future-phase work
- Do not ask to redesign authentication, authorization, transport, hosting, or other repo-wide patterns unless the phase explicitly changes them
- Prefer approval when the plan is viable
- Keep the review concise and actionable

Current phase file: ${phaseFileName}

Future phases that must NOT be implemented in this phase:
${futurePhaseBlock}

FEATURE ARCHITECTURE / SHARED RULES
===================================
${architecture || "(none provided)"}

FROZEN ASSUMPTIONS
==================
${frozenAssumptions || "(none provided)"}

PLANNING BRIEF
==============
${planningBrief}

PHASE SPECIFICATION
===================
${phaseSpec}

PREVIOUS REVIEW CONTEXT
=======================
${previousReview || "(none)"}

SETTLED DECISIONS
=================
${settledDecisions || "(none)"}

CODEX IMPLEMENTATION PLAN
=========================
${codexPlan}
`.trim();
}

export function buildCodexPlanRevisionPrompt(
  phaseSpec,
  architecture,
  frozenAssumptions,
  planningBrief,
  currentPlan,
  reviewText,
  phaseFileName,
  futurePhaseNames,
  roundNumber,
  settledDecisions = ""
) {
  const futurePhaseBlock = futurePhaseBlockText(futurePhaseNames);

  return `
Revise the implementation plan for ${phaseFileName}.

You are planning only.
Do NOT implement code.
Do NOT edit files.
Do NOT include commentary outside the final plan.
Do NOT redesign repository-wide systems.

Return only markdown with these sections:
# Implementation Plan
## Files To Change
## Step By Step Changes
## Tests
## Verification
## Risks
## Out Of Scope

This is revision round ${roundNumber}.

Requirements:
- address every BLOCKING issue raised in the review
- preserve viable decisions already made
- obey phase boundaries strictly
- do not implement future phases
- avoid unrelated refactors
- keep the plan concrete enough for direct execution
- do not reopen settled decisions

Future phases that remain out of scope:
${futurePhaseBlock}

FEATURE ARCHITECTURE / SHARED RULES
===================================
${architecture || "(none provided)"}

FROZEN ASSUMPTIONS
==================
${frozenAssumptions || "(none provided)"}

PLANNING BRIEF
==============
${planningBrief}

PHASE SPECIFICATION
===================
${phaseSpec}

SETTLED DECISIONS
=================
${settledDecisions || "(none)"}

CURRENT IMPLEMENTATION PLAN
===========================
${currentPlan}

REVIEW TO ADDRESS
=================
${reviewText}
`.trim();
}
