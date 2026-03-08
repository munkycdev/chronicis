import { VERIFY_SCRIPT, REPO_ROOT } from "../paths.mjs";
import { runProcess } from "../process-utils.mjs";

export async function runVerify() {
  return await runProcess(
    "powershell",
    ["-ExecutionPolicy", "Bypass", "-File", VERIFY_SCRIPT],
    {
      cwd: REPO_ROOT,
      captureOutput: true,
      shell: false,
    }
  );
}
