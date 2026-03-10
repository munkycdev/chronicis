import { writeText, readText } from "../fs-utils.mjs";
import { runCodexPlanPrompt } from "../codex-cli.mjs";
import {
  buildPlanningBriefPrompt,
  buildCodexPlanDraftPrompt,
  buildPlanReviewPrompt,
  buildCodexPlanRevisionPrompt,
} from "../prompts/planning-prompts.mjs";
import { askModel } from "../openai-client.mjs";
import { logStep } from "../console-ui.mjs";
import { MAX_PLAN_REVIEW_ROUNDS } from "../config.mjs";
import {
  getCodexPlanDraftPath,
  getCodexPlanReviewPath,
} from "../phase-files.mjs";

export async function generatePlanningBrief(
  phaseSpec,
  architecture,
  frozenAssumptions,
  phaseFileName,
  futurePhaseNames
) {
  return await askModel(
    buildPlanningBriefPrompt(
      phaseSpec,
      architecture,
      frozenAssumptions,
      phaseFileName,
      futurePhaseNames
    )
  );
}

export async function reviewCodexPlan(
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
  return await askModel(
    buildPlanReviewPrompt(
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
    )
  );
}

export function parseReviewVerdict(reviewText) {
  const normalized = reviewText.toUpperCase();

  if (normalized.includes("VERDICT: APPROVED")) {
    return "APPROVED";
  }

  if (normalized.includes("VERDICT: REVISE")) {
    return "REVISE";
  }

  throw new Error("Could not parse plan review verdict.");
}

function extractSettledDecisions(reviewText) {
  const match = reviewText.match(
    /## Settled Decisions\s*([\s\S]*?)(?:\n## |\n# |$)/i
  );

  if (!match) {
    return "";
  }

  return match[1].trim();
}

export async function createAndApprovePlan(
  phaseSpec,
  architecture,
  frozenAssumptions,
  phaseFileName,
  futurePhaseNames,
  paths
) {
  logStep("Generating planning brief...");
  const planningBrief = await generatePlanningBrief(
    phaseSpec,
    architecture,
    frozenAssumptions,
    phaseFileName,
    futurePhaseNames
  );
  writeText(paths.planningBrief, planningBrief);

  let currentPlan = "";
  let approved = false;
  let reviewRoundsUsed = 0;
  let previousReview = "";
  let settledDecisions = "";

  for (let round = 1; round <= MAX_PLAN_REVIEW_ROUNDS; round += 1) {
    const isFinalRound = round === MAX_PLAN_REVIEW_ROUNDS;

    if (round === 1) {
      logStep(`Requesting Codex implementation plan draft (round ${round})...`);
      currentPlan = await runCodexPlanPrompt(
        buildCodexPlanDraftPrompt(
          phaseSpec,
          architecture,
          frozenAssumptions,
          planningBrief,
          phaseFileName,
          futurePhaseNames
        )
      );
    } else {
      logStep(`Requesting Codex implementation plan revision (round ${round})...`);
      const previousPlan = readText(getCodexPlanDraftPath(paths.dir, round - 1));
      const previousReviewText = readText(
        getCodexPlanReviewPath(paths.dir, round - 1)
      );

      currentPlan = await runCodexPlanPrompt(
        buildCodexPlanRevisionPrompt(
          phaseSpec,
          architecture,
          frozenAssumptions,
          planningBrief,
          previousPlan,
          previousReviewText,
          phaseFileName,
          futurePhaseNames,
          round,
          settledDecisions
        )
      );
    }

    const planPath = getCodexPlanDraftPath(paths.dir, round);
    writeText(planPath, currentPlan);

    logStep(
      `Reviewing Codex plan with ChatGPT (round ${round}${isFinalRound ? ", final" : ""})...`
    );

    const review = await reviewCodexPlan(
      phaseSpec,
      architecture,
      frozenAssumptions,
      planningBrief,
      currentPlan,
      phaseFileName,
      futurePhaseNames,
      previousReview,
      settledDecisions,
      round,
      isFinalRound
    );

    const reviewPath = getCodexPlanReviewPath(paths.dir, round);
    writeText(reviewPath, review);

    previousReview = review;

    const extractedSettledDecisions = extractSettledDecisions(review);
    if (extractedSettledDecisions) {
      settledDecisions = extractedSettledDecisions;
      if (paths.settledDecisions) {
        writeText(paths.settledDecisions, settledDecisions);
      }
    }

    const verdict = parseReviewVerdict(review);
    reviewRoundsUsed = round;

    logStep(`Plan review verdict: ${verdict}`);

    if (verdict === "APPROVED") {
      approved = true;
      writeText(paths.approvedPlan, currentPlan);
      break;
    }
  }

  if (!approved) {
    throw new Error(
      `Plan for ${phaseFileName} was not approved after ${MAX_PLAN_REVIEW_ROUNDS} review rounds.`
    );
  }

  return {
    planningBrief,
    approvedPlan: readText(paths.approvedPlan),
    reviewRoundsUsed,
    settledDecisions,
  };
}
