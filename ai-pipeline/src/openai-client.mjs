import OpenAI from "openai";
import { MODEL, REASONING_EFFORT } from "./config.mjs";

const client = new OpenAI();

export async function askModel(prompt) {
  const response = await client.responses.create({
    model: MODEL,
    reasoning: { effort: REASONING_EFFORT },
    input: prompt,
  });

  const text = response.output_text?.trim();
  if (!text) {
    console.dir(response, { depth: 8 });
    throw new Error("Model returned no output_text.");
  }

  return text;
}
