import { runProcess } from "../process-utils.mjs";
import { REPO_ROOT } from "../paths.mjs";

export async function hasTrackedChanges() {
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

  return Boolean(result.stdout.trim());
}

export async function commitAllChanges(message) {
  const addResult = await runProcess(
    "git",
    ["add", "-A"],
    {
      cwd: REPO_ROOT,
      captureOutput: true,
      shell: false,
    }
  );

  if (addResult.code !== 0) {
    throw new Error(`Failed to stage changes. ${addResult.stderr}`);
  }

  const commitResult = await runProcess(
    "git",
    ["commit", "-m", message],
    {
      cwd: REPO_ROOT,
      captureOutput: true,
      shell: false,
    }
  );

  if (commitResult.code !== 0) {
    throw new Error(`Failed to create git commit. ${commitResult.stderr}`);
  }
}
