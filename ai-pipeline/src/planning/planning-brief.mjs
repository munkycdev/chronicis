import { askModel } from "../openai-client.mjs";
import { buildPlanningBriefPrompt } from "../prompts/planning-prompts.mjs";

export async function generatePlanningBrief(
  phaseSpec,
  architecture,
  frozenAssumptions,
  phaseFileName,
  futurePhaseNames
) {
  return await askModel(
    buildPlanningBriefPrompt(
      phaseSpec,
      architecture,
      frozenAssumptions,
      phaseFileName,
      futurePhaseNames
    )
  );
}
