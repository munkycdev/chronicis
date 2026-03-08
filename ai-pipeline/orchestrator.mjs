import OpenAI from "openai";
import fs from "fs";
import path from "path";
import { spawn } from "child_process";

const client = new OpenAI();

const PIPELINE_DIR = process.cwd();
const REPO_ROOT = path.resolve(PIPELINE_DIR, "..");
const PHASES_DIR = path.join(PIPELINE_DIR, "phases");
const STATE_DIR = path.join(PIPELINE_DIR, "state");
const ARCH_PATH = path.join(PHASES_DIR, "_architecture.md");
const VERIFY_SCRIPT = path.join(REPO_ROOT, "scripts", "verify.ps1");
const RUN_SUMMARY_PATH = path.join(STATE_DIR, "run-summary.md");

const MAX_FIX_ATTEMPTS = 3;
const MODEL = "gpt-5.4";

function ensureDir(dirPath) {
  fs.mkdirSync(dirPath, { recursive: true });
}

function fileExists(filePath) {
  return fs.existsSync(filePath);
}

function readText(filePath) {
  return fs.readFileSync(filePath, "utf8");
}

function writeText(filePath, text) {
  ensureDir(path.dirname(filePath));
  fs.writeFileSync(filePath, text, "utf8");
}

function listPhaseFiles() {
  if (!fileExists(PHASES_DIR)) {
    throw new Error(`Phases directory not found: ${PHASES_DIR}`);
  }

  return fs
    .readdirSync(PHASES_DIR)
    .filter((name) => {
      return name.toLowerCase().endsWith(".md") && !name.startsWith("_");
    })
    .sort((a, b) => a.localeCompare(b, undefined, { numeric: true }))
    .map((name) => path.join(PHASES_DIR, name));
}

function getArchitectureText() {
  if (!fileExists(ARCH_PATH)) {
    return "";
  }

  return readText(ARCH_PATH).trim();
}

function getFuturePhaseFileNames(currentPhasePath, allPhasePaths) {
  const currentIndex = allPhasePaths.findIndex((p) => p === currentPhasePath);

  if (currentIndex < 0) {
    return [];
  }

  return allPhasePaths.slice(currentIndex + 1).map((p) => path.basename(p));
}

function getPhaseStatePaths(phaseFileName) {
  const phaseBase = phaseFileName.replace(/\.md$/i, "");
  const phaseStateDir = path.join(STATE_DIR, phaseBase);

  return {
    dir: phaseStateDir,
    plan: path.join(phaseStateDir, "plan.md"),
    critique: path.join(phaseStateDir, "critique.md"),
    task: path.join(phaseStateDir, "codex-task.md"),
    verifyLog: path.join(phaseStateDir, "verify.log"),
    summary: path.join(phaseStateDir, "summary.md"),
  };
}

async function askModel(prompt) {
  const response = await client.responses.create({
    model: MODEL,
    input: prompt,
  });

  const text = response.output_text?.trim();
  if (!text) {
    console.dir(response, { depth: 8 });
    throw new Error("Model returned no output_text.");
  }

  return text;
}

function getCodexInvocationArgs(baseArgs) {
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

function runProcess(command, args, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      cwd: options.cwd ?? PIPELINE_DIR,
      shell: options.shell ?? false,
      stdio: options.captureOutput
        ? ["pipe", "pipe", "pipe"]
        : ["pipe", "inherit", "inherit"],
      windowsHide: true,
    });

    let stdout = "";
    let stderr = "";

    if (options.stdinText && child.stdin) {
      child.stdin.write(options.stdinText);
    }

    if (child.stdin) {
      child.stdin.end();
    }

    if (options.captureOutput) {
      if (child.stdout) {
        child.stdout.on("data", (data) => {
          stdout += data.toString();
        });
      }

      if (child.stderr) {
        child.stderr.on("data", (data) => {
          stderr += data.toString();
        });
      }
    }

    child.on("error", reject);

    child.on("close", (code) => {
      resolve({ code, stdout, stderr });
    });
  });
}

function getCodexCommand() {
  if (process.platform === "win32") {
    return "codex.cmd";
  }

  return "codex";
}

async function runCodex(taskText) {

  const invocation = getCodexInvocationArgs([
    "exec",
    "--cd",
    REPO_ROOT,
    "--full-auto",
    "-"
  ]);

  const result = await runProcess(
    invocation.command,
    invocation.args,
    {
      cwd: PIPELINE_DIR,
      stdinText: taskText,
      captureOutput: false,
      shell: invocation.shell,
    }
  );

  if (result.code !== 0) {
    throw new Error(`Codex exec failed with exit code ${result.code}`);
  }
}

