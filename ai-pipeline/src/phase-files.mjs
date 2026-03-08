import fs from "fs";
import path from "path";
import {
  ARCH_PATH,
  FROZEN_ASSUMPTIONS_PATH,
  PHASES_DIR,
  STATE_DIR,
} from "./paths.mjs";
import { fileExists, readText } from "./fs-utils.mjs";

export function listPhaseFiles() {
  if (!fileExists(PHASES_DIR)) {
    throw new Error(`Phases directory not found: ${PHASES_DIR}`);
  }

  return fs
    .readdirSync(PHASES_DIR)
    .filter((name) => name.toLowerCase().endsWith(".md") && !name.startsWith("_"))
    .sort((a, b) => a.localeCompare(b, undefined, { numeric: true }))
    .map((name) => path.join(PHASES_DIR, name));
}

export function getArchitectureText() {
  if (!fileExists(ARCH_PATH)) {
    return "";
  }

  return readText(ARCH_PATH).trim();
}

export function getFrozenAssumptionsText() {
  if (!fileExists(FROZEN_ASSUMPTIONS_PATH)) {
    return "";
  }

  return readText(FROZEN_ASSUMPTIONS_PATH).trim();
}

export function getFuturePhaseFileNames(currentPhasePath, allPhasePaths) {
  const currentIndex = allPhasePaths.findIndex((p) => p === currentPhasePath);

  if (currentIndex < 0) {
    return [];
  }

  return allPhasePaths.slice(currentIndex + 1).map((p) => path.basename(p));
}

export function getCodexPlanDraftPath(phaseDir, roundNumber) {
  return path.join(phaseDir, `codex-plan-v${roundNumber}.md`);
}

export function getCodexPlanReviewPath(phaseDir, roundNumber) {
  return path.join(phaseDir, `codex-plan-review-${roundNumber}.md`);
}

export function futurePhaseBlockText(futurePhaseNames) {
  return futurePhaseNames.length > 0
    ? futurePhaseNames.map((name) => `- ${name}`).join("\n")
    : "(none)";
}

export function getPhaseStatePaths(phaseFileName) {
  const phaseBase = phaseFileName.replace(/\.md$/i, "");
  const phaseStateDir = path.join(STATE_DIR, phaseBase);

  return {
    dir: phaseStateDir,
    planningBrief: path.join(phaseStateDir, "planning-brief.md"),
    approvedPlan: path.join(phaseStateDir, "approved-plan.md"),
    settledDecisions: path.join(phaseStateDir, "settled-decisions.md"),
    task: path.join(phaseStateDir, "codex-task.md"),
    verifyLog: path.join(phaseStateDir, "verify.log"),
    summary: path.join(phaseStateDir, "summary.md"),
  };
}
