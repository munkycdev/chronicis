import path from "path";
import { ensureDir, fileExists, readText, writeText } from "../fs-utils.mjs";
import { setTerminalTitle, timestamp } from "../console-ui.mjs";
import { getFuturePhaseFileNames, getPhaseStatePaths } from "../phase-files.mjs";
import { createAndApprovePlan } from "../planning/plan-review-loop.mjs";
import { buildCodexImplementationTask } from "../prompts/implementation-prompts.mjs";
import { runCodex } from "../codex-cli.mjs";
import { runVerifyWithRepairs } from "./repair-loop.mjs";
import { COMMIT_AFTER_EACH_PHASE } from "../config.mjs";
import { hasTrackedChanges, commitAllChanges } from "../git/git-commits.mjs";
import { logStep } from "../console-ui.mjs";

function parseSummaryField(summary, label) {
  const match = summary.match(new RegExp(`^${label}: (.+)$`, "m"));
  return match ? match[1].trim() : null;
}

export async function executePhase(phasePath, allPhasePaths, architecture, frozenAssumptions) {
  const phaseFileName = path.basename(phasePath);
  const phaseName = phaseFileName.replace(/\.md$/i, "");
  const futurePhaseNames = getFuturePhaseFileNames(phasePath, allPhasePaths);
  const paths = getPhaseStatePaths(phaseFileName);

  ensureDir(paths.dir);

  // If this phase previously passed, skip it.
  if (fileExists(paths.summary)) {
    const summary = readText(paths.summary);
    if (parseSummaryField(summary, "Status") === "PASSED") {
      console.log(`\n=== Phase ${phaseName}: already passed — skipping ===\n`);
      return {
        phaseName,
        passed: true,
        reviewRoundsUsed: parseInt(parseSummaryField(summary, "Plan review rounds used") ?? "0", 10),
        attempts: parseInt(parseSummaryField(summary, "Repair attempts used") ?? "0", 10),
      };
    }
    console.log(`    Phase ${phaseName} previously failed — resuming...`);
  }

  console.clear();
  setTerminalTitle(`AIP - Running ${phaseName}`);
  console.log(`
=== Phase ${phaseName}: start ===`);

  const phaseSpec = readText(phasePath);

  // If an approved plan already exists from a prior (interrupted) run, skip planning.
  let planResult;
  if (fileExists(paths.approvedPlan)) {
    logStep("Approved plan found — skipping planning stage...");
    planResult = {
      planningBrief: fileExists(paths.planningBrief) ? readText(paths.planningBrief) : "",
      approvedPlan: readText(paths.approvedPlan),
      reviewRoundsUsed: 0,
      settledDecisions: fileExists(paths.settledDecisions) ? readText(paths.settledDecisions) : "",
    };
  } else {
    planResult = await createAndApprovePlan(
      phaseSpec,
      architecture,
      frozenAssumptions,
      phaseFileName,
      futurePhaseNames,
      paths
    );
  }

  const codexTask = buildCodexImplementationTask(
    phaseSpec,
    architecture,
    frozenAssumptions,
    planResult.approvedPlan,
    phaseFileName,
    futurePhaseNames
  );
  writeText(paths.task, codexTask);

  setTerminalTitle(`AIP - ${phaseName} - Codex implementation`);
  console.log("    Running Codex implementation...");
  await runCodex(codexTask);

  const repairResult = await runVerifyWithRepairs({
    phaseFileName,
    phaseName,
    architecture,
    frozenAssumptions,
    phaseSpec,
    approvedPlan: planResult.approvedPlan,
    futurePhaseNames,
    verifyLogPath: paths.verifyLog,
  });

  const phaseSummary = `
# ${phaseName}

Status: ${repairResult.passed ? "PASSED" : "FAILED"}
Planning brief file: ${paths.planningBrief}
Approved plan file: ${paths.approvedPlan}
Settled decisions file: ${paths.settledDecisions}
Implementation task file: ${paths.task}
Verify log: ${paths.verifyLog}
Plan review rounds used: ${planResult.reviewRoundsUsed}
Repair attempts used: ${repairResult.attempts}
Future phases excluded:
${
  futurePhaseNames.length > 0
    ? futurePhaseNames.map((name) => `- ${name}`).join("\n")
    : "(none)"
}
Generated at: ${timestamp()}
`.trim();

  writeText(paths.summary, phaseSummary);

  if (!repairResult.passed) {
    throw new Error(
      `Phase ${phaseName} failed verification after ${repairResult.attempts} repair attempts.`
    );
  }

  console.log(`=== Phase ${phaseName}: passed ===`);
  console.log("");

  if (COMMIT_AFTER_EACH_PHASE) {
    const changed = await hasTrackedChanges();

    if (changed) {
        logStep(`Creating commit for successful phase ${phaseName}...`);
        await commitAllChanges(`ai: complete ${phaseName}`);
    } else {
        logStep(`No changes detected after ${phaseName}; skipping commit.`);
    }
}

  return {
    phaseName,
    passed: true,
    reviewRoundsUsed: planResult.reviewRoundsUsed,
    attempts: repairResult.attempts,
  };
}
