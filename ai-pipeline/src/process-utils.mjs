import { spawn } from "child_process";
import { PIPELINE_DIR } from "./paths.mjs";

export function stripAnsi(text) {
  return text.replace(
    // eslint-disable-next-line no-control-regex
    /\u001b\[[0-9;]*m/g,
    ""
  );
}

export function runProcess(command, args, options = {}) {
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
      resolve({
        code,
        stdout,
        stderr,
      });
    });
  });
}
