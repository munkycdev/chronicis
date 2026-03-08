import path from "path";
import { ensureDir, readText, writeText } from "../fs-utils.mjs";
import { setTerminalTitle, timestamp } from "../console-ui.mjs";
import { getFuturePhaseFileNames, getPhaseStatePaths } from "../phase-files.mjs";
import { createAndApprovePlan } from "../planning/plan-review-loop.mjs";
import { buildCodexImplementationTask } from "../prompts/implementation-prompts.mjs";
import { runCodex } from "../codex-cli.mjs";
import { runVerifyWithRepairs } from "./repair-loop.mjs";

export async function executePhase(phasePath, allPhasePaths, architecture, frozenAssumptions) {
  const phaseFileName = path.basename(phasePath);
  const phaseName = phaseFileName.replace(/\.md$/i, "");
  const futurePhaseNames = getFuturePhaseFileNames(phasePath, allPhasePaths);
  const paths = getPhaseStatePaths(phaseFileName);

  ensureDir(paths.dir);

  setTerminalTitle(`AI Pipeline - Running Phase ${phaseName}`);
  console.log(`
=== Phase ${phaseName}: start ===`);

  const phaseSpec = readText(phasePath);

  const planResult = await createAndApprovePlan(
    phaseSpec,
    architecture,
    frozenAssumptions,
    phaseFileName,
    phaseName,
    futurePhaseNames,
    paths
  );

  const codexTask = buildCodexImplementationTask(
    phaseSpec,
    architecture,
    frozenAssumptions,
    planResult.approvedPlan,
    phaseFileName,
    futurePhaseNames
  );
  writeText(paths.task, codexTask);

  setTerminalTitle(`AI Pipeline - ${phaseName} - Codex implementation`);
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

  return {
    phaseName,
    passed: true,
    reviewRoundsUsed: planResult.reviewRoundsUsed,
    attempts: repairResult.attempts,
  };
}
