<p align="center">
  <p align="center">
    <img src="https://github.com/user-attachments/assets/ea5e0aa7-3110-463d-98f1-733c6838cb93" width="96" />
  </p>
  <h1 align="center">Arctron</h1>
  <p align="center">
    Build fast, lightweight desktop apps in TypeScript using native webview.<br />
    Designed to power
      <a href="https://github.com/PenguLoader/PenguLoader" target="_blank"><strong>Pengu Loader</strong></a>.
  </p>
  <br />
  <p align="center">
    <img src="https://img.shields.io/npm/v/arctron?style=for-the-badge" />
    <!-- <img src="https://img.shields.io/npm/d18m/arctron?style=for-the-badge" /> -->
  </p>
</p>

<br />

Monorepo for the arctron runtime, native host, and example apps.

## What Arctron is

Arctron is a lightweight desktop app stack that lets you write app logic in TypeScript while delegating windowing, dialogs, filesystem, and shell integration to a native host.

At a high level:

- your app imports APIs from `arctron` (runtime package),
- the runtime forwards those calls through a native bridge object,
- the host executes them using platform-specific implementations,
- your renderer can stay as a normal web app (for example Vite).

## Monorepo overview

This workspace contains three main parts:

- `packages/arctron` — TypeScript runtime library and Vite plugin (`arctron/vite`)
- `packages/arctron-host` — .NET host process that embeds JS runtime + native platform integrations
- `examples/vite-app` — reference app demonstrating runtime + Vite development flow

## Requirements

- Node.js 20+
- pnpm@9
- .NET 9 SDK

## Workspace scripts

- `pnpm build`
- `pnpm dev`
- `pnpm clean`

## Architecture

Arctron is implemented as layered components:

1. **App code (TypeScript)**
   - Uses public APIs like `app`, `BrowserWindow`, `dialog`, `fs`, `path`, `shell`, `process`, `Tray`.
2. **Runtime package (`packages/arctron`)**
   - Exposes typed APIs and maps calls to `globalThis.__arctronNative`.
   - Handles in-runtime event routing (for example tray callbacks and window RPC dispatch).
3. **Native bridge (`packages/arctron-host/Arctron.Host/NativeBridge.cs`)**
   - C# object injected into JS runtime as `__arctronNative`.
   - Translates JS method calls to `IHostPlatform` operations.
4. **Host platform implementation (`IHostPlatform`)**
   - Executes OS-native behavior (windows, dialogs, shell, fs, process, etc.).
   - Current production implementation is Windows-focused (`WindowsHostPlatform`).

## Implementation details

### Runtime package (`packages/arctron`)

- Public exports are centralized in `src/index.ts` and typed via `src/api/*` and `src/types.ts`.
- Platform modules in `src/platform/*` call `getNativeBridge()` and forward operations.
- Async APIs like `dialog.*`, `fs.*`, and `process.exec` currently wrap synchronous native bridge calls via `Promise.resolve(...)`.
- `BrowserWindow` supports per-window RPC method registration and dispatch:
  - manifest is sent to native via `windowSetRpcManifest`,
  - incoming invoke events are received through `__arctronOnWindowRpcInvoke`,
  - handlers resolve/reject via `windowRpcResolve` / `windowRpcReject`.
- Tray events are bridged once globally and then routed to per-tray emitters.

### Host package (`packages/arctron-host`)

- Entry (`Program.cs`) resolves the main script path from:
  - first CLI argument, or
  - `ARCTRON_MAIN` env var.
- `AppHost` wires together:
  - platform instance (`HostPlatformFactory.Create()`),
  - `NativeBridge`,
  - JS engine runtime (`JsRuntime`), then executes the compiled main script.
- JS runtime uses Jint and injects:
  - `__arctronNative` (native bridge),
  - `console` bridge,
  - `fetch` bridge.
- Platform abstraction is defined by `IHostPlatform`; `WindowsHostPlatform` contains Win32/WebView2-backed behavior, while `DefaultHostPlatform` provides a reduced fallback implementation for non-Windows environments.

### Vite plugin (`arctron/vite`)

In dev mode, plugin behavior is deterministic and process-managed:

- Loads `arctron.config.ts` from app root.
- Starts app main watcher (`dev:main` by default).
- Waits for `arctron.config.main` output file.
- Starts host process (`pnpm --filter @arctron/host dev` by default).
- Sets `ARCTRON_MAIN` to the resolved main output path.
- Prefixes child logs as `[arctron-main]` and `[arctron-host]`.
- Stops watcher and host when Vite server exits.

Available plugin options include:

- `enabled`
- `hostFilter`
- `hostScript`
- `workspaceRoot`
- `mainDevScript`
- `mainReadyTimeoutMs`

## Execution flow

### Development flow (example app)

1. Run `pnpm --filter arctron-vite-example dev`.
2. Vite plugin starts main-process watcher (`dev:main`).
3. Plugin waits for compiled main output (`dist/main-process.js`).
4. Plugin launches host with `ARCTRON_MAIN` pointing to that output.
5. Host executes main-process code, which creates a `BrowserWindow` and loads the Vite dev URL.

### Runtime call flow (API invocation)

1. App code calls runtime API (for example `dialog.openFile`).
2. Runtime module forwards call to `__arctronNative`.
3. `NativeBridge` maps call to `IHostPlatform`.
4. Platform executes native operation and returns result.
5. Runtime exposes result back to app code (often as a Promise).

## Example app requirements

- Every Vite app should define `arctron.config.ts` with a valid `main` output path.
- The app should expose a long-running main build script (`dev:main` by default) that writes to that same output path.
- If scripts/package names differ, configure plugin options (`mainDevScript`, `hostFilter`, `hostScript`, etc.) instead of hardcoding defaults.

## Example app (dev)

Run the Vite example end-to-end:

1. `pnpm install`
2. `pnpm --filter arctron-vite-example dev`

When `arctron/vite` runs in dev mode, it automatically:

- loads `arctron.config.ts` from the app root,
- starts the app main-process watcher (`dev:main` by default),
- waits for `arctron.config.main` output,
- starts the host (`@arctron/host` `dev` script by default),
- sets `ARCTRON_MAIN` for the host process,
- prefixes logs as `[arctron-main]` and `[arctron-host]`,
- stops child processes when the Vite server closes.

## Example app (build checks)

Use these targeted checks:

- `pnpm --filter arctron build`
- `pnpm --filter arctron-vite-example build`

## API surface

The runtime exports:

- `app`
- `BrowserWindow`
- `Tray`
- `dialog`
- `shell`
- `process`
- `path`
- `fs`

For full API signatures and examples, see `docs/api.md`.

## Platform scope and current status

- Windows has the most complete host implementation today (`WindowsHostPlatform`).
- A cross-platform default host exists (`DefaultHostPlatform`) but is intentionally limited and does not provide full native parity.
- Some host APIs are present but still partial in implementation (for example certain tray and RPC native completion paths on the host side).

## Contributor validation checklist

For runtime/plugin changes:

- `pnpm --filter arctron build`
- `pnpm --filter arctron-vite-example build`

For dev orchestration changes, also smoke test:

- `pnpm --filter arctron-vite-example dev`

## Minimal example main process

```ts
import { app, BrowserWindow } from "arctron";

app.whenReady().then(() => {
  const win = new BrowserWindow({
    width: 1200,
    height: 800,
    title: "Arctron"
  });

  win.loadURL("http://localhost:5173");
});
```