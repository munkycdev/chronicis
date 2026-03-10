import path from "path";

export const PIPELINE_DIR = process.cwd();
export const REPO_ROOT = path.resolve(PIPELINE_DIR, "..");
export const PHASES_DIR = path.join(PIPELINE_DIR, "phases");
export const STATE_DIR = path.join(PIPELINE_DIR, "state");
export const ARCH_PATH = path.join(PHASES_DIR, "_architecture.md");
export const FROZEN_ASSUMPTIONS_PATH = path.join(PHASES_DIR, "_frozen-assumptions.md");
export const VERIFY_SCRIPT = path.join(REPO_ROOT, "scripts", "verify.ps1");
export const RUN_SUMMARY_PATH = path.join(STATE_DIR, "run-summary.md");
