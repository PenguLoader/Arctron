# Arctron API Reference

This document describes the public runtime APIs exported by `arctron`.

## Import

```ts
import {
  app,
  BrowserWindow,
  Tray,
  dialog,
  shell,
  process,
  path,
  fs
} from "arctron";
```

## `app`

### Events

```ts
type AppEvent = "ready" | "before-quit" | "window-all-closed" | "activate";
```

### Methods

```ts
app.whenReady(): Promise<void>
app.quit(): void
app.on(event: AppEvent, handler: () => void): () => void
```

### Example

```ts
await app.whenReady();
const off = app.on("activate", () => {
  console.log("app activated");
});

// later
off();
```

## `BrowserWindow`

### Options

```ts
interface BrowserWindowOptions {
  width?: number;
  height?: number;
  x?: number;
  y?: number;
  title?: string;
  show?: boolean;
  frameless?: boolean;
  initialUrl?: string;
  devTools?: boolean;
  contextMenu?: boolean;
  rpc?: {
    namespace?: string;
    methods?: Record<string, (...args: unknown[]) => Promise<unknown> | unknown>;
  };
}
```

### Methods

```ts
new BrowserWindow(options?: BrowserWindowOptions)
window.loadURL(url: string): void
window.show(): void
window.hide(): void
window.close(): void
window.setTitle(title: string): void
window.getSize(): [number, number]
window.getPosition(): [number, number]
window.focus(): void
window.center(): void
window.minimize(): void
window.unminimize(): void
window.isMinimized(): boolean
window.maximize(): void
window.unmaximize(): void
window.isMaximized(): boolean
```

### Example

```ts
const win = new BrowserWindow({
  width: 1200,
  height: 800,
  x: 100,
  y: 80,
  title: "Arctron",
  frameless: false,
  initialUrl: "http://localhost:5173",
  devTools: true,
  contextMenu: true,
  rpc: {
    namespace: "app",
    methods: {
      ping: async (message: string) => `pong:${message}`,
      add: (a: number, b: number) => a + b
    }
  }
});

win.center();
const [w, h] = win.getSize();
const [x, y] = win.getPosition();
console.log(w, h, x, y);
```

### RPC behavior

- `rpc.methods` registers named functions on the main/runtime side per window.
- Renderer integration is host-implementation dependent (WebView2 on Windows, WebKit WebView on macOS).
- The contract is request/response and async-friendly: renderer invoke -> native -> main handler -> native resolve/reject.

## `Tray`

### Types

```ts
interface TrayMenuItem {
  id: string;
  label: string;
  enabled?: boolean;
}

interface TrayOptions {
  tooltip?: string;
  icon?: string;
  menu?: TrayMenuItem[];
}

type TrayEvent = "click" | "double-click" | "menu-item-click";
```

### Methods

```ts
new Tray(options?: TrayOptions)
tray.setToolTip(text: string): void
tray.setContextMenu(menu: TrayMenuItem[]): void
tray.on(event: "click" | "double-click", handler: () => void): () => void
tray.on(event: "menu-item-click", handler: (menuItemId: string) => void): () => void
```

### Example

```ts
const tray = new Tray({
  tooltip: "Arctron",
  menu: [{ id: "open", label: "Open" }, { id: "quit", label: "Quit" }]
});

tray.setToolTip("Arctron Running");
tray.on("click", () => {
  console.log("tray clicked");
});

tray.on("double-click", () => {
  console.log("tray double-clicked");
});

tray.on("menu-item-click", (menuItemId) => {
  console.log("tray menu item selected:", menuItemId);
});
```

## `dialog`

### Types

```ts
interface OpenDialogOptions {
  title?: string;
  allowMultiple?: boolean;
  filters?: { name: string; extensions: string[] }[];
}

interface SaveDialogOptions {
  title?: string;
  defaultPath?: string;
  filters?: { name: string; extensions: string[] }[];
}

interface MessageBoxOptions {
  title?: string;
  message: string;
  detail?: string;
  buttons?: string[];
}

interface MessageBoxResult {
  response: number;
}
```

### Methods

```ts
dialog.openFile(options?: OpenDialogOptions): Promise<string[]>
dialog.saveFile(options?: SaveDialogOptions): Promise<string | null>
dialog.messageBox(options: MessageBoxOptions): Promise<MessageBoxResult>
```

### Example

```ts
const files = await dialog.openFile({ allowMultiple: true });
const saveTo = await dialog.saveFile({ defaultPath: "output.txt" });
await dialog.messageBox({ title: "Done", message: "Operation completed" });
```

## `shell`

### Methods

```ts
shell.openURL(url: string): Promise<void>
shell.revealPath(path: string): Promise<void>
shell.trashPath(path: string): Promise<void>

// compatibility aliases
shell.openExternal(url: string): Promise<void>
shell.openPath(path: string): Promise<void>
```

### Example

```ts
await shell.openURL("https://example.com");
await shell.revealPath("C:/tmp/report.txt");
await shell.trashPath("C:/tmp/old-report.txt");
```

## `process`

### Types

```ts
interface ExecOptions {
  cwd?: string;
  env?: Record<string, string>;
  timeoutMs?: number;
}

interface ExecResult {
  exitCode: number;
  stdout: string;
  stderr: string;
}
```

### Methods

```ts
process.exec(command: string, args?: string[], options?: ExecOptions): Promise<ExecResult>
```

### Example

```ts
const result = await process.exec("node", ["-v"]);
console.log(result.exitCode, result.stdout);
```

## `path`

### Methods

```ts
path.join(...parts: string[]): string
path.resolve(...parts: string[]): string
path.getUserData(): string
path.getLocalData(): string
path.getTemp(): string
path.getCwd(): string
path.getBaseExeDir(): string
```

### Special path semantics

- `getUserData()` → roaming app data directory.
- `getLocalData()` → local app data directory.
- `getTemp()` → OS temp directory.
- `getCwd()` → current working directory of the host process.
- `getBaseExeDir()` → directory of the host executable/runtime base.

### Example

```ts
const cacheDir = path.join(path.getLocalData(), "arctron", "cache");
const absoluteConfig = path.resolve("./config", "app.json");
const tempFile = path.join(path.getTemp(), "session.tmp");
```

## `fs`

### Methods

```ts
fs.readText(path: string): Promise<string>
fs.writeText(path: string, content: string): Promise<void>
fs.appendText(path: string, content: string): Promise<void>
fs.exists(path: string): Promise<boolean>
fs.mkdir(path: string, recursive?: boolean): Promise<void>
fs.readdir(path: string): Promise<string[]>
fs.rm(path: string, recursive?: boolean): Promise<void>
```

### Example

```ts
const logsDir = path.join(path.getLocalData(), "arctron", "logs");
await fs.mkdir(logsDir, true);

const logFile = path.join(logsDir, "app.log");
await fs.writeText(logFile, "boot\n");
await fs.appendText(logFile, "ready\n");

const exists = await fs.exists(logFile);
if (exists) {
  const content = await fs.readText(logFile);
  console.log(content);
}

const entries = await fs.readdir(logsDir);
console.log(entries);
```

## Notes

- Most APIs are thin wrappers over the native bridge (`__arctronNative`).
- Path separator and path normalization follow host platform behavior.
- `shell.trashPath()` may be platform-dependent and can throw if not implemented by a specific host backend.
