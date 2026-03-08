export function setTerminalTitle(title) {
  const safe = String(title).replace(/[^\x20-\x7E]/g, "");
  process.stdout.write(`\u001b]0;${safe}\u0007`);
}

export function timestamp() {
  return new Date().toISOString();
}

export function logStep(message) {
  console.log(`    ${message}`);
}

export function logBanner(title, details = []) {
  const line = "=".repeat(46);
  console.log(line);
  console.log(` ${title}`);
  for (const detail of details) {
    console.log(` ${detail}`);
  }
  console.log(line);
}
