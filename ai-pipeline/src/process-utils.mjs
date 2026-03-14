import { spawn, execSync } from "child_process";
import { PIPELINE_DIR } from "./paths.mjs";

export function stripAnsi(text) {
  return text.replace(
    // eslint-disable-next-line no-control-regex
    /\u001b\[[0-9;]*m/g,
    ""
  );
}

// Registry of all active child processes so they can be cleaned up on exit.
const activeChildren = new Set();

function killChild(child) {
  if (child.pid == null) return;
  try {
    if (process.platform === "win32") {
      // /T kills the entire process tree; /F forces termination.
      execSync(`taskkill /T /F /PID ${child.pid}`, { stdio: "ignore" });
    } else {
      process.kill(-child.pid, "SIGKILL");
    }
  } catch {
    // Process may have already exited — ignore.
  }
}

function killAllChildren() {
  for (const child of activeChildren) {
    killChild(child);
  }
  activeChildren.clear();
}

process.on("exit", killAllChildren);
process.on("SIGINT", () => { killAllChildren(); process.exit(130); });
process.on("SIGTERM", () => { killAllChildren(); process.exit(143); });

export function runProcess(command, args, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      cwd: options.cwd ?? PIPELINE_DIR,
      shell: options.shell ?? false,
      stdio: options.captureOutput
        ? ["pipe", "pipe", "pipe"]
        : ["pipe", "inherit", "inherit"],
      windowsHide: true,
      // detached on POSIX lets us kill the whole process group via -pid
      detached: process.platform !== "win32",
    });

    activeChildren.add(child);

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

    child.on("error", (err) => {
      activeChildren.delete(child);
      reject(err);
    });

    child.on("close", (code) => {
      activeChildren.delete(child);
      resolve({
        code,
        stdout,
        stderr,
      });
    });
  });
}