async function resumeCodex(repairPrompt) {

  const invocation = getCodexInvocationArgs([
    "exec",
    "resume",
    "--last",
    "--cd",
    REPO_ROOT,
    "--full-auto",
    "-"
  ]);

  const result = await runProcess(
    invocation.command,
    invocation.args,
    {
      cwd: PIPELINE_DIR,
      stdinText: repairPrompt,
      captureOutput: false,
      shell: invocation.shell,
    }
  );

  if (result.code !== 0) {
    throw new Error(`Codex resume failed with exit code ${result.code}`);
  }
}

async function runVerify() {
  const result = await runProcess(
    "powershell",
    ["-ExecutionPolicy", "Bypass", "-File", VERIFY_SCRIPT],
    {
      cwd: REPO_ROOT,
      captureOutput: true,
    }
  );

  return result;
}

async function generatePlan(phaseSpec, architecture, phaseFileName, futurePhaseNames) {
  const futurePhaseBlock =
    futurePhaseNames.length > 0
      ? futurePhaseNames.map((name) => `- ${name}`).join("\n")
      : "(none)";

  return await askModel(`
You are a senior software architect.

Produce a detailed implementation plan for this phase only.

Requirements:
- obey phase boundaries strictly
- do not pull work from later phases
- identify likely files to change
- identify tests to add or update
- include verification expectations
- call out risks and assumptions

Current phase file: ${phaseFileName}

Future phases that must NOT be implemented in this phase:
${futurePhaseBlock}

GLOBAL ARCHITECTURE / SHARED RULES
==================================
${architecture || "(none provided)"}

PHASE SPECIFICATION
===================
${phaseSpec}
`.trim());
}

async function generateCritique(
  phaseSpec,
  architecture,
  plan,
  phaseFileName,
  futurePhaseNames
) {
  const futurePhaseBlock =
    futurePhaseNames.length > 0
      ? futurePhaseNames.map((name) => `- ${name}`).join("\n")
      : "(none)";

  return await askModel(`
You are a strict staff engineer reviewing an implementation plan.

Review the plan for this phase only.

Identify:
- architecture issues
- scope drift
- missing edge cases
- test gaps
- sequencing problems
- concrete corrections

Be specific and actionable.

Current phase file: ${phaseFileName}

Future phases that must NOT be implemented in this phase:
${futurePhaseBlock}

GLOBAL ARCHITECTURE / SHARED RULES
==================================
${architecture || "(none provided)"}

PHASE SPECIFICATION
===================
${phaseSpec}

IMPLEMENTATION PLAN
===================
${plan}
`.trim());
}

function buildCodexTask(
  phaseSpec,
  architecture,
  plan,
  critique,
  phaseFileName,
  futurePhaseNames
) {
  const futurePhaseBlock =
    futurePhaseNames.length > 0
      ? futurePhaseNames.map((name) => `- ${name}`).join("\n")
      : "(none)";

  return `
Implement the phase described in ${phaseFileName}.

Follow the phase specification exactly.
Use the implementation plan below.
Address all critique items below.

PHASE GUARD
===========
You are only allowed to implement the current phase.
Do NOT implement work intended for future phases.

Future phases that are explicitly out of scope for this run:
${futurePhaseBlock}

If a tiny cross-phase adjustment is absolutely required for compilation or test stability:
- keep it minimal
- do not complete the future behavior
- do not expose unfinished future functionality
- do not expand scope beyond what is necessary to keep this phase correct

Hard requirements:
- implement only this phase
- preserve shared architecture and system rules
- add or update focused tests
- avoid unrelated refactors
- run .\\scripts\\verify.ps1
- if verification fails, fix the issues and rerun until it passes
- stop after this phase is complete
- end with a concise summary of changed files, tests, and any remaining risks
- explicitly call out any minimal cross-phase adjustments that were required

GLOBAL ARCHITECTURE / SHARED RULES
==================================
${architecture || "(none provided)"}

PHASE SPECIFICATION
===================
${phaseSpec}

IMPLEMENTATION PLAN
===================
${plan}

CRITIQUE
========
${critique}
`.trim();
}

