import fs from "fs";
import path from "path";

export function ensureDir(dirPath) {
  fs.mkdirSync(dirPath, { recursive: true });
}

export function fileExists(filePath) {
  return fs.existsSync(filePath);
}

export function readText(filePath) {
  return fs.readFileSync(filePath, "utf8");
}

export function writeText(filePath, text) {
  ensureDir(path.dirname(filePath));
  fs.writeFileSync(filePath, text, "utf8");
}
