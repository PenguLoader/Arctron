import fs from "node:fs";
import path from "node:path";
import jiti from "jiti";
import type { ArctronConfig } from "../types";

export function loadConfig(cwd = process.cwd(), fileName = "arctron.config.ts"): ArctronConfig {
  const fullPath = path.join(cwd, fileName);
  if (!fs.existsSync(fullPath)) {
    throw new Error(`Config file not found: ${fullPath}`);
  }

  const loader = jiti(cwd, { interopDefault: true });
  const loaded = loader(fullPath) as { default?: ArctronConfig } | ArctronConfig;
  const config = (loaded as { default?: ArctronConfig }).default ?? (loaded as ArctronConfig);

  if (!config || typeof config.main !== "string") {
    throw new Error("Invalid arctron config: missing 'main' field");
  }

  return config;
}
