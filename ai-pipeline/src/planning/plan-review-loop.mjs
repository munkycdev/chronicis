import { MAX_PLAN_REVIEW_ROUNDS } from "../config.mjs";
import { logStep, setTerminalTitle } from "../console-ui.mjs";
import { writeText, readText } from "../fs-utils.mjs";
import { askModel } from "../openai-client.mjs";
import { runCodexPlanPrompt } from "../codex-cli.mjs";
import {
  getCodexPlanDraftPath,
  getCodexPlanReviewPath,
} from "../phase-files.mjs";
import {
  buildCodexPlanDraftPrompt,
  buildPlanReviewPrompt,
  buildCodexPlanRevisionPrompt,
} from "../prompts/planning-prompts.mjs";
import { generatePlanningBrief } from "./planning-brief.mjs";

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

function extractSection(reviewText, heading) {
  const escaped = heading.replace(/[.*+?^${}()|[\]\]]/g, "\$&");
  const regex = new RegExp(`## ${escaped}\n([\s\S]*?)(?=\n## |$)`, "i");
  const match = reviewText.match(regex);
  return match ? match[1].trim() : "";
}

export async function reviewCodexPlan(
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
      settledDecisions
    )
  );
}

export async function createAndApprovePlan(
  phaseSpec,
  architecture,
  frozenAssumptions,
  phaseFileName,
  phaseName,
  futurePhaseNames,
  paths
) {
  setTerminalTitle(`AIP - ${phaseName} - planning brief`);
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
  let settledDecisions = paths.settledDecisions && readTextSafe(paths.settledDecisions);

  for (let round = 1; round <= MAX_PLAN_REVIEW_ROUNDS; round += 1) {
    if (round === 1) {
      setTerminalTitle(`AIP - ${phaseName} - Codex plan draft`);
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
      setTerminalTitle(`AIP - ${phaseName} - Codex plan revision ${round}`);
      logStep(`Requesting Codex implementation plan revision (round ${round})...`);
      const previousPlan = readText(getCodexPlanDraftPath(paths.dir, round - 1));

      currentPlan = await runCodexPlanPrompt(
        buildCodexPlanRevisionPrompt(
          phaseSpec,
          architecture,
          frozenAssumptions,
          planningBrief,
          previousPlan,
          previousReview,
          phaseFileName,
          futurePhaseNames,
          round,
          settledDecisions
        )
      );
    }

    const planPath = getCodexPlanDraftPath(paths.dir, round);
    writeText(planPath, currentPlan);

    setTerminalTitle(`AIP - ${phaseName} - ChatGPT review ${round}`);
    logStep(`Reviewing Codex plan with ChatGPT (round ${round})...`);
    const review = await reviewCodexPlan(
      phaseSpec,
      architecture,
      frozenAssumptions,
      planningBrief,
      currentPlan,
      phaseFileName,
      futurePhaseNames,
      previousReview,
      settledDecisions
    );

    const reviewPath = getCodexPlanReviewPath(paths.dir, round);
    writeText(reviewPath, review);

    previousReview = review;
    const extractedSettledDecisions = extractSection(review, "Settled Decisions");
    if (extractedSettledDecisions) {
      settledDecisions = extractedSettledDecisions;
      writeText(paths.settledDecisions, settledDecisions);
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

function readTextSafe(filePath) {
  try {
    return filePath ? readText(filePath) : "";
  } catch {
    return "";
  }
}
