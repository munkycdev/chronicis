import { runProcess } from "../process-utils.mjs";
import { REPO_ROOT } from "../paths.mjs";

function sanitizeBranchPart(value) {
  return String(value)
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .replace(/-+/g, "-");
}

function getTimestampForBranch() {
  const now = new Date();
  const yyyy = now.getFullYear();
  const mm = String(now.getMonth() + 1).padStart(2, "0");
  const dd = String(now.getDate()).padStart(2, "0");
  const hh = String(now.getHours()).padStart(2, "0");
  const min = String(now.getMinutes()).padStart(2, "0");
  const ss = String(now.getSeconds()).padStart(2, "0");
  return `${yyyy}${mm}${dd}-${hh}${min}${ss}`;
}

export function buildFeatureBranchName(prefix, featureName) {
  const safePrefix = sanitizeBranchPart(prefix || "ai");
  const safeFeature = sanitizeBranchPart(featureName || "feature");
  const timestamp = getTimestampForBranch();
  return `${safePrefix}/${safeFeature}-${timestamp}`;
}

export async function getCurrentBranchName() {
  const result = await runProcess(
    "git",
    ["rev-parse", "--abbrev-ref", "HEAD"],
    {
      cwd: REPO_ROOT,
      captureOutput: true,
      shell: false,
    }
  );

  if (result.code !== 0) {
    throw new Error(`Failed to read current git branch. ${result.stderr}`);
  }

  return result.stdout.trim();
}

export async function ensureGitWorkingTreeIsUsable() {
  const result = await runProcess(
    "git",
    ["status", "--porcelain"],
    {
      cwd: REPO_ROOT,
      captureOutput: true,
      shell: false,
    }
  );

  if (result.code !== 0) {
    throw new Error(`Failed to read git status. ${result.stderr}`);
  }

  if (result.stdout.trim()) {
    throw new Error(
      "Git working tree is not clean. Commit or stash changes before running the orchestrator."
    );
  }
}

export async function createAndSwitchToBranch(branchName) {
  const result = await runProcess(
    "git",
    ["switch", "-c", branchName],
    {
      cwd: REPO_ROOT,
      captureOutput: true,
      shell: false,
    }
  );

  if (result.code !== 0) {
    throw new Error(`Failed to create feature branch '${branchName}'. ${result.stderr}`);
  }
}