function buildRepairPrompt(
  phaseFileName,
  architecture,
  phaseSpec,
  futurePhaseNames,
  verifyResult
) {
  const futurePhaseBlock =
    futurePhaseNames.length > 0
      ? futurePhaseNames.map((name) => `- ${name}`).join("\n")
      : "(none)";

  return `
The previous implementation for phase ${phaseFileName} did not pass verification.

Fix the issues shown below and rerun .\\scripts\\verify.ps1.

Stay within this phase's scope.
Do NOT implement future phases.

Future phases still out of scope:
${futurePhaseBlock}

If a tiny cross-phase adjustment is absolutely required for compilation or test stability:
- keep it minimal
- do not complete future behavior
- do not expose unfinished future functionality

GLOBAL ARCHITECTURE / SHARED RULES
==================================
${architecture || "(none provided)"}

PHASE SPECIFICATION
===================
${phaseSpec}

VERIFY STDOUT
=============
${verifyResult.stdout}

VERIFY STDERR
=============
${verifyResult.stderr}
`.trim();
}

async function executePhase(phasePath, allPhasePaths, architecture) {
  const phaseFileName = path.basename(phasePath);
  const phaseName = phaseFileName.replace(/\.md$/i, "");
  const futurePhaseNames = getFuturePhaseFileNames(phasePath, allPhasePaths);
  const paths = getPhaseStatePaths(phaseFileName);

  ensureDir(paths.dir);

  console.log(`\n=== Phase ${phaseName}: start ===`);

  const phaseSpec = readText(phasePath);

  console.log(`    Generating implementation plan...`);
  const plan = await generatePlan(
    phaseSpec,
    architecture,
    phaseFileName,
    futurePhaseNames
  );
  writeText(paths.plan, plan);

  console.log(`    Generating critique of the plan...`);
  const critique = await generateCritique(
    phaseSpec,
    architecture,
    plan,
    phaseFileName,
    futurePhaseNames
  );
  writeText(paths.critique, critique);

  const codexTask = buildCodexTask(
    phaseSpec,
    architecture,
    plan,
    critique,
    phaseFileName,
    futurePhaseNames
  );
  writeText(paths.task, codexTask);

  console.log(`    Running Codex implementation...`);
  await runCodex(codexTask);

  console.log(`    Running verification...`);
  let verifyResult = await runVerify();
  writeText(
    paths.verifyLog,
    `${verifyResult.stdout}\n\n--- STDERR ---\n\n${verifyResult.stderr}`
  );

  let attempts = 0;

  while (verifyResult.code !== 0 && attempts < MAX_FIX_ATTEMPTS) {
    attempts += 1;
    console.log(
      `    Verification failed, repair attempt ${attempts}/${MAX_FIX_ATTEMPTS}`
    );

    const repairPrompt = buildRepairPrompt(
      phaseFileName,
      architecture,
      phaseSpec,
      futurePhaseNames,
      verifyResult
    );

    await resumeCodex(repairPrompt);

    console.log(`    Running verification after repair attempt...`);
    verifyResult = await runVerify();
    writeText(
      paths.verifyLog,
      `${verifyResult.stdout}\n\n--- STDERR ---\n\n${verifyResult.stderr}`
    );
  }

  const passed = verifyResult.code === 0;

  const phaseSummary = `
# ${phaseName}

Status: ${passed ? "PASSED" : "FAILED"}
Repair attempts used: ${attempts}
Plan file: ${paths.plan}
Critique file: ${paths.critique}
Task file: ${paths.task}
Verify log: ${paths.verifyLog}
Future phases excluded:
${
  futurePhaseNames.length > 0
    ? futurePhaseNames.map((name) => `- ${name}`).join("\n")
    : "(none)"
}
`.trim();

  writeText(paths.summary, phaseSummary);

  if (!passed) {
    throw new Error(
      `Phase ${phaseName} failed verification after ${MAX_FIX_ATTEMPTS} repair attempts.`
    );
  }

  console.log(`=== Phase ${phaseName}: ${passed ? "passed" : "failed"} ===`);
  console.log('');
  return {
    phaseName,
    passed: true,
    attempts,
  };
}

async function run() {
  try {

    console.clear();
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
    const results = [];

    for (const phasePath of phaseFiles) {
      const result = await executePhase(phasePath, phaseFiles, architecture);
      results.push(result);
    }

    const runSummary = `
# Run Summary

Repository root: ${REPO_ROOT}
Phases executed: ${results.length}
Architecture file: ${fileExists(ARCH_PATH) ? ARCH_PATH : "(none)"}

${results
  .map((r) => `- ${r.phaseName}: PASSED (repair attempts: ${r.attempts})`)
  .join("\n")}
`.trim();

    writeText(RUN_SUMMARY_PATH, runSummary);

    console.log("\nAll phases passed.");
    console.log(`Run summary written to: ${RUN_SUMMARY_PATH}`);
  } catch (err) {
    console.error("\nOrchestrator failed:");
    console.error(err);
    process.exit(1);
  }
}

await run();
