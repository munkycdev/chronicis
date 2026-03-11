import { REPO_ROOT, PIPELINE_DIR } from "./paths.mjs";
import { runProcess, stripAnsi } from "./process-utils.mjs";

export function getCodexInvocationArgs(baseArgs) {
  if (process.platform === "win32") {
    return {
      command: "cmd.exe",
      args: ["/d", "/s", "/c", "codex", ...baseArgs],
      shell: false,
    };
  }

  return {
    command: "codex",
    args: baseArgs,
    shell: false,
  };
}

export async function runCodexPlanPrompt(promptText) {
  const invocation = getCodexInvocationArgs([
    "exec",
    REPO_ROOT,
    "--model",
    "gpt-5.4",
    "-c",
    'model_reasoning_effort="high"',
    "--sandbox",
    "read-only",
    "-"
  ]);

  const result = await runProcess(invocation.command, invocation.args, {
    cwd: PIPELINE_DIR,
    stdinText: promptText,
    captureOutput: true,
    shell: invocation.shell,
  });

  if (result.code !== 0) {
    throw new Error(`Codex planning exec failed with exit code ${result.code}`);
  }

  const stdout = stripAnsi(result.stdout).trim();
  if (!stdout) {
    throw new Error("Codex planning exec returned no stdout.");
  }

  return stdout;
}

export async function runCodex(taskText) {
  const invocation = getCodexInvocationArgs([
    "exec",
    REPO_ROOT,
    "--model",
    "gpt-5.4",
    "-c",
    'model_reasoning_effort="medium"',
    "--full-auto",
    "-"
  ]);

  const result = await runProcess(invocation.command, invocation.args, {
    cwd: PIPELINE_DIR,
    stdinText: taskText,
    captureOutput: false,
    shell: invocation.shell,
  });

  if (result.code !== 0) {
    throw new Error(`Codex exec failed with exit code ${result.code}`);
  }
}

export async function resumeCodex(repairPrompt) {
  const invocation = getCodexInvocationArgs([
    "exec",
    "resume",
    "--last",
    REPO_ROOT,
    "--model",
    "gpt-5.4",
    "-c",
    'model_reasoning_effort="medium"',
    "--full-auto",
    "-"
  ]);

  const result = await runProcess(invocation.command, invocation.args, {
    cwd: PIPELINE_DIR,
    stdinText: repairPrompt,
    captureOutput: false,
    shell: invocation.shell,
  });

  if (result.code !== 0) {
    throw new Error(`Codex resume failed with exit code ${result.code}`);
  }
}
