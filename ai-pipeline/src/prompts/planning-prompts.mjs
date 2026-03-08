function futurePhaseBlockText(futurePhaseNames) {
  return futurePhaseNames.length > 0
    ? futurePhaseNames.map((name) => `- ${name}`).join("\n")
    : "(none)";
}

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

Return only these sections:
- Goal
- In-Scope Work
- Frozen Assumptions
- Likely Files
- Required Tests
- Risks
- Phase Guard

Requirements:
- be concise
- stay within the phase
- do not reopen settled repo-level decisions
- do not suggest auth, transport, or infrastructure redesign unless explicitly required by the phase
- keep under 300 words

Current phase file: ${phaseFileName}

Future phases that must NOT be implemented in this phase:
${futurePhaseBlock}

FROZEN ASSUMPTIONS
==================
${frozenAssumptions || "(none provided)"}

FEATURE ARCHITECTURE
====================
${architecture || "(none provided)"}

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

Future phases that are out of scope:
${futurePhaseBlock}

FROZEN ASSUMPTIONS
==================
${frozenAssumptions || "(none provided)"}

FEATURE ARCHITECTURE
====================
${architecture || "(none provided)"}

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
  previousReview,
  settledDecisions,
  roundNumber,
  isFinalRound
) {
  const futurePhaseBlock = futurePhaseBlockText(futurePhaseNames);

  const finalRoundGuidance = isFinalRound
    ? `
FINAL REVIEW ROUND
==================
This is the last review round.

You must prefer approval if the plan is viable and safe to implement.

Only return VERDICT: REVISE if significant changes are absolutely necessary due to one or more true blocking issues:
- phase specification violation
- future-phase scope drift
- likely compile, runtime, or test failure
- clearly missing required tests
- violation of frozen assumptions

Do NOT request revision for:
- optional improvements
- stylistic preferences
- alternative valid approaches
- re-adjudicating settled repository decisions
- minor sequencing preferences

If implementation could proceed safely with the current plan and any remaining concerns can be handled as non-blocking notes, you must approve.
`.trim()
    : "";

  return `
You are reviewing an implementation plan for a single existing-repository phase.

This is NOT a greenfield design review.
Do NOT reopen settled repository-level decisions unless the phase explicitly changes them.

Your job is to find only BLOCKING issues.

A BLOCKING issue is one that:
- violates the phase specification
- violates frozen assumptions
- likely causes compile, runtime, or test failure
- omits clearly required tests
- pulls in future-phase work
- breaks established project patterns that this phase is not allowed to redesign

A NON-BLOCKING issue is:
- style preference
- optional improvement
- alternative design that is not required
- reopening settled repo-wide concerns

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
- Do not ask to redesign authentication, authorization, transport, hosting, or repo-wide patterns unless the phase explicitly changes them
- Prefer approval when the plan is viable

Review round: ${roundNumber}

${finalRoundGuidance}

Current phase file: ${phaseFileName}

Future phases that must NOT be implemented in this phase:
${futurePhaseBlock}

FROZEN ASSUMPTIONS
==================
${frozenAssumptions || "(none provided)"}

FEATURE ARCHITECTURE
====================
${architecture || "(none provided)"}

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
  settledDecisions
) {
  const futurePhaseBlock = futurePhaseBlockText(futurePhaseNames);

  return `
Revise the implementation plan for ${phaseFileName}.

You are planning only.
Do NOT implement code.
Do NOT edit files.
Do NOT include commentary outside the final plan.
Do NOT reopen settled repository decisions.

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
- address the BLOCKING issues raised in the review
- do not churn valid prior decisions
- obey phase boundaries strictly
- do not implement future phases
- avoid unrelated refactors
- keep the plan concrete enough for direct execution

Future phases that remain out of scope:
${futurePhaseBlock}

FROZEN ASSUMPTIONS
==================
${frozenAssumptions || "(none provided)"}

FEATURE ARCHITECTURE
====================
${architecture || "(none provided)"}

PLANNING BRIEF
==============
${planningBrief}

PHASE SPECIFICATION
===================
${phaseSpec}

CURRENT IMPLEMENTATION PLAN
===========================
${currentPlan}

SETTLED DECISIONS
=================
${settledDecisions || "(none)"}

REVIEW TO ADDRESS
=================
${reviewText}
`.trim();
}
