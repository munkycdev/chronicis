import { MAX_FIX_ATTEMPTS } from "../config.mjs";
import { logStep, setTerminalTitle } from "../console-ui.mjs";
import { writeText } from "../fs-utils.mjs";
import { resumeCodex } from "../codex-cli.mjs";
import { buildRepairPrompt } from "../prompts/implementation-prompts.mjs";
import { runVerify } from "./verify-runner.mjs";

export async function runVerifyWithRepairs({
  phaseFileName,
  phaseName,
  architecture,
  frozenAssumptions,
  phaseSpec,
  approvedPlan,
  futurePhaseNames,
  verifyLogPath,
}) {
  setTerminalTitle(`AI Pipeline - ${phaseName} - verify`);
  logStep("Running verification...");
  let verifyResult = await runVerify();
  writeText(
    verifyLogPath,
    `${verifyResult.stdout}

--- STDERR ---

${verifyResult.stderr}`
  );

  let attempts = 0;

  while (verifyResult.code !== 0 && attempts < MAX_FIX_ATTEMPTS) {
    attempts += 1;
    setTerminalTitle(`AI Pipeline - ${phaseName} - repair ${attempts}`);
    logStep(`Verification failed, repair attempt ${attempts}/${MAX_FIX_ATTEMPTS}...`);

    const repairPrompt = buildRepairPrompt(
      phaseFileName,
      architecture,
      frozenAssumptions,
      phaseSpec,
      approvedPlan,
      futurePhaseNames,
      verifyResult
    );

    await resumeCodex(repairPrompt);

    setTerminalTitle(`AI Pipeline - ${phaseName} - verify after repair ${attempts}`);
    logStep("Running verification after repair attempt...");
    verifyResult = await runVerify();
    writeText(
      verifyLogPath,
      `${verifyResult.stdout}

--- STDERR ---

${verifyResult.stderr}`
    );
  }

  return {
    verifyResult,
    attempts,
    passed: verifyResult.code === 0,
  };
}
