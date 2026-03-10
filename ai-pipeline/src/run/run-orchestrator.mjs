import {
  ARCH_PATH,
  FROZEN_ASSUMPTIONS_PATH,
  PHASES_DIR,
  REPO_ROOT,
  RUN_SUMMARY_PATH,
  STATE_DIR,
  VERIFY_SCRIPT,
  PIPELINE_DIR,
} from "../paths.mjs";
import { ensureDir, fileExists, writeText } from "../fs-utils.mjs";
import {
  getArchitectureText,
  getFrozenAssumptionsText,
  listPhaseFiles,
} from "../phase-files.mjs";
import { executePhase } from "../execution/execute-phase.mjs";
import { timestamp, logBanner } from "../console-ui.mjs";
import {
  CREATE_FEATURE_BRANCH,
  COMMIT_AFTER_EACH_PHASE,
  FEATURE_BRANCH_PREFIX,
  FEATURE_NAME,
} from "../config.mjs";

import {
  buildFeatureBranchName,
  ensureGitWorkingTreeIsUsable,
  createAndSwitchToBranch,
  getCurrentBranchName,
} from "../git/git-branching.mjs";


export async function run() {
  try {
    console.clear();

    logBanner("Chronicis AI Phase Orchestrator", [
      `Pipeline dir: ${PIPELINE_DIR}`,
      `Repo root:    ${REPO_ROOT}`,
      `Phases dir:   ${PHASES_DIR}`,
      `State dir:    ${STATE_DIR}`,
    ]);
    console.log("");

    if (!process.env.OPENAI_API_KEY) {
      throw new Error("OPENAI_API_KEY is not set in this shell.");
    }

    if (!fileExists(VERIFY_SCRIPT)) {
      throw new Error(`verify.ps1 not found: ${VERIFY_SCRIPT}`);
    }

    ensureDir(STATE_DIR);

    const phaseFiles = listPhaseFiles();
    if (phaseFiles.length === 0) {
      throw new Error(`No phase markdown files found in ${PHASES_DIR}`);
    }

    const architecture = getArchitectureText();
    const frozenAssumptions = getFrozenAssumptionsText();
    const results = [];

    console.log(`Discovered ${phaseFiles.length} executable phase(s).`);
    if (architecture) {
      console.log("Loaded shared architecture document.");
    } else {
      console.log("No shared architecture document found. Continuing without it.");
    }

    if (frozenAssumptions) {
      console.log("Loaded frozen assumptions document.");
    } else {
      console.log("No frozen assumptions document found. Continuing without it.");
    }
    console.log("");

    let createdBranchName = "";
    const startingBranch = await getCurrentBranchName();

    if (CREATE_FEATURE_BRANCH) {
    await ensureGitWorkingTreeIsUsable();

    createdBranchName = buildFeatureBranchName(
        FEATURE_BRANCH_PREFIX,
        FEATURE_NAME
    );

    console.log(`Creating feature branch: ${createdBranchName}`);
    await createAndSwitchToBranch(createdBranchName);
    }

    for (const phasePath of phaseFiles) {
      const result = await executePhase(
        phasePath,
        phaseFiles,
        architecture,
        frozenAssumptions
      );
      results.push(result);
    }

    const runSummary = `
# Run Summary

Repository root: ${REPO_ROOT}
Phases executed: ${results.length}
Architecture file: ${fileExists(ARCH_PATH) ? ARCH_PATH : "(none)"}
Frozen assumptions file: ${fileExists(FROZEN_ASSUMPTIONS_PATH) ? FROZEN_ASSUMPTIONS_PATH : "(none)"}
Generated at: ${timestamp()}

${results
  .map((r) => {
    return `- ${r.phaseName}: PASSED (plan review rounds: ${r.reviewRoundsUsed}, repair attempts: ${r.attempts})`;
  })
  .join("\n")}
`.trim();

    writeText(RUN_SUMMARY_PATH, runSummary);

    logBanner("All phases passed.", [
      `Run summary written to: ${RUN_SUMMARY_PATH}`,
    ]);
  } catch (err) {
    console.error("");
    console.error("Orchestrator failed:");
    console.error(err);
    process.exit(1);
  }
}
