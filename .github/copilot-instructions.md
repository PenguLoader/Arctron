# Project Copilot Instructions

## Scope
- This is a pnpm monorepo for Arctron runtime (`packages/arctron`), native host (`packages/arctron-host`), and Vite example app (`examples/vite-app`).
- Make focused changes only; avoid unrelated refactors or broad formatting updates.

## Current Vite Integration
- The Vite plugin lives in `packages/arctron/src/vite/index.ts` and is exported as `arctron/vite`.
- In dev mode (`vite dev`), the plugin currently does all of the following:
  - loads `arctron.config.ts` from app root,
  - starts app-level main process watcher via `pnpm run dev:main`,
  - waits for `arctron.config.main` output to exist,
  - starts host via `pnpm --filter @arctron/host dev`,
  - passes `ARCTRON_MAIN` to host,
  - pipes watcher and host logs into Vite console with `[arctron-main]` and `[arctron-host]` prefixes,
  - stops child processes when Vite server closes.

## App Expectations
- Each Vite app using this plugin must provide `arctron.config.ts` with a valid `main` output path.
- The app should define a `dev:main` script that continuously builds `src/main-process.ts` into the same output path used by `arctron.config.ts`.
- If script names differ, use plugin options (`mainDevScript`, `hostFilter`, `hostScript`, etc.) instead of hardcoding assumptions.

## Implementation Rules
- Keep plugin behavior deterministic across Windows/macOS/Linux; use Node APIs and avoid shell-dependent behavior where possible.
- Prefer explicit, actionable errors when required files/scripts are missing.
- Preserve current logging style and process lifecycle semantics unless requested otherwise.
- Do not introduce new dependencies unless they are necessary and justified.

## Validation
- For plugin/runtime changes, run targeted checks first:
  - `pnpm --filter arctron build`
  - `pnpm --filter arctron-vite-example build`
- For dev-orchestration changes, also smoke test:
  - `pnpm --filter arctron-vite-example dev`
